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

        public LoanDisbursementController(ApplicationDbContext db, PayStackTransferService transferService, IConfiguration config)
        {
            _db = db;
            _transferService = transferService;
            _simulateTransfers = config.GetValue<bool>("PayStack:SimulateTransfers");
        }

    
        private int GetCurrentOwnerId()
        {
            var ownerId = HttpContext.Session.GetInt32("UserId");
            if (ownerId is null)
                throw new InvalidOperationException("Could not resolve the current shop owner's OwnerId from the session.");
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

        [HttpGet]
        public async Task<IActionResult> DisburseForm(int id) // id = LoanApplicationId
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

      

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disburse(DisburseViewModel model)
        {
            var branchId = await GetCurrentBranchIdAsync();

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

            if (_simulateTransfers)
            {
                disbursement.RecipientCode = "SIMULATED";
                disbursement.TransferCode = $"SIM-{Guid.NewGuid().ToString()[..8].ToUpper()}";
                disbursement.TransferStatus = "success";
                disbursement.Success = true;

                _db.LoanDisbursements.Add(disbursement);
                application.Status = LoanApplicationStatus.Disbursed;


                // ── Create the repayment record (UC-05) ──
                var loan = new Loan
                {
                    LoanApplicationId = application.LoanApplicationId,
                    PrincipalAmount = application.RequestedAmount,
                    InterestRate = 0.05m, // ASSUMPTION — confirm with team
                    TotalRepayable = application.RequestedAmount * 1.05m,
                    OutstandingBalance = application.RequestedAmount * 1.05m,
                    DisbursedOn = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddMonths(6),
                    Status = LoanRepaymentStatus.Active
                };
                _db.Loans.Add(loan);

                await _db.SaveChangesAsync();

                return RedirectToAction(nameof(DisburseComplete), new { id = application.LoanApplicationId });
            }

            // ── Real PayStack transfer path (used once account is a Registered Business) ──
            var recipientResult = await _transferService.CreateRecipient(
                model.AccountHolderName, model.AccountNumber, model.BankCode);

            if (!recipientResult.Success)
            {
                disbursement.Success = false;
                disbursement.FailureMessage = $"Could not create PayStack recipient: {recipientResult.Message}";
                _db.LoanDisbursements.Add(disbursement);
                await _db.SaveChangesAsync();

                TempData["Error"] = disbursement.FailureMessage;
                return RedirectToAction(nameof(DisburseForm), new { id = application.LoanApplicationId });
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

                // ── Create the repayment record (UC-05) ──
                var loan = new Loan
                {
                    LoanApplicationId = application.LoanApplicationId,
                    PrincipalAmount = application.RequestedAmount,
                    InterestRate = 0.05m, // ASSUMPTION — confirm with team
                    TotalRepayable = application.RequestedAmount * 1.05m,
                    OutstandingBalance = application.RequestedAmount * 1.05m,
                    DisbursedOn = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddMonths(6),
                    Status = LoanRepaymentStatus.Active
                };
                _db.Loans.Add(loan);
            }

            await _db.SaveChangesAsync();

            if (!transferResult.Success)
            {
                TempData["Error"] = $"Transfer could not be completed: {transferResult.Message}";
                return RedirectToAction(nameof(DisburseForm), new { id = application.LoanApplicationId });
            }

            return RedirectToAction(nameof(DisburseComplete), new { id = application.LoanApplicationId });
        }

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