using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.JobPostServices;
using SkyTech.Services;
using SkyTech.ViewModels;


namespace SkyTech.Controllers
{
    [AuthorizeUser]
    public class HRController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly EmailServices _services;
        private readonly IAIJobPostDraftService _aiService;
        private readonly IWebHostEnvironment _webHostEnvironment;



        public HRController(ApplicationDbContext context, EmailServices services, IAIJobPostDraftService aiService, IWebHostEnvironment webHostEnvironment)
        {
            db = context;
            _services = services;
            _aiService = aiService;
            _webHostEnvironment = webHostEnvironment;
        }


        //==============================HR POSTING AND REVIEWING JOBS==============================
        public IActionResult JobCenter()
        {
            return View("ManagingJobs/JobCenter");
        }

        public async Task<IActionResult> ComposeJob()
        {
            var owners = await db.shopowners
                .Include(o => o.Branch)
                .Where(o => o.ApplicationStatus == ApplicationStat.Approved)
                .ToListAsync();

            ViewBag.Owners = new SelectList(
                owners.Select(o => new { o.OwnerId, Display = $"{o.Names} {o.Surname} — {o.Branch?.BranchName ?? "No Branch Assigned"}" }),
                "OwnerId", "Display");

            var positions = await db.StaffPositions
                .OrderBy(p => p.PositionName)
                .ToListAsync();

            ViewBag.JobTitles = new SelectList(positions, "PositionId", "PositionName"); // ← value is now PositionId

            return View("ManagingJobs/ComposeJob");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ComposeJob(JobPost model)
        {
            int currentId = HttpContext.Session.GetInt32("UserId")!.Value;

            if (!ModelState.IsValid)
            {
                var owners = await db.shopowners
                    .Include(o => o.Branch)
                    .Where(o => o.ApplicationStatus == ApplicationStat.Approved)
                    .ToListAsync();

                ViewBag.Owners = new SelectList(
                    owners.Select(o => new { o.OwnerId, Display = $"{o.Names} {o.Surname} — {o.Branch?.BranchName ?? "No Branch Assigned"}" }),
                    "OwnerId", "Display");

                var positionsInvalid = await db.StaffPositions
                    .OrderBy(p => p.PositionName)
                    .ToListAsync();

                ViewBag.JobTitles = new SelectList(positionsInvalid, "PositionId", "PositionName");

                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Content(string.Join("\n", errors));
            }

            var position = await db.StaffPositions.FindAsync(model.PositionId);
            if (position == null)
            {
                ModelState.AddModelError("PositionId", "Please select a valid job title.");
                return Content("Invalid position selected.");
            }

            model.JobTitle = position.PositionName; // keep the denormalized text in sync
            model.HRId = currentId;
            model.PostStatus = CurrentPostStatus.Published;
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;

            db.Add(model);
            await db.SaveChangesAsync();
            TempData["successful"] = "Job Posted successfully";
            return RedirectToAction("JobCenter");
        }


        public async Task<IActionResult> ComposeJobAI(int roleId, int ownerId)
        {
            var role = await db.JobRoleKnowledges.FindAsync(roleId);
            if (role == null) return NotFound();

            var owner = await db.shopowners.Include(o => o.Branch).FirstOrDefaultAsync(o => o.OwnerId == ownerId);
            if (owner == null) return NotFound();

            ViewBag.RoleId = role.RoleId;
            ViewBag.OwnerId = owner.OwnerId;
            ViewBag.JobTitle = role.JobTitle;
            ViewBag.BranchName = owner.Branch?.BranchName ?? "No Branch Assigned";
            return View("ManagingJobs/ComposeJobAI");
        }


        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> GenerateJobDraft(int roleId, int ownerId, JobPost model)
        {
            int currentId = HttpContext.Session.GetInt32("UserId")!.Value;

            var role = await db.JobRoleKnowledges.FindAsync(roleId);
            if (role == null) return NotFound();

            var owner = await db.shopowners.FindAsync(ownerId);
            if (owner == null) return NotFound();

            var position = await db.StaffPositions
           .FirstOrDefaultAsync(p => p.PositionId == role.PositionId);

            if (position == null)
            {
                ModelState.AddModelError("", "Selected position does not exist.");
                return View(role);
            }
            JobPostDraftResult draft;
            try
            {
                draft = await _aiService.GenerateDraftFromRoleAsync(role);
            }
            catch
            {
                draft = new JobPostDraftResult
                {
                    Responsibilities = role.CompanyResponsibilities,
                    RequiredCerts = role.RequiredQualifications,
                    RequiredSkillsAndXP = role.RequiredSkills + "\n" + role.RequiredExperience
                };
                TempData["aiError"] = "AI formatting failed — showing the raw approved information instead.";
            }

            model.OwnerId = owner.OwnerId; // ← real, valid OwnerId from the dropdown selection
            model.HRId = currentId;
            model.PositionId = role.PositionId; // ← NEW
            model.JobTitle = position.PositionName;
            model.Responsibilities = draft.Responsibilities;
            model.RequiredCerts = draft.RequiredCerts;
            model.RequiredSkillsAndXP = draft.RequiredSkillsAndXP;
            model.PostStatus = CurrentPostStatus.Draft;
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;

            db.Add(model);
            await db.SaveChangesAsync();
            return RedirectToAction("ReviewJobDraft", new { id = model.PostId });
        }

        // Step 3: owner reviews/edits the AI draft
        [HttpGet]
        public async Task<IActionResult> ReviewJobDraft(int id)
        {
            var post = await db.JobPosts.FindAsync(id);
            if (post == null) return NotFound();
            return View("ManagingJobs/ReviewJobDraft", post);
        }

        // Step 4a: owner edits text then clicks Publish
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> PublishJobDraft(JobPost model)
        {
            var post = await db.JobPosts.FindAsync(model.PostId);
            if (post == null) return NotFound();

            post.Responsibilities = model.Responsibilities;
            post.RequiredCerts = model.RequiredCerts;
            post.RequiredSkillsAndXP = model.RequiredSkillsAndXP;
            post.PostStatus = CurrentPostStatus.Published;
            post.UpdatedAt = DateTime.Now;

            await db.SaveChangesAsync();
            TempData["successful"] = "Job Posted successfully";
            return RedirectToAction("JobCenter");
        }

        // Step 4b: owner isn't happy with the draft, asks AI to try again
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> RegenerateJobDraft(int id)
        {
            var post = await db.JobPosts.FindAsync(id);
            if (post == null) return NotFound();

            var draft = await _aiService.GenerateDraftAsync(new JobPostDraftRequest
            {
                JobTitle = post.JobTitle,
                Department = post.Department.ToString(),
                Location = post.Location.ToString(),
                EmploymentType = post.EmploymentType.ToString(),
                SalaryMin = post.SalaryMin,
                SalaryMax = post.SalaryMax
            });

            post.Responsibilities = draft.Responsibilities;
            post.RequiredCerts = draft.RequiredCerts;
            post.RequiredSkillsAndXP = draft.RequiredSkillsAndXP;
            post.UpdatedAt = DateTime.Now;

            await db.SaveChangesAsync();
            return RedirectToAction("ReviewJobDraft", new { id });
        }


        public async Task<IActionResult> SelectJobRole()
        {
            var roles = await db.JobRoleKnowledges
                .Where(r => r.IsActive)
                .OrderBy(r => r.JobTitle)
                .ToListAsync();

            var owners = await db.shopowners
                .Include(o => o.Branch)
                .Where(o => o.ApplicationStatus == ApplicationStat.Approved)
                .ToListAsync();

            ViewBag.Roles = new SelectList(roles, "RoleId", "JobTitle");
            ViewBag.Owners = new SelectList(
                owners.Select(o => new { o.OwnerId, Display = $"{o.Names} {o.Surname} — {o.Branch?.BranchName ?? "No Branch Assigned"}" }),
                "OwnerId", "Display");

            return View("ManagingJobs/SelectJobRole");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectJobRole(int roleId, int ownerId, string mode)
        {
            var role = await db.JobRoleKnowledges.FindAsync(roleId);
            if (role == null) return NotFound();

            var owner = await db.shopowners.FindAsync(ownerId);
            if (owner == null) return NotFound();

            var position = await db.StaffPositions
             .FirstOrDefaultAsync(p => p.PositionId == role.PositionId);

            if (position == null)
            {
                ModelState.AddModelError("", "Selected position does not exist.");
                return View(role);
            }

            if (mode == "manual")
            {
                var seeded = new JobPost
                {
                    JobTitle = position.PositionName,
                    PositionId = role.PositionId, // ← NEW
                    OwnerId = owner.OwnerId,
                    Responsibilities = role.CompanyResponsibilities,
                    RequiredCerts = role.RequiredQualifications,
                    RequiredSkillsAndXP = role.RequiredSkills + "\n" + role.RequiredExperience
                };
                return View("ManagingJobs/ComposeJob", seeded);
            }

            return RedirectToAction("ComposeJobAI", new { roleId = role.RoleId, ownerId = owner.OwnerId });
        }
        //============================HR managing job applicationd====================

        public IActionResult HRDash()
        {
            return View();
        }



        public async Task<IActionResult> ChangeStatus(int id, JobApplicationStat status)
        {
            var application = await db.ApplicationForms.FindAsync(id);
            if (application == null) return NotFound();

            application.JobStat = status;
            await db.SaveChangesAsync();

            if (status == JobApplicationStat.Rejected)
            {
                _services.RejectJobApplication(application.Email, application.FirstName);

            }
            else if (status == JobApplicationStat.Online_interview)
            {
                _services.OnlineInterview(application.Email, application.FirstName);

            }
            else
            {
                _services.PhysicalInterview(application.Email, application.FirstName);

            }
            TempData["Message"] = "Feedback Sent";
            return RedirectToAction("HRDash");
        }

        // ── ADD this action anywhere in HRController ──
        // Shows all staff (branch scoping deliberately deferred — see conversation
        // note: HR has no BranchId yet to scope by).

        public async Task<IActionResult> StaffDirectory()
        {
            var staffList = await db.Staffs
                .Include(s => s.Branches)
                .Include(s => s.Positions)
                .OrderBy(s => s.Branches.BranchName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            return View(staffList);
        }



        public async Task<IActionResult> JobApplications(int? jobId)
        {
            int? currentId = HttpContext.Session.GetInt32("UserId");

            // Get ALL submitted applications
            // and load the JobPost + Assessments + TestResult
            var jobApplications = await db.ApplicationForms
                .Include(af => af.JobPosts)
                    .ThenInclude(jp => jp.Assessments)
                .Include(af => af.TestResults)
                .ToListAsync();

            // Load interviews
            var interviews = await db.Interviews
                .ToListAsync();

            var viewModel = jobApplications.Select(af =>
            {
                // Find the interview belonging to THIS application's TestResult
                var interview = af.TestResults == null
                    ? null
                    : interviews.FirstOrDefault(i =>
                        i.ResultId == af.TestResults.ResultId);

                return new HRReviewViewModel
                {
                    ApplicationId = af.ApplicationId,
                    FirstName = af.FirstName,
                    Email = af.Email,
                    JobTitle = af.JobTitle,

                    Score = af.TestResults?.Score ?? 0,
                    Passed = af.TestResults?.Passed ?? false,

                    JobStat = af.JobStat,

                    // NEW — how many required skills this applicant matched at
                    // submission time. HR uses this to make the Shortlist/Reject
                    // call themselves; nothing is auto-rejected anymore.
                    MatchedSkillCount = af.MatchedSkillCount,

                    // Assessment belongs to the JobPost
                    IsPublished = af.JobPosts != null &&
                                  af.JobPosts.Assessments.Any(a => a.IsPublished),

                    // Interview belongs to the TestResult
                    // null means this applicant has no interview yet
                    InterviewStat = interview?.Status
                };
            }).ToList();

            return View(viewModel);
        }


        public IActionResult Confirm(int id)
        {
            var application = db.ApplicationForms
                .FirstOrDefault(a => a.ApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            // Cannot interview rejected applicants
            if (application.JobStat == JobApplicationStat.Rejected)
            {
                TempData["Message"] = "Rejected applicants cannot be invited for an interview.";
                return RedirectToAction("JobApplications");
            }

            // Applicant must be shortlisted first
            if (application.JobStat == JobApplicationStat.Applied)
            {
                TempData["Message"] = "Shortlist the applicant before inviting them for an interview.";
                return RedirectToAction("JobApplications");
            }

            // Check if this job has a published assessment
            bool hasAssessment = db.Assessments
                .Any(a => a.PostId == application.PostId && a.IsPublished);

            // If a quiz exists, applicant must pass it
            if (hasAssessment && application.JobStat != JobApplicationStat.Passed_Test)
            {
                TempData["Message"] =
                    "This vacancy requires an assessment. The applicant must pass before an interview can be scheduled.";

                return RedirectToAction("JobApplications");
            }

            application.JobStat = JobApplicationStat.Online_interview;
            db.SaveChanges();

            TempData["Message"] =
                $"Interview invitation sent to {application.Email}";

            return RedirectToAction("JobApplications");
        }



        // Shortlist applicant
        public IActionResult Shortlist(int id)
        {
            var application = db.ApplicationForms
                .FirstOrDefault(a => a.ApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            application.JobStat = JobApplicationStat.Shortlisted;

            db.SaveChanges();

            // =====================================================
            // SEND EMAIL BASED ON APPLICATION STATUS
            // =====================================================

            try
            {
                _services.JobApplicationShortlisted(
                    application.Email,
                    application.FirstName);
            }
            catch (Exception ex)
            {
                // Don't delete/reject the application just because the email failed.
                Console.WriteLine($"Email failed: {ex.Message}");
            }


            return RedirectToAction("JobApplications");
        }


        // Reject applicant
        public IActionResult Reject(int id)
        {
            var application = db.ApplicationForms
                .FirstOrDefault(a => a.ApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            application.JobStat = JobApplicationStat.Rejected;

            db.SaveChanges();

            try
            {
                _services.RejectJobApplication(
                    application.Email,
                    application.FirstName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email failed: {ex.Message}");
            }

            return RedirectToAction("JobApplications");
        }




        //====Create Assessment======
        // Display Create Assessment page
        public IActionResult CreateAssessment()
        {
            var model = new CreateAssessmentViewModel();

            model.JobPosts = db.JobPosts
                .Select(j => new SelectListItem
                {
                    Value = j.PostId.ToString(),
                    Text = j.JobTitle
                })
                .ToList();

            return View("CreateAssessment", model);
        }

        [HttpPost]
        public IActionResult CreateAssessment(CreateAssessmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.JobPosts = db.JobPosts
                    .Select(j => new SelectListItem
                    {
                        Value = j.PostId.ToString(),
                        Text = j.JobTitle
                    })
                    .ToList();

                return View(model);
            }

            // Check if this job already has an assessment
            var assessment = db.Assessments
                .FirstOrDefault(a => a.PostId == model.PostId);

            // If not, create one
            if (assessment == null)
            {
                assessment = new Assessment
                {
                    PostId = model.PostId,
                    JobTitle = model.JobTitle,
                    PassMark = model.PassMark,
                    TimeLimit = model.TimeLimit,
                    IsPublished = false
                };

                db.Assessments.Add(assessment);
                db.SaveChanges();
            }

            // Go to Add Question page
            return RedirectToAction("AddQuestion", new
            {
                id = assessment.AssessmentId
            });
        }




        public IActionResult AddQuestion(int id)
        {
            var assessment = db.Assessments
                .Include(a => a.QuizQuestions)
                .FirstOrDefault(a => a.AssessmentId == id);

            if (assessment == null)
            {
                return NotFound();
            }

            var model = new AddQuestionViewModel
            {
                AssessmentId = assessment.AssessmentId,
                AssessmentTitle = assessment.JobTitle,
                ExistingQuestions = assessment.QuizQuestions!.ToList()
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult AddQuestion(AddQuestionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var question = new QuizQuestion
            {
                AssessmentId = model.AssessmentId,
                QuestionText = model.QuestionText,
                OptionA = model.OptionA,
                OptionB = model.OptionB,
                OptionC = model.OptionC,
                OptionD = model.OptionD,
                CorrectAnswer = model.CorrectAnswer
            };

            db.QuizQuestions.Add(question);
            db.SaveChanges();

            return RedirectToAction("AddQuestion",
                new { id = model.AssessmentId });
        }

        // Edit Question - save changes
        [HttpPost]
        public IActionResult EditQuestion(int id, AddQuestionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var question = db.QuizQuestions
                .FirstOrDefault(q => q.QuestionId == id);

            if (question == null)
            {
                return NotFound();
            }

            question.QuestionText = model.QuestionText;
            question.OptionA = model.OptionA;
            question.OptionB = model.OptionB;
            question.OptionC = model.OptionC;
            question.OptionD = model.OptionD;
            question.CorrectAnswer = model.CorrectAnswer;

            db.SaveChanges();

            return RedirectToAction("AddQuestion", new
            {
                id = question.AssessmentId
            });
        }


        // Delete Question
        public IActionResult DeleteQuestion(int id)
        {
            var question = db.QuizQuestions
                .FirstOrDefault(q => q.QuestionId == id);

            if (question == null)
            {
                return NotFound();
            }

            int assessmentId = question.AssessmentId;

            db.QuizQuestions.Remove(question);
            db.SaveChanges();

            return RedirectToAction("AddQuestion", new
            {
                id = assessmentId
            });
        }




        public IActionResult PublishAssessment(int id)
        {
            var assessment = db.Assessments
                .Include(a => a.QuizQuestions)
                .FirstOrDefault(a => a.AssessmentId == id);

            if (assessment == null)
            {
                return NotFound();
            }

            if (!assessment.QuizQuestions.Any())
            {
                TempData["Message"] = "Please add at least one question before publishing.";

                return RedirectToAction("AddQuestion", new { id });
            }

            assessment.IsPublished = true;

            db.SaveChanges();

            TempData["Message"] = "Quiz published successfully.";

            return RedirectToAction("JobApplications");
        }

        public IActionResult UnpublishAssessment(int id)
        {
            var assessment = db.Assessments
                .FirstOrDefault(a => a.AssessmentId == id);

            if (assessment == null)
            {
                return NotFound();
            }

            assessment.IsPublished = false;

            db.SaveChanges();

            TempData["Message"] = "Assessment unpublished successfully.";

            return RedirectToAction("ManageAssessment", new { id });
        }






        //==Edit Assessment==

        public IActionResult ManageAssessments()
        {
            var assessments = db.Assessments
                .Include(a => a.JobPosts)
                .Include(a => a.QuizQuestions)
                .Select(a => new AssessmentListViewModel
                {
                    AssessmentId = a.AssessmentId,
                    AssessmentTitle = a.JobTitle,
                    JobTitle = a.JobPosts!.JobTitle,
                    QuestionCount = a.QuizQuestions.Count,
                    PassMark = a.PassMark,
                    TimeLimit = a.TimeLimit,
                    IsPublished = a.IsPublished
                })
                .ToList();

            return View(assessments);
        }
        public IActionResult ManageAssessment(int id)
        {
            var assessment = db.Assessments
                .Include(a => a.JobPosts)
                .Include(a => a.QuizQuestions)
                .FirstOrDefault(a => a.AssessmentId == id);

            if (assessment == null)
            {
                return NotFound();
            }

            var model = new AssessmentManagementViewModel
            {
                Assessment = assessment,
                QuestionCount = assessment.QuizQuestions.Count
            };

            return View(model);
        }




        // Edit Assessment - display page
        public IActionResult EditAssessment(int id)
        {
            var assessment = db.Assessments
                .FirstOrDefault(a => a.AssessmentId == id);

            if (assessment == null)
            {
                return NotFound();
            }

            // Published assessments cannot be edited
            if (assessment.IsPublished)
            {
                TempData["Message"] =
                    "Published assessments cannot be edited. Unpublish the assessment first.";

                return RedirectToAction("ManageAssessment", new { id });
            }

            var model = new CreateAssessmentViewModel
            {
                PostId = assessment.PostId,
                JobTitle = assessment.JobTitle,
                PassMark = assessment.PassMark,
                TimeLimit = assessment.TimeLimit
            };

            return View(model);
        }

        // Edit Assessment - save changes
        [HttpPost]
        public IActionResult EditAssessment(int id, CreateAssessmentViewModel model)
        {
            var assessment = db.Assessments
                .FirstOrDefault(a => a.AssessmentId == id);

            if (assessment == null)
            {
                return NotFound();
            }

            // Published assessments cannot be edited
            if (assessment.IsPublished)
            {
                TempData["Message"] =
                    "Published assessments cannot be edited. Unpublish the assessment first.";

                return RedirectToAction("ManageAssessment", new { id });
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            assessment.JobTitle = model.JobTitle;
            assessment.PassMark = model.PassMark;
            assessment.TimeLimit = model.TimeLimit;

            db.SaveChanges();

            TempData["Message"] = "Assessment updated successfully.";

            return RedirectToAction("ManageAssessment", new { id });
        }






    }

}