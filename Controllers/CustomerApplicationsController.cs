using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyTech.JobPostServices;
using SkyTech.Models;
using SkyTech.Models.Interviews;
using SkyTech.Models.JobPostServices;
using SkyTech.ViewModels;
using SkyTech.Models.SkytechWorkers;
using SkyTech.Services;
//using SkyTech.Models.ServiceMods

namespace SkyTech.Controllers
{
    [AuthorizeUser]
    public class CustomerApplicationsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly ICvExtractionService _cvExtractionService;
        private readonly ISkillMatchService _skillMatchService;
        private readonly EmailServices _emailServices;
        public CustomerApplicationsController(ApplicationDbContext context, ISkillMatchService skillMatchService, ICvExtractionService cvExtractionService, EmailServices emailServices)
        {
            db = context ?? throw new ArgumentNullException(nameof(context));
            _cvExtractionService = cvExtractionService;
            _skillMatchService = skillMatchService;
            _emailServices = emailServices;
        }
        public async Task<IActionResult> Careers()
        {
            var listOfJobs = await db.JobPosts
                .ToListAsync();
            return View(listOfJobs);

        }


        [HttpGet]
        public async Task<IActionResult> StartApplication(int postId)
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");

            var job = await db.JobPosts.FindAsync(postId);
            if (job == null)
                return NotFound();

            return View(job);
        }

        [HttpPost]
        public async Task<IActionResult> UploadCv(int postId, IFormFile cvFile)
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");

            var job = await db.JobPosts.FindAsync(postId);
            if (job == null) return NotFound();

            if (cvFile == null || cvFile.Length == 0)
            {
                ModelState.AddModelError("", "Please select a CV file to upload.");
                return View("StartApplication", job);
            }

            var extension = Path.GetExtension(cvFile.FileName).ToLowerInvariant();
            if (extension != ".pdf" && extension != ".docx")
            {
                ModelState.AddModelError("", "Only PDF or DOCX files are supported.");
                return View("StartApplication", job);
            }

            string cvText;
            try
            {
                cvText = CvTextExtractor.ExtractText(cvFile);
            }
            catch
            {
                ModelState.AddModelError("", "We couldn't read that file. Please try a different CV.");
                return View("StartApplication", job);
            }

            CvExtractionResult extracted;
            try
            {
                extracted = await _cvExtractionService.ExtractFromTextAsync(cvText);
            }
            catch
            {
                // AI extraction failed — fall back to a blank form so the applicant
                // can still type everything in manually rather than being blocked
                extracted = new CvExtractionResult();
                TempData["cvError"] = "We couldn't auto-fill your details — please enter them manually.";
            }

            var applicationForm = new ApplicationForm
            {
                PostId = postId,
                FirstName = extracted.FirstName,
                LastName = extracted.LastName,
                PhoneNumber = extracted.PhoneNumber,
                IDNumber = extracted.IDNumber,
                Email = extracted.Email,
                Location = extracted.Location,
                Experience = extracted.Experience,
                Skills = extracted.Skills,
                Certificates = extracted.Certificates,
                Links = extracted.Links


            };

            return View("ReviewApplication", applicationForm);
        }

        public IActionResult ReviewApplication()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitApplication(ApplicationForm model)
        {
            int? currentId = HttpContext.Session.GetInt32("UserId");

            if (currentId == null)
            {
                return RedirectToAction("Login", "Authentication");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                return Content(string.Join("\n", errors));
            }

            // Get the actual job from the database
            var job = await db.JobPosts.FindAsync(model.PostId);

            if (job == null)
            {
                return NotFound();
            }

            // Make sure the applicant actually entered skills
            if (string.IsNullOrWhiteSpace(model.Skills))
            {
                TempData["ApplicationError"] =
                    "Please enter your skills before submitting your application.";

                return RedirectToAction(
                    "UploadCv",
                    "CustomerApplications",
                    new { id = model.PostId });
            }

            // Make sure the job has requirements
            if (string.IsNullOrWhiteSpace(job.RequiredSkillsAndXP))
            {
                TempData["ApplicationError"] =
                    "This job does not currently have any skill requirements configured.";

                return RedirectToAction(
                    "UploadCv",
                    "CustomerApplications",
                    new { id = model.PostId });
            }

            // ==========================================
            // AI SKILL MATCHING
            // ==========================================

            SkillMatchResult matchResult;

            try
            {
                matchResult = await _skillMatchService.MatchSkillsAsync(
                    job.RequiredSkillsAndXP,
                    model.Skills
                );
            }
            catch (Exception ex)
            {
                // Log the actual error
                Console.WriteLine($"Skill matching failed: {ex.Message}");

                TempData["ApplicationError"] =
                    "We could not verify your skills at the moment. Please try again.";

                return RedirectToAction(
                    "UploadCv",
                    "CustomerApplications",
                    new { id = model.PostId });
            }

            // ==========================================
            // MATCH COUNT — informational only.
            // HR makes the Shortlist/Reject call, not this code.
            // ==========================================

            int matchCount = matchResult.MatchedSkills
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            model.JobStat = JobApplicationStat.Applied;
            model.MatchedSkillCount = matchCount;

            // ==========================================
            // COPY JOB INFORMATION
            // ==========================================

            model.CustomerId = currentId.Value;

            model.JobTitle = job.JobTitle;
            model.Department = job.Department;
            model.EmploymentType = job.EmploymentType;
            model.SalaryMin = job.SalaryMin;
            model.SalaryMax = job.SalaryMax;

            // ==========================================
            // SAVE APPLICATION
            // ==========================================

            db.ApplicationForms.Add(model);

            await db.SaveChangesAsync();

            // =====================================================
            // SEND EMAIL — applicant is always Applied now, so this
            // is always the "accepted for review" email, not a rejection.
            // =====================================================

            try
            {
                _emailServices.JobApplicationAccepted(
                    model.Email,
                    model.FirstName);
            }
            catch (Exception ex)
            {
                // Don't delete/reject the application just because the email failed.
                Console.WriteLine($"Email failed: {ex.Message}");
            }

            // ==========================================
            // MESSAGE TO APPLICANT
            // ==========================================

            TempData["successful"] =
                $"Your application has been submitted successfully. " +
                $"You matched {matchCount} required skills.";

            return RedirectToAction(
                "ViewAppliations",
                "CustomerApplications");
        }

        public async Task<IActionResult> ViewAppliations()
        {
            int? currentId = HttpContext.Session.GetInt32("UserId");

            var Applications = await db.ApplicationForms
                .Where(p => p.CustomerId == currentId)
                .ToListAsync();

            return View(Applications);
        }


        //===Interview====
        private int? CurrentCustomerId
        {
            get
            {
                var userType = HttpContext.Session.GetString("UserType");
                if (userType != "Customer") return null;
                return HttpContext.Session.GetInt32("UserId");
            }
        }

        public async Task<IActionResult> MyStatus()
        {
            var customerId = CurrentCustomerId;
            if (customerId == null) return RedirectToAction("LogIn", "Authentication");

            var test = await db.ApplicationForms
                .Include(c => c.TestResults)
                .Where(t => t.CustomerId == customerId)
                .OrderByDescending(t => t.TestResults.DateWritten)
                .FirstOrDefaultAsync();

            var interview = await db.Interviews
                .Where(i => i.CustomerId == customerId)
                .OrderByDescending(i => i.ScheduledDate)
                .FirstOrDefaultAsync();

            ViewBag.Test = test;
            ViewBag.Interview = interview;

            return View();
        }

        public async Task<IActionResult> MyInterview()
        {
            var customerId = CurrentCustomerId;
            if (customerId == null) return RedirectToAction("LogIn", "Authentication");

            var interview = await db.Interviews
                .Where(i => i.CustomerId == customerId)
                .OrderByDescending(i => i.ScheduledDate)
                .FirstOrDefaultAsync();

            if (interview == null)
            {
                TempData["Info"] = "You don't have an interview scheduled yet.";
                return RedirectToAction("CustomerDash", "Customer");
            }

            return View(interview);
        }



        public async Task<IActionResult> JoinMeeting(int id)
        {
            var customerId = CurrentCustomerId;
            if (customerId == null) return RedirectToAction("LogIn", "Authentication");

            var interview = await db.Interviews
                .FirstOrDefaultAsync(i => i.InterviewId == id && i.CustomerId == customerId);

            if (interview == null) return Forbid();
            return View(interview);
        }


        public async Task<IActionResult> MyContracts()
        {
            var customerId = HttpContext.Session.GetInt32("UserId");

            var contracts = await db.Contracts
                .Include(c => c.ApplicationForm)
                .Include(c => c.JobPost)
                .Where(c => c.ApplicationForm.CustomerId == customerId
                         && c.ContractStat != ContractStatus.Draft)
                .Select(c => new CandidateContractViewModel
                {
                    ContractId = c.ContractId,

                    ApplicationId = c.ApplicationId,

                    CandidateName = c.ApplicationForm.FirstName + " " +
                                    c.ApplicationForm.LastName,

                    Email = c.ApplicationForm.Email,

                    JobTitle = c.JobPost.JobTitle,

                    JobDescription = c.JobPost.Responsibilities,

                    ContractTerms = c.JobPost.ContractTerms,

                    EmploymentType = c.JobPost.EmploymentType,

                    DateSent = c.DateSent,

                    ContractStat = c.ContractStat
                })
                .ToListAsync();

            return View(contracts);
        }

        public async Task<IActionResult> SignContract(int id)
        {
            var contract = await db.Contracts

                .Include(c => c.ApplicationForm)

                .Include(c => c.JobPost)

                .FirstOrDefaultAsync(c => c.ContractId == id);

            if (contract == null)
                return NotFound();

            CandidateContractViewModel model = new CandidateContractViewModel
            {
                ContractId = contract.ContractId,

                ApplicationId = contract.ApplicationId,

                CandidateName =
                    contract.ApplicationForm.FirstName + " " +
                    contract.ApplicationForm.LastName,

                Email = contract.ApplicationForm.Email,

                JobTitle = contract.JobPost.JobTitle,

                JobDescription = contract.JobPost.Responsibilities,

                ContractTerms = contract.JobPost.ContractTerms,

                EmploymentType = contract.JobPost.EmploymentType,

                DateSent = contract.DateSent,

                ContractStat = contract.ContractStat
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignContract(CandidateContractViewModel model)
        {
            var customerId = HttpContext.Session.GetInt32("UserId");

            var contract = await db.Contracts
                .Include(c => c.ApplicationForm)
                .FirstOrDefaultAsync(c =>
                    c.ContractId == model.ContractId &&
                    c.ApplicationForm.CustomerId == customerId);

            if (contract == null)
            {
                return NotFound();
            }

            // Prevent signing an already signed contract
            if (contract.ContractStat != ContractStatus.Sent)
            {
                TempData["Error"] = "This contract cannot be signed.";
                return RedirectToAction(nameof(MyContracts));
            }

            if (!model.AgreeToTerms)
            {
                ModelState.AddModelError(
                    "AgreeToTerms",
                    "You must agree to the terms and conditions.");

                await LoadContractForView(model);
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.CandidateSignature))
            {
                ModelState.AddModelError(
                    "CandidateSignature",
                    "Please provide your signature.");

                await LoadContractForView(model);
                return View(model);
            }

            contract.CandidateSignature = model.CandidateSignature;
            contract.CandidateSignedOn = DateTime.Now;
            contract.ContractStat = ContractStatus.SignedByCandidate;

            // ---- Create the Staff record now that the candidate has signed ----
            var jobPost = await db.JobPosts
                .Include(jp => jp.ShopOwner)
                    .ThenInclude(o => o.Branch)
                .FirstOrDefaultAsync(jp => jp.PostId == contract.PostId);

            string generatedUsername = null;
            string generatedPassword = null;

            if (jobPost?.ShopOwner?.BranchId != null)
            {
                var staffRole = await db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Staff");
                if (staffRole == null)
                {
                    staffRole = new Role { RoleName = "Staff" };
                    db.Roles.Add(staffRole);
                    await db.SaveChangesAsync();
                }

                var (username, password) = await GenerateStaffCredentials(
                    contract.ApplicationForm.FirstName, contract.ApplicationForm.LastName);

                generatedUsername = username;
                generatedPassword = password;

                var newStaff = new Staff
                {
                    FirstName = contract.ApplicationForm.FirstName,
                    LastName = contract.ApplicationForm.LastName,
                    Email = contract.ApplicationForm.Email,
                    PhoneNumber = contract.ApplicationForm.PhoneNumber,
                    Username = username,
                    Password = password,
                    ConfirmPassword = password,
                    OwnerId = jobPost.OwnerId,
                    BranchId = jobPost.ShopOwner.BranchId,
                    PositionId = jobPost.PositionId,
                    RoleId = staffRole.RoleId,
                    EmploymentType = jobPost.EmploymentType,
                    StaffAvailability = IsTheStaffMemberAvailable.Active,
                    UserType = "Staff",
                    DateEmployed = DateTime.Now
                };

                db.Staffs.Add(newStaff);
            }
            else
            {
                TempData["StaffCreationWarning"] =
                    "Contract signed, but the shop owner for this post has no branch assigned, so a staff account wasn't created automatically.";
            }

            // Save first — only email real, persisted credentials
            await db.SaveChangesAsync();

            // ---- Send the welcome email with login credentials, now that the Staff row is confirmed saved ----
            if (generatedUsername != null && generatedPassword != null)
            {
                try
                {
                    _emailServices.StaffAccountCreated(
                        contract.ApplicationForm.Email,
                        contract.ApplicationForm.FirstName,
                        generatedUsername,
                        generatedPassword);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Staff welcome email failed: {ex.Message}");
                    TempData["StaffCreationWarning"] =
                        "Staff account created, but the welcome email could not be sent.";
                }
            }

            TempData["Success"] = "Your contract has been signed successfully.";
            return RedirectToAction(nameof(MyContracts));
        }

        //Helper method for adding staff automatically
        private async Task<(string username, string password)> GenerateStaffCredentials(string firstName, string lastName)
        {
            var rng = new Random();
            string baseName = $"{firstName}.{lastName}".ToLower().Replace(" ", "");

            string username;
            do
            {
                username = $"{baseName}{rng.Next(100, 999)}@skytechstaff";
            }
            while (await db.Staffs.AnyAsync(s => s.Username == username));

            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
            string password = new string(Enumerable.Range(0, 12)
                .Select(_ => chars[rng.Next(chars.Length)]).ToArray());

            return (username, password);
        }
        //Because the POST can return the view when validation fails, we need to reload the contract information so the page doesn't lose the Job Title, Description, Terms, etc.
        private async Task LoadContractForView(CandidateContractViewModel model)
        {
            var contract = await db.Contracts
                .Include(c => c.ApplicationForm)
                .Include(c => c.JobPost)
                .FirstOrDefaultAsync(c =>
                    c.ContractId == model.ContractId);

            if (contract == null)
                return;

            model.ApplicationId = contract.ApplicationId;

            model.CandidateName =
                contract.ApplicationForm.FirstName + " " +
                contract.ApplicationForm.LastName;

            model.Email =
                contract.ApplicationForm.Email;

            model.JobTitle =
                contract.JobPost.JobTitle;

            model.JobDescription =
                contract.JobPost.Responsibilities;

            model.ContractTerms =
                contract.JobPost.ContractTerms;

            model.EmploymentType =
                contract.JobPost.EmploymentType;

            model.DateSent =
                contract.DateSent;

            model.ContractStat =
                contract.ContractStat;
        }

    }
}