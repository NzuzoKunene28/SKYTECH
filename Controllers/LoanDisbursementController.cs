using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.SkytechWorkers;
using SkyTech.Models.ViewModels;
using SkyTech.Services;

namespace SkyTech.Controllers
{
    [AuthorizeUser]
    public class LoanDisbursementController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly PayStackTransferService _transferService;
        private readonly bool _simulateTransfers;
        private readonly EmailServices _services;

        public LoanDisbursementController(
            ApplicationDbContext db,
            EmailServices services,
            PayStackTransferService transferService,
            IConfiguration config)
        {
            _db = db;
            _transferService = transferService;
            _services = services;
            _simulateTransfers = config.GetValue<bool>("PayStack:SimulateTransfers");
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------

        private int GetCurrentOwnerId()
        {
            var ownerId = HttpContext.Session.GetInt32("UserId");
            if (ownerId is null)
                throw new InvalidOperationException(
                    "Could not resolve the current shop owner's OwnerId from the session.");
            return ownerId.Value;
        }

        private async Task<int> GetCurrentBranchIdAsync(CancellationToken ct = default)
        {
            var ownerId = GetCurrentOwnerId();
            return await _db.shopowners
                .Where(o => o.OwnerId == ownerId)
                .Select(o => o.BranchId)
                .FirstAsync(ct);
        }

        /// <summary>
        /// Loads the customer directly by CustomerId — avoids relying on
        /// .Include(a => a.Customer) which can return null when Customer
        /// lives in a different namespace to LoanApplication.
        /// </summary>
        private async Task<Customer?> GetCustomerForApplicationAsync(LoanApplication application)
        {
            return await _db.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == application.CustomerId);
        }

        // ----------------------------------------------------------------
        // GET: DisburseForm
        // ----------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> DisburseForm(int id)
        {
            var branchId = await GetCurrentBranchIdAsync();

            var application = await _db.LoanApplications
                .FirstOrDefaultAsync(a => a.LoanApplicationId == id);

            if (application is null) return NotFound();
            if (application.BranchId != branchId) return Forbid();
            if (application.Status != LoanApplicationStatus.Approved)
            {
                TempData["Error"] = "This application must be approved before it can be disbursed.";
                return RedirectToAction("Queue", "LoanAssessment");
            }

            var banks = await _transferService.GetZaBanks();

            var model = new DisburseViewModel
            {
                LoanApplicationId = application.LoanApplicationId,
                RequestedAmount = application.RequestedAmount,
                AvailableBanks = banks
                    .Select(b => (dynamic)b)
                    .Select(b => new SelectListItem { Value = b.code, Text = b.name })
                    .ToList()
            };

            return View(model);
        }

        // ----------------------------------------------------------------
        // POST: Disburse
        // ----------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disburse(DisburseViewModel model)
        {
            var branchId = await GetCurrentBranchIdAsync();

            // Load application WITHOUT Include — customer loaded separately below
            var application = await _db.LoanApplications
                .FirstOrDefaultAsync(a => a.LoanApplicationId == model.LoanApplicationId);

            if (application is null) return NotFound();
            if (application.BranchId != branchId) return Forbid();
            if (application.Status != LoanApplicationStatus.Approved)
            {
                TempData["Error"] = "This application must be approved before it can be disbursed.";
                return RedirectToAction("Queue", "LoanAssessment");
            }

            if (!ModelState.IsValid)
            {
                model.AvailableBanks = (await _transferService.GetZaBanks())
                    .Select(b => (dynamic)b)
                    .Select(b => new SelectListItem { Value = b.code, Text = b.name })
                    .ToList();
                return View("DisburseForm", model);
            }

            var disbursement = new LoanDisbursement
            {
                LoanApplicationId = application.LoanApplicationId,
                OwnerId = GetCurrentOwnerId(),
                AccountHolderName = model.AccountHolderName,
                AccountNumber = model.AccountNumber,
                BankCode = model.BankCode,
                AmountDisbursed = application.RequestedAmount
            };

            // ── Simulated transfer path ──────────────────────────────────
            if (_simulateTransfers)
            {
                disbursement.RecipientCode = "SIMULATED";
                disbursement.TransferCode = $"SIM-{Guid.NewGuid().ToString()[..8].ToUpper()}";
                disbursement.TransferStatus = "success";
                disbursement.Success = true;

                _db.LoanDisbursements.Add(disbursement);
                application.Status = LoanApplicationStatus.Disbursed;

                _db.Loans.Add(new Loan
                {
                    LoanApplicationId = application.LoanApplicationId,
                    PrincipalAmount = application.RequestedAmount,
                    InterestRate = 0.05m,
                    TotalRepayable = application.RequestedAmount * 1.05m,
                    OutstandingBalance = application.RequestedAmount * 1.05m,
                    DisbursedOn = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddMonths(6),
                    Status = LoanRepaymentStatus.Active
                });

                await _db.SaveChangesAsync();

                // Direct customer query — does not rely on navigation property
                var customer = await GetCustomerForApplicationAsync(application);

                if (customer != null)
                {
                    _services.LoanDisbursed(
                        customer.Email,
                        customer.FirstName,
                        disbursement.AmountDisbursed,
                        disbursement.TransferCode);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[LoanDisbursement] WARNING: Customer not found for CustomerId {application.CustomerId} — email not sent.");
                }

                return RedirectToAction(nameof(DisburseComplete),
                    new { id = application.LoanApplicationId });
            }

            // ── Real PayStack transfer path ──────────────────────────────
            var recipientResult = await _transferService.CreateRecipient(
                model.AccountHolderName, model.AccountNumber, model.BankCode);

            if (!recipientResult.Success)
            {
                disbursement.Success = false;
                disbursement.FailureMessage =
                    $"Could not create PayStack recipient: {recipientResult.Message}";

                _db.LoanDisbursements.Add(disbursement);
                await _db.SaveChangesAsync();

                TempData["Error"] = disbursement.FailureMessage;
                return RedirectToAction(nameof(DisburseForm),
                    new { id = application.LoanApplicationId });
            }

            disbursement.RecipientCode = recipientResult.RecipientCode;

            var reference = $"LOAN-{application.LoanApplicationId}-{DateTime.UtcNow.Ticks}";
            var transferResult = await _transferService.InitiateTransfer(
                recipientResult.RecipientCode,
                (double)application.RequestedAmount,
                $"Loan disbursement for application #{application.LoanApplicationId}",
                reference);

            disbursement.TransferCode = transferResult.TransferCode;
            disbursement.TransferStatus = transferResult.Status;
            disbursement.Success = transferResult.Success;
            disbursement.FailureMessage = transferResult.Success ? null : transferResult.Message;

            _db.LoanDisbursements.Add(disbursement);

            if (transferResult.Success)
            {
                application.Status = LoanApplicationStatus.Disbursed;

                _db.Loans.Add(new Loan
                {
                    LoanApplicationId = application.LoanApplicationId,
                    PrincipalAmount = application.RequestedAmount,
                    InterestRate = 0.05m,
                    TotalRepayable = application.RequestedAmount * 1.05m,
                    OutstandingBalance = application.RequestedAmount * 1.05m,
                    DisbursedOn = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddMonths(6),
                    Status = LoanRepaymentStatus.Active
                });

                await _db.SaveChangesAsync();

                // Direct customer query — does not rely on navigation property
                var customer = await GetCustomerForApplicationAsync(application);

                if (customer != null)
                {
                    _services.LoanDisbursed(
                        customer.Email,
                        customer.FirstName,
                        disbursement.AmountDisbursed,
                        disbursement.TransferCode);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[LoanDisbursement] WARNING: Customer not found for CustomerId {application.CustomerId} — email not sent.");
                }

                return RedirectToAction(nameof(DisburseComplete),
                    new { id = application.LoanApplicationId });
            }

            await _db.SaveChangesAsync();

            TempData["Error"] = $"Transfer could not be completed: {transferResult.Message}";
            return RedirectToAction(nameof(DisburseForm),
                new { id = application.LoanApplicationId });
        }

        // ----------------------------------------------------------------
        // GET: DisburseComplete
        // ----------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> DisburseComplete(int id)
        {
            var branchId = await GetCurrentBranchIdAsync();

            var application = await _db.LoanApplications
                .FirstOrDefaultAsync(a => a.LoanApplicationId == id);

            if (application is null) return NotFound();
            if (application.BranchId != branchId) return Forbid();

            var disbursement = await _db.LoanDisbursements
                .Where(d => d.LoanApplicationId == id && d.Success)
                .OrderByDescending(d => d.AttemptedOn)
                .FirstOrDefaultAsync();

            ViewBag.Disbursement = disbursement;
            return View(application);
        }
    }
}