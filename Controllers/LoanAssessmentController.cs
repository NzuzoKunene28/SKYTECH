using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.SkytechWorkers;
//using SkyTech.Models.SkytechWorkers.SkyTech.Models.SkytechWorkers;
using SkyTech.Models.ViewModels;
using SkyTech.Services;
using SkyTech.ViewModels;
//using static SkyTech.ViewModels.AssessmentViewModel;

namespace SkyTech.Controllers
{
    /// <summary>
    /// Shop-owner side of the loan feature (UC-04): reviewing applications
    /// submitted from a customer's own branch, re-checking credit/clearance,
    /// running fraud checks, physically re-verifying collateral, and
    /// recording the final decision.
    /// </summary>
    // [Authorize(Roles = "ShopOwner")] // ASSUMPTION: matches the role name used elsewhere for shop-owner-only actions
    [AuthorizeUser]
    public class LoanAssessmentController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ICollateralVerificationService _verificationService;
        private readonly IFraudDetectionService _fraudService;
        private readonly IWebHostEnvironment _env;

       
        private const int CreditQualifyingThreshold = 600;

        public LoanAssessmentController(
            ApplicationDbContext db,
            ICollateralVerificationService verificationService,
            IFraudDetectionService fraudService,
            IWebHostEnvironment env)
        {
            _db = db;
            _verificationService = verificationService;
            _fraudService = fraudService;
            _env = env;
        }

        /// <summary>Resolves the OwnerId of the currently logged-in shop owner.
        /// ASSUMPTION: pulled from claims the same way other shop-owner-only
        /// actions in this project resolve the current owner. Adjust the
        /// claim type to whatever's actually used at login.</summary>
        private int GetCurrentOwnerId()
        {
            var ownerId = HttpContext.Session.GetInt32("UserId");

            if (ownerId is null)
                throw new InvalidOperationException("Could not resolve the current shop owner's OwnerId from the session. Ensure the shop owner is logged in and [AuthorizeUser] (or equivalent) has already run.");

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

        // ── Queue: applications submitted at this owner's branch, awaiting assessment ──
        [HttpGet]
        public async Task<IActionResult> Queue()
        {
            var branchId = await GetCurrentBranchIdAsync();

            var applications = await _db.LoanApplications
                .Where(a => a.BranchId == branchId
                    && (a.Status == LoanApplicationStatus.Submitted || a.Status == LoanApplicationStatus.UnderStaffReview))
                .OrderBy(a => a.SubmittedOn)
                .ToListAsync();

            return View(applications);
        }

        // ── Loading screen shown between clicking "Assess" and Step 1 ──
        [HttpGet]
        public async Task<IActionResult> AssessmentLoading(int id)
        {
            var assessment = await GetOwnedAssessmentAsync(id);
            if (assessment is null) return NotFound();

            return View(assessment);
        }



        // ── StartAssessment now redirects into Step 1 instead of one big page ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartAssessment(int loanApplicationId)
        {
            var branchId = await GetCurrentBranchIdAsync();

            var application = await _db.LoanApplications
                .Include(a => a.CollateralItems)
                .FirstOrDefaultAsync(a => a.LoanApplicationId == loanApplicationId);

            if (application is null) return NotFound();
            if (application.BranchId != branchId) return Forbid();

            var existing = await _db.LoanAssessments
                .Where(a => a.LoanApplicationId == loanApplicationId && a.CompletedOn == null)
                .OrderByDescending(a => a.StartedOn)
                .FirstOrDefaultAsync();

            //if (existing is not null)
            //{
            //    return RedirectToAction(nameof(CreditRecheck), new { id = existing.LoanAssessmentId });
            //}

            if (existing is not null)
            {
                return RedirectToAction(nameof(AssessmentLoading), new { id = existing.LoanAssessmentId });
            }

            var creditRecord = await _db.CreditRecords.FindAsync(application.NationalId);
            var criminalRecord = await _db.CriminalRecords.FindAsync(application.NationalId);
            var currentScore = creditRecord?.CreditScore ?? 0;

            var assessment = new LoanAssessment
            {
                LoanApplicationId = loanApplicationId,
                OwnerId = GetCurrentOwnerId(),
                CreditScoreAtAssessment = currentScore,
                ClearanceStatusAtAssessment = criminalRecord?.ClearanceStatus ?? ClearanceStatus.UnderReview,
                StillMeetsCreditThreshold = currentScore >= CreditQualifyingThreshold
            };

            _db.LoanAssessments.Add(assessment);
            await _db.SaveChangesAsync();

            var fraudFlags = await _fraudService.DetectAsync(application, assessment.LoanAssessmentId);
            if (fraudFlags.Count > 0)
            {
                _db.FraudFlags.AddRange(fraudFlags);
            }

            application.Status = LoanApplicationStatus.UnderStaffReview;
            await _db.SaveChangesAsync();

            // return RedirectToAction(nameof(CreditRecheck), new { id = assessment.LoanAssessmentId });

            return RedirectToAction(nameof(AssessmentLoading), new { id = assessment.LoanAssessmentId });
        }


        // ── Shared loader used by all four step pages ──
        private async Task<LoanAssessment?> GetOwnedAssessmentAsync(int id)
        {
            var branchId = await GetCurrentBranchIdAsync();

            var assessment = await _db.LoanAssessments
                .Include(a => a.LoanApplication).ThenInclude(app => app.CollateralItems)
                .Include(a => a.FraudFlags)
                .FirstOrDefaultAsync(a => a.LoanAssessmentId == id);

            if (assessment is null) return null;
            if (assessment.LoanApplication.BranchId != branchId) return null; // treated as NotFound by callers, same as Forbid intent

            return assessment;
        }

        // ── Step 1 of 4: Credit & Clearance Re-check ──
        [HttpGet]
        public async Task<IActionResult> CreditRecheck(int id)
        {
            var assessment = await GetOwnedAssessmentAsync(id);
            if (assessment is null) return NotFound();

            return View(AssessmentViewModel.FromEntity(assessment));
        }

       
        [HttpGet]
        public async Task<IActionResult> FraudFlags(int id)
        {
            var assessment = await GetOwnedAssessmentAsync(id);
            if (assessment is null) return NotFound();

            var vm = AssessmentViewModel.FromEntity(assessment);
            vm.CollateralImageChecks = await _fraudService.CheckImageDuplicatesAsync(assessment.LoanApplication);

            return View(vm);
        }

        // ── Loading screen shown between Credit Recheck and Fraud Flags ──
        [HttpGet]
        public async Task<IActionResult> FraudFlagsLoading(int id)
        {
            var assessment = await GetOwnedAssessmentAsync(id);
            if (assessment is null) return NotFound();

            return View(assessment);
        }

        // ── Step 3 of 3: Final Decision ──
        [HttpGet]
        public async Task<IActionResult> Decision(int id)
        {
            var assessment = await GetOwnedAssessmentAsync(id);
            if (assessment is null) return NotFound();

            return View(AssessmentViewModel.FromEntity(assessment));
        }
        //[HttpGet]
        //public async Task<IActionResult> Decision(int id)
        //{
        //    var assessment = await GetOwnedAssessmentAsync(id);
        //    if (assessment is null) return NotFound();

        //    var vm = AssessmentViewModel.FromEntity(assessment);
        //    vm.CollateralImageChecks = await _fraudService.CheckImageDuplicatesAsync(assessment.LoanApplication);

        //    return View(vm);
        //}


        // ── Completion screen ──
        [HttpGet]
        public async Task<IActionResult> AssessmentComplete(int id)
        {
            var assessment = await GetOwnedAssessmentAsync(id);
            if (assessment is null) return NotFound();

            return View(AssessmentViewModel.FromEntity(assessment));
        }


      
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DismissFraudFlag(int fraudFlagId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return BadRequest(new { success = false, message = "A reason is required to dismiss a fraud flag." });

            var flag = await _db.FraudFlags
                .Include(f => f.LoanAssessment).ThenInclude(a => a.LoanApplication)
                .FirstOrDefaultAsync(f => f.FraudFlagId == fraudFlagId);

            if (flag is null) return NotFound();

            var branchId = await GetCurrentBranchIdAsync();
            if (flag.LoanAssessment.LoanApplication.BranchId != branchId) return Forbid();

            flag.Dismissed = true;
            flag.DismissReason = reason;

            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decide(int loanAssessmentId, LoanApplicationStatus decision, string? notes)
        {
            var allowedDecisions = new[] { LoanApplicationStatus.Approved, LoanApplicationStatus.Rejected };
            if (!allowedDecisions.Contains(decision))
                return BadRequest(new { success = false, message = "Invalid decision status." });

            var assessment = await _db.LoanAssessments
                .Include(a => a.LoanApplication)
                .Include(a => a.FraudFlags)
                .FirstOrDefaultAsync(a => a.LoanAssessmentId == loanAssessmentId);
            // ← dropped .Include(a => a.CollateralVerifications), no longer needed here

            if (assessment is null) return NotFound();

            var branchId = await GetCurrentBranchIdAsync();
            if (assessment.LoanApplication.BranchId != branchId) return Forbid();

            if (decision == LoanApplicationStatus.Approved || decision == LoanApplicationStatus.Disbursed)
            {
                if (assessment.AnyUnresolvedCriticalFlags)
                    ModelState.AddModelError(string.Empty, "There are unresolved critical fraud flags on this application.");

                if (!assessment.StillMeetsCreditThreshold)
                    ModelState.AddModelError(string.Empty, "The applicant no longer meets the credit score threshold.");

                // AllCollateralVerified check removed — collateral is now physically
                // verified in person by staff, not re-checked through the system.
            }

            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    success = false,
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            assessment.Decision = decision;
            assessment.DecisionNotes = notes;
            assessment.CompletedOn = DateTime.UtcNow;
            assessment.LoanApplication.Status = decision;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(AssessmentComplete), new { id = loanAssessmentId });
        }


        //History
        private static readonly LoanApplicationStatus[] HistoryStatuses =
        {
            LoanApplicationStatus.Approved,
            LoanApplicationStatus.Rejected,
            LoanApplicationStatus.Disbursed
        };

        // ── History: applications this branch has already decided on, filterable by status ──
        [HttpGet]
        public async Task<IActionResult> History(LoanApplicationStatus? status)
        {
            var branchId = await GetCurrentBranchIdAsync();

            var query = _db.LoanApplications
                .Where(a => a.BranchId == branchId && HistoryStatuses.Contains(a.Status));

            if (status.HasValue && HistoryStatuses.Contains(status.Value))
            {
                query = query.Where(a => a.Status == status.Value);
            }

            var applications = await query
                .OrderByDescending(a => a.SubmittedOn)
                .ToListAsync();

            ViewBag.SelectedStatus = status;
            return View(applications);
        }

        // ── Read-only view of a past decision — reuses the AssessmentComplete view ──
        [HttpGet]
        public async Task<IActionResult> HistoryDetails(int id) // id = LoanApplicationId
        {
            var branchId = await GetCurrentBranchIdAsync();

            var assessment = await _db.LoanAssessments
                .Include(a => a.LoanApplication)
                .Include(a => a.FraudFlags)
                .Where(a => a.LoanApplicationId == id && a.CompletedOn != null)
                .OrderByDescending(a => a.CompletedOn)
                .FirstOrDefaultAsync();

            if (assessment is null) return NotFound();
            if (assessment.LoanApplication.BranchId != branchId) return Forbid();

            return View("AssessmentComplete", AssessmentViewModel.FromEntity(assessment));
        }


        // ── Settled loans at this owner's branch (UC-05, shop-owner view) ──
        [HttpGet]
        public async Task<IActionResult> SettledLoans()
        {
            var branchId = await GetCurrentBranchIdAsync();

            var loans = await _db.Loans
                .Include(l => l.LoanApplication).ThenInclude(a => a.Customer)
                .Include(l => l.Payments)
                .Where(l => l.LoanApplication.BranchId == branchId && l.Status == LoanRepaymentStatus.Settled)
                .OrderByDescending(l => l.SettledOn)
                .ToListAsync();

            return View(loans);
        }
       
        // ── NEW: active loans still being paid off, with progress ──
        [HttpGet]
        public async Task<IActionResult> ActiveLoans()
        {
            var branchId = await GetCurrentBranchIdAsync();

            var loans = await _db.Loans
                .Include(l => l.LoanApplication).ThenInclude(a => a.Customer)
                .Include(l => l.Payments)
                .Where(l => l.LoanApplication.BranchId == branchId && l.Status == LoanRepaymentStatus.Active)
                .OrderBy(l => l.DueDate)
                .ToListAsync();

            return View(loans);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCollateralCollected(int loanId, string collectionTokenInput)
        {
            var branchId = await GetCurrentBranchIdAsync();

            var loan = await _db.Loans
                .Include(l => l.LoanApplication)
                .FirstOrDefaultAsync(l => l.LoanId == loanId);

            if (loan == null) return NotFound();
            if (loan.LoanApplication.BranchId != branchId) return Forbid();
            if (loan.Status != LoanRepaymentStatus.Settled)
            {
                TempData["Error"] = "This loan hasn't been settled yet.";
                return RedirectToAction(nameof(SettledLoans));
            }

            if (!string.Equals(loan.CollectionToken, collectionTokenInput?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "The code doesn't match this loan. Ask the customer to show their QR code again.";
                return RedirectToAction(nameof(SettledLoans));
            }

            if (!loan.CollateralCollected)
            {
                loan.CollateralCollected = true;
                loan.CollateralCollectedOn = DateTime.UtcNow;
                loan.CollectedByOwnerId = GetCurrentOwnerId();
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(SettledLoans));
        }
    }
}