using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.ServicesMods;
using SkyTech.Models.SkytechWorkers;
using SkyTech.Models.ViewModels;
using SkyTech.Services;

namespace SkyTech.Controllers
{
  
    [AuthorizeUser]
    public class LoanApplicationController : Controller
    {
        private static readonly string[] AllowedProofOfIncomeExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedPhotoExtensions = { ".jpg", ".jpeg", ".png" };
        private const long MaxUploadBytes = 5 * 1024 * 1024; // 5MB

        private const int GoodCreditScoreThreshold = 600;

        private readonly ApplicationDbContext _db;
        private readonly ICollateralImageAnalysisService _collateralAnalysisService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<LoanApplicationController> _logger;

        public LoanApplicationController(
            ApplicationDbContext db,
            ICollateralImageAnalysisService collateralAnalysisService,
            IWebHostEnvironment env,
            ILogger<LoanApplicationController> logger)
        {
            _db = db;
            _collateralAnalysisService = collateralAnalysisService;
            _env = env;
            _logger = logger;
        }

        // ---------- Step 1-2: Application form ----------

        //[HttpGet]
        //public IActionResult Apply()
        //{
        //    return View(new LoanApplyViewModel());
        //}

        [HttpGet]
        public async Task<IActionResult> Apply()
        {
            var model = new LoanApplyViewModel
            {
                AvailableBranches = await _db.Branches
                    .Select(b => new SelectListItem
                    {
                        Value = b.BranchId.ToString(),
                        Text = b.BranchName // ASSUMPTION: adjust to the actual display property on Branch
                    })
                    .ToListAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(LoanApplyViewModel model)
        {
            //branch selection
            if (!ModelState.IsValid)
            {
                model.AvailableBranches = await _db.Branches
                    .Select(b => new SelectListItem { Value = b.BranchId.ToString(), Text = b.BranchName })
                    .ToListAsync();
                return View(model);
            }


            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var fileError = ValidateUpload(model.ProofOfIncomeDoc, AllowedProofOfIncomeExtensions);
            if (fileError != null)
            {
                ModelState.AddModelError(nameof(model.ProofOfIncomeDoc), fileError);
                return View(model);
            }

            int? customerId = HttpContext.Session.GetInt32("UserId");
            var proofPath = await SaveUploadAsync(model.ProofOfIncomeDoc, "proof-of-income");

            var application = new LoanApplication
            {
                CustomerId = (int)customerId!,
                NationalId = model.NationalId.Trim(),
                RequestedAmount = model.RequestedAmount,
                MonthlySalary = model.MonthlySalary,
                ProofOfIncomeDocPath = proofPath,
                Purpose = model.Purpose,
                IsEmployed = model.IsEmployed,
                Status = LoanApplicationStatus.Started,
                BranchId = model.BranchId

            };

            _db.LoanApplications.Add(application);
            await _db.SaveChangesAsync();

            // ---------- Step 3: Eligibility check ---------
            await RunEligibilityCheckAsync(application);

            return RedirectToAction(nameof(EligibilityLoading), new
            {
                id = application.LoanApplicationId
            });
        }

        // ---------- Step 4: Display eligibility result ----------
        [HttpGet]
        public async Task<IActionResult> EligibilityLoading(int id)
        {
            var application = await GetOwnedApplicationAsync(id);

            if (application == null)
                return NotFound();

            return View(application);
        }

        [HttpGet]
        public async Task<IActionResult> ShowEligibilityResult(int id)
        {
            var application = await GetOwnedApplicationAsync(id);
            if (application == null) return NotFound();

            var vm = new EligibilityResultViewModel
            {
                LoanApplicationId = application.LoanApplicationId,
                EligibilityResult = application.EligibilityResult,
                CreditScore = application.CreditScoreAtApplication,
                ClearanceStatus = application.ClearanceStatusAtApplication,
                RequestedAmount = application.RequestedAmount
            };

            return View("EligibilityResult", vm);
        }

        // ---------- Step 5: Upload collateral photos ----------

        [HttpGet]
        public async Task<IActionResult> UploadCollateral(int id)
        {
            var application = await GetOwnedApplicationAsync(id, includeCollateral: true);
            if (application == null) return NotFound();
            if (application.EligibilityResult != EligibilityResult.Eligible)
            {
                return RedirectToAction(nameof(ShowEligibilityResult), new { id });
            }

            var vm = new UploadCollateralViewModel
            {
                LoanApplicationId = application.LoanApplicationId,
                RequestedAmount = application.RequestedAmount,
                ExistingItems = application.CollateralItems.ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadCollateral(int id, UploadCollateralViewModel model)
        {
            var application = await GetOwnedApplicationAsync(id, includeCollateral: true);
            if (application == null) return NotFound();
            if (application.EligibilityResult != EligibilityResult.Eligible)
            {
                return RedirectToAction(nameof(ShowEligibilityResult), new { id });
            }

            if (model.NewItems == null || model.NewItems.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Add at least one collateral item.");
            }

            if (!ModelState.IsValid)
            {
                model.LoanApplicationId = application.LoanApplicationId;
                model.RequestedAmount = application.RequestedAmount;
                model.ExistingItems = application.CollateralItems.ToList();
                return View(model);
            }

            //foreach (var item in model.NewItems!)
            //{
            //    var fileError = ValidateUpload(item.Photo, AllowedPhotoExtensions);
            //    if (fileError != null)
            //    {
            //        ModelState.AddModelError(string.Empty, $"{item.Description}: {fileError}");
            //        continue;
            //    }

            //    var imagePath = await SaveUploadAsync(item.Photo, "collateral");

            //    var collateralItem = new CollateralItem
            //    {
            //        LoanApplicationId = application.LoanApplicationId,
            //        Description = item.Description,
            //        DeclaredValue = item.DeclaredValue,
            //        ImagePath = imagePath,
            //        SerialNumber = item.SerialNumber,
            //        Condition = item.Condition,
            //        AiScanResult = AiScanResult.Pending,
            //        UploadedOn = DateTime.UtcNow
            //    };

            //    _db.CollateralItems.Add(collateralItem);
            //}

            foreach (var item in model.NewItems!)
            {
                var fileError = ValidateUpload(item.Photo, AllowedPhotoExtensions);
                if (fileError != null)
                {
                    ModelState.AddModelError(string.Empty, $"{item.Description}: {fileError}");
                    continue;
                }

                var imagePath = await SaveUploadAsync(item.Photo, "collateral");

                // Compute perceptual hash for duplicate-collateral fraud detection.
                string? perceptualHash = null;
                try
                {
                    using var stream = new MemoryStream();
                    await item.Photo.CopyToAsync(stream);
                    perceptualHash = ImageHashHelper.ComputeAverageHash(stream.ToArray());
                }
                catch (Exception ex)
                {
                    // Don't block the upload if hashing fails for some reason (e.g.
                    // a corrupt image ImageSharp can't parse) — just skip the
                    // duplicate check for this one item rather than failing the whole
                    // application.
                    //_logger?.LogWarning(ex, "Could not compute perceptual hash for collateral photo.");
                    _logger.LogWarning(ex, "Could not compute perceptual hash for collateral photo.");
                }

                var collateralItem = new CollateralItem
                {
                    LoanApplicationId = application.LoanApplicationId,
                    Description = item.Description,
                    DeclaredValue = item.DeclaredValue,
                    ImagePath = imagePath,
                    SerialNumber = item.SerialNumber,
                    Condition = item.Condition,
                    AiScanResult = AiScanResult.Pending,
                    UploadedOn = DateTime.UtcNow,
                    ImagePerceptualHash = perceptualHash
                };

                _db.CollateralItems.Add(collateralItem);
            }

            if (!ModelState.IsValid)
            {
                model.LoanApplicationId = application.LoanApplicationId;
                model.RequestedAmount = application.RequestedAmount;
                model.ExistingItems = application.CollateralItems.ToList();
                return View(model);
            }

            application.Status = LoanApplicationStatus.AwaitingCollateral;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(ScanCollateral), new
            {
                id = application.LoanApplicationId
            });
        }

        [HttpGet]
        public async Task<IActionResult> ScanCollateral(int id)
        {
            var application = await GetOwnedApplicationAsync(
                id,
                includeCollateral: true);

            if (application == null)
                return NotFound();

            if (application.EligibilityResult !=
                EligibilityResult.Eligible)
            {
                return RedirectToAction(
                    nameof(ShowEligibilityResult),
                    new { id });
            }

            return View(application);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartCollateralScan(int id)
        {
            var application = await GetOwnedApplicationAsync(
                id,
                includeCollateral: true);

            if (application == null)
                return NotFound();

            if (application.EligibilityResult !=
                EligibilityResult.Eligible)
            {
                return BadRequest();
            }

            await RunCollateralScansAsync(
                application.LoanApplicationId);

            return Json(new
            {
                success = true
            });
        }

        // ---------- Step 7: Review AI verdicts, then submit ----------

        [HttpGet]
        public async Task<IActionResult> ReviewScan(int id)
        {
            var application = await GetOwnedApplicationAsync(id, includeCollateral: true);
            if (application == null) return NotFound();

            var vm = new ReviewScanViewModel
            {
                LoanApplicationId = application.LoanApplicationId,
                RequestedAmount = application.RequestedAmount,
                TotalDeclaredCollateralValue = application.TotalDeclaredCollateralValue,
                Items = application.CollateralItems.ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id)
        {
            var application = await GetOwnedApplicationAsync(id, includeCollateral: true);
            if (application == null) return NotFound();

            if (application.CollateralItems.Count == 0)
            {
                TempData["SubmitError"] = "Add at least one collateral item before submitting.";
                return RedirectToAction(nameof(UploadCollateral), new { id });
            }

            if (application.CollateralItems.Any(c => c.AiScanResult == AiScanResult.Pending))
            {
                TempData["SubmitError"] = "Some collateral items are still being scanned. Please wait and try again.";
                return RedirectToAction(nameof(ReviewScan), new { id });
            }

            application.Status = LoanApplicationStatus.Submitted;
            application.SubmittedOn = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Confirmation), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var application = await GetOwnedApplicationAsync(id, includeCollateral: true);
            if (application == null) return NotFound();

            return View(application);
        }

        // ---------- Internal helpers ----------

        private async Task RunEligibilityCheckAsync(LoanApplication application)
        {
            var creditRecord = await _db.CreditRecords.FindAsync(application.NationalId);
            var criminalRecord = await _db.CriminalRecords.FindAsync(application.NationalId);

            var creditScore = creditRecord?.CreditScore ?? 0;
            var clearanceStatus = criminalRecord?.ClearanceStatus ?? ClearanceStatus.UnderReview;

            application.CreditScoreAtApplication = creditScore;
            application.ClearanceStatusAtApplication = clearanceStatus;

            var isEmployed = application.IsEmployed;
            var hasGoodCredit = creditScore >= GoodCreditScoreThreshold;
            var isClear = clearanceStatus == ClearanceStatus.Clear;

            var passes = isEmployed && hasGoodCredit && isClear;

            application.EligibilityResult = passes ? EligibilityResult.Eligible : EligibilityResult.NotEligible;
            application.Status = passes ? LoanApplicationStatus.AwaitingCollateral : LoanApplicationStatus.Rejected;

            await _db.SaveChangesAsync();
        }

        private async Task RunCollateralScansAsync(int loanApplicationId)
        {
            var pendingItems = await _db.CollateralItems
                .Where(c => c.LoanApplicationId == loanApplicationId && c.AiScanResult == AiScanResult.Pending)
                .ToListAsync();

            foreach (var item in pendingItems)
            {
                var fullPath = Path.Combine(_env.WebRootPath, item.ImagePath.TrimStart('/', '\\'));
                if (!System.IO.File.Exists(fullPath))
                {
                    item.AiScanResult = AiScanResult.Pending;
                    item.AiScanNotes = "Image file could not be located for scanning.";
                    continue;
                }

                var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                var contentType = GetContentType(fullPath);

                var result = await _collateralAnalysisService.AnalyzeAsync(new CollateralImageAnalysisRequest
                {
                    Description = item.Description,
                    ImageBytes = bytes,
                    ImageContentType = contentType,
                    DeclaredValue = item.DeclaredValue,
                    Condition = item.Condition,
                    SerialNumber = item.SerialNumber
                });

                item.AiScanResult = result.Verdict;
                item.AiConfidencePercent = result.Confidence ?? 0;
                item.AiMatchedCategory = result.MatchedCategory;
                item.AiScanNotes = result.Notes;
            }

            await _db.SaveChangesAsync();
        }

        private async Task<LoanApplication?> GetOwnedApplicationAsync(int id, bool includeCollateral = false)
        {
            int? customerId = HttpContext.Session.GetInt32("UserId");

            IQueryable<LoanApplication> query = _db.LoanApplications;
            if (includeCollateral)
            {
                query = query.Include(a => a.CollateralItems);
            }

            return await query.FirstOrDefaultAsync(a => a.LoanApplicationId == id && a.CustomerId == customerId);
        }

        private static string? ValidateUpload(IFormFile file, string[] allowedExtensions)
        {
            if (file == null || file.Length == 0)
            {
                return "A file is required.";
            }

            if (file.Length > MaxUploadBytes)
            {
                return "File exceeds the 5MB size limit.";
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                return $"Unsupported file type. Allowed: {string.Join(", ", allowedExtensions)}.";
            }

            return null;
        }

        private async Task<string> SaveUploadAsync(IFormFile file, string subfolder)
        {
            var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", subfolder);
            Directory.CreateDirectory(uploadsRoot);

            var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fullPath = Path.Combine(uploadsRoot, safeFileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Path stored relative to wwwroot, with forward slashes for URL use.
            return $"/uploads/{subfolder}/{safeFileName}";
        }

        private static string GetContentType(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };
        }
    }
}