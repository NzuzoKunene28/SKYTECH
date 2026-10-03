using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.SkytechWorkers;
using SkyTech.Models.ViewModels;
using QRCoder;

namespace SkyTech.Controllers
{
    /// <summary>
    /// Customer-side repayment feature (UC-05): view outstanding balance
    /// and repayment history, and settle a loan.
    /// </summary>
    [AuthorizeUser]
    public class LoanRepaymentController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly EmailServices _emailServices;

        public LoanRepaymentController(ApplicationDbContext context, EmailServices emailServices)
        {
            db = context;
            _emailServices = emailServices;
        }

        private int GetCurrentCustomerId()
        {
            var customerId = HttpContext.Session.GetInt32("UserId");
            if (customerId is null)
                throw new InvalidOperationException("Could not resolve the current customer's CustomerId from the session.");
            return customerId.Value;
        }


        // ── NEW: entry point from the dashboard link ──
        [HttpGet]
        public async Task<IActionResult> MyLoan()
        {
            var customerId = GetCurrentCustomerId();

            var loan = await db.Loans
                .Where(l => l.LoanApplication.CustomerId == customerId && l.Status == LoanRepaymentStatus.Active)
                .OrderByDescending(l => l.DisbursedOn)
                .FirstOrDefaultAsync();

            if (loan == null)
            {
                TempData["Info"] = "You don't have an active loan to repay right now.";
                return RedirectToAction("ShoppingPage", "Customer");
            }

            return RedirectToAction(nameof(RepayLoan), new { loanId = loan.LoanId });
        }

        [HttpGet]
        public async Task<IActionResult> RepayLoan(int loanId)
        {
            var loan = await GetOwnedLoanAsync(loanId);
            if (loan == null) return NotFound();

            if (loan.Status != LoanRepaymentStatus.Active)
                return RedirectToAction(nameof(Confirmation), new { loanId });

            return View(BuildViewModel(loan));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SettleLoan(RepayLoanViewModel model)
        {
            var loan = await GetOwnedLoanAsync(model.LoanId);
            if (loan == null) return NotFound();

            if (loan.Status != LoanRepaymentStatus.Active)
                return RedirectToAction(nameof(Confirmation), new { loanId = loan.LoanId });

            // ── NEW: verify transfer reference matches the original disbursement ──
            var disbursement = await db.LoanDisbursements
                .Where(d => d.LoanApplicationId == loan.LoanApplicationId && d.Success)
                .OrderByDescending(d => d.AttemptedOn)
                .FirstOrDefaultAsync();

            if (disbursement == null ||
                !string.Equals(disbursement.TransferCode?.Trim(), model.TransferReferenceInput?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.TransferReferenceInput), "That transfer reference doesn't match our records. Check your disbursement confirmation and try again.");
                return View("RepayLoan", BuildViewModel(loan));
            }

            if (model.PaymentAmount <= 0 || model.PaymentAmount > loan.OutstandingBalance)
            {
                ModelState.AddModelError(nameof(model.PaymentAmount), "Enter a valid amount up to the outstanding balance.");
                return View("RepayLoan", BuildViewModel(loan));
            }

            loan.OutstandingBalance -= model.PaymentAmount;

            db.LoanPayments.Add(new LoanPayment
            {
                LoanId = loan.LoanId,
                Amount = model.PaymentAmount,
                Method = model.PaymentMethod,
                PaidOn = DateTime.UtcNow,
                BalanceAfterPayment = loan.OutstandingBalance,
                TransactionReference = model.TransferReferenceInput // reuse it as the payment's own reference too
            });

            if (loan.OutstandingBalance <= 0)
            {
                loan.OutstandingBalance = 0;
                loan.Status = LoanRepaymentStatus.Settled;
                loan.SettledOn = DateTime.UtcNow;

                var collectionDate = DateTime.UtcNow.AddDays(3);
                try
                {
                    var customer = await db.Customers.FindAsync(loan.LoanApplication.CustomerId);
                    if (customer != null && !string.IsNullOrWhiteSpace(customer.Email))
                    {
                        _emailServices.LoanSettled(
                            customer.Email,
                            customer.FirstName,
                            loan.LoanApplication.CollateralItems.Any()
                                ? string.Join(", ", loan.LoanApplication.CollateralItems.Select(c => c.Description))
                                : "your item",
                            collectionDate);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Email failed: {ex.Message}");
                }
            }

            await db.SaveChangesAsync();

            return RedirectToAction(nameof(Confirmation), new { loanId = loan.LoanId });
        }
        [HttpGet]
        public async Task<IActionResult> Confirmation(int loanId)
        {
            var loan = await GetOwnedLoanAsync(loanId);
            if (loan == null) return NotFound();
            return View(BuildViewModel(loan));
        }

        private async Task<Loan?> GetOwnedLoanAsync(int loanId)
        {
            var customerId = GetCurrentCustomerId();

            return await db.Loans
                .Include(l => l.Payments)
                .Include(l => l.LoanApplication).ThenInclude(a => a.CollateralItems)
                .FirstOrDefaultAsync(l => l.LoanId == loanId && l.LoanApplication.CustomerId == customerId);
        }

        private RepayLoanViewModel BuildViewModel(Loan loan)
        {
            var history = new List<BalancePointViewModel>
    {
        new() { Date = loan.DisbursedOn, Balance = loan.TotalRepayable }
    };
            history.AddRange(loan.Payments
                .OrderBy(p => p.PaidOn)
                .Select(p => new BalancePointViewModel { Date = p.PaidOn, Balance = p.BalanceAfterPayment }));

            return new RepayLoanViewModel
            {
                LoanId = loan.LoanId,
                OutstandingBalance = loan.OutstandingBalance,
                TotalRepayable = loan.TotalRepayable,
                AmountPaid = loan.AmountPaid,
                DueDate = loan.DueDate,
                Collateral = loan.LoanApplication.CollateralItems
                    .Select(c => new CollateralDisplayItem
                    {
                        Description = c.Description,
                        ImagePath = c.ImagePath,
                        DeclaredValue = c.DeclaredValue,
                        Condition = c.Condition
                    })
                    .ToList(),
                BalanceHistory = history,
                CollectionToken = loan.CollectionToken
            };
        }

        [HttpGet]
        public async Task<IActionResult> CollectionQr(int loanId)
        {
            var loan = await GetOwnedLoanAsync(loanId);
            if (loan == null) return NotFound();
            if (loan.Status != LoanRepaymentStatus.Settled) return BadRequest("Loan must be settled first.");

            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(loan.CollectionToken, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            var bytes = qrCode.GetGraphic(20);

            return File(bytes, "image/png");
        }

    }
}