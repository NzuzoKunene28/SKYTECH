using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.Interviews;
using System.Threading.Tasks;
using SkyTech.Models.JobPostServices;
using SkyTech.ViewModels;

namespace SkyTech.Controllers
{
    [AuthorizeUser]
    public class RecruitmentController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly RecruitmentService _recruitmentService;
        private readonly EmailServices _services;


        public RecruitmentController(ApplicationDbContext context, RecruitmentService recruitmentService, EmailServices services)
        {
            db = context;
            _recruitmentService = recruitmentService;
            _services = services;
        }

        // Shows all customers who passed a test but have no interview yet
        public async Task<IActionResult> PendingInterviews()
        {
            var pending = await db.ApplicationForms
                .Include(t=> t.TestResults)
                .Where(t => t.JobStat == JobApplicationStat.Passed_Test &&
                            !db.Interviews.Any(i => i.ResultId == t.TestResults.ResultId))
                .Select(a => new ScheduleMeetingViewModel
                {
                    ResultId = a.TestResults.ResultId,
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    Score = a.TestResults.Score,
                    DateWritten = a.TestResults.DateWritten
                })
        .ToListAsync();

            return View(pending);
        }

        [HttpPost]
        public async Task<IActionResult> ScheduleFromTest(int ResultId, DateTime scheduledDate)
        {
            // Find the test result first, since we need it either way
            var testResult = await db.TestResults
                .FirstOrDefaultAsync(r => r.ResultId == ResultId);

            if (testResult == null)
            {
                return NotFound();
            }

            var application = await db.ApplicationForms
                .FirstOrDefaultAsync(a => a.ApplicationId == testResult.ApplicationId);

            if (application == null)
            {
                return NotFound();
            }

            var success = await _recruitmentService.ProcessTestResult(ResultId, scheduledDate);

            if (!success)
            {
                // Candidate failed the test — notify them and update status
                application.JobStat = JobApplicationStat.Failed_Test;

                try
                {
                    _services.JobTestFailed(application.Email, application.FirstName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Email failed: {ex.Message}");
                }

                await db.SaveChangesAsync();

                TempData["Error"] = "This candidate did not pass the test.";
                return RedirectToAction(nameof(PendingInterviews));
            }

            // Candidate passed and interview was successfully scheduled
            application.JobStat = JobApplicationStat.Shortlisted;

            try
            {
                _services.JobInterviewScheduled(
                    application.Email,
                    application.FirstName,
                    scheduledDate);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email failed: {ex.Message}");
            }

            await db.SaveChangesAsync();

            return RedirectToAction(nameof(ScheduledInterviews));
        }
        public async Task<IActionResult> ScheduledInterviews()
        {
            var interviews = await db.Interviews
                .Include(i => i.Customer)
                .Where(i => i.Status == InterviewStatus.Scheduled)
                .ToListAsync();

            return View(interviews);
        }

        [HttpGet]
        public async Task<IActionResult> Evaluate(int id)
        {
            var interview = await db.Interviews
                .Include(i => i.Customer)
                .FirstOrDefaultAsync(i => i.InterviewId == id);

            if (interview == null) return NotFound();
            return View(interview);
        }

        [HttpPost]
        public async Task<IActionResult> Evaluate(int interviewId, int score, string notes, bool shortlist)
        {
            await _recruitmentService.EvaluateInterview(interviewId, score, notes, shortlist);
            return RedirectToAction(nameof(ScheduledInterviews));
        }

        public async Task<IActionResult> HRReview()
        {
            var shortlisted = await db.Interviews
                .Include(i => i.Customer)
                .Where(i => i.Status == InterviewStatus.Pre_Approved)
                .ToListAsync();

            return View(shortlisted);
        }

        //[HttpPost]
        //public async Task<IActionResult> Hire(int interviewId, string jobTitle, decimal salary, DateTime startDate)
        //{
        //    await _recruitmentService.HireCandidate(interviewId, jobTitle, salary, startDate);
        //    return RedirectToAction("");
        //}

        [HttpPost]
        public async Task<IActionResult> RejectAtHR(int interviewId)
        {
            var interview = await db.Interviews.FindAsync(interviewId);
            if (interview != null)
            {
                interview.Status = InterviewStatus.Unsuccessful;
                await db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(HRReview));
        }

        // to join the meeting
        public async Task<IActionResult> JoinMeeting(int id)
        {
            var interview = await db.Interviews
                .Include(i => i.Customer)
                .FirstOrDefaultAsync(i => i.InterviewId == id);

            if (interview == null) return NotFound();
            return View(interview);
        }


        public async Task<IActionResult> ShortlistedCandidates()
        {
            var applicants = await
                (from application in db.ApplicationForms
                 join interview in db.Interviews
                     on application.CustomerId equals interview.CustomerId
                 where interview.Status == InterviewStatus.Pre_Approved
                 select new ApplicantDashboardViewModel
                 {
                     ApplicationId = application.ApplicationId,
                     FirstName = application.FirstName + " " + application.LastName,
                     JobTitle = application.JobTitle,
                     Email = application.Email,
                     JobStat = application.JobStat,
                     InterviewStat = interview.Status,
                     EvaluationNotes = interview.EvaluationNotes,
                     EvaluationScore = interview.EvaluationScore,
                     EvaluatedOn = interview.EvaluatedOn,
                     CanWriteTest = !application.TestWritten,
                     // for contract
                     ContractId = db.Contracts
                   .Where(c => c.ApplicationId == application.ApplicationId)
                   .Select(c => (int?)c.ContractId)
                   .FirstOrDefault(),

                     ContractStatus = db.Contracts
                       .Where(c => c.ApplicationId == application.ApplicationId)
                       .Select(c => (ContractStatus?)c.ContractStat)
                       .FirstOrDefault()
                 })
                .ToListAsync();

            return View(applicants);
        }

        //when the HR presses send contract button this action happens
        [HttpGet]
        public async Task<IActionResult> SendContract(int id, Interview inModel)
        {
            var application = await db.ApplicationForms
                .Include(a => a.JobPosts)
                .FirstOrDefaultAsync(a => a.ApplicationId == id);

            if (application == null)
                return NotFound();

            // Prevent creating duplicate contracts
            var existingContract = await db.Contracts
                .FirstOrDefaultAsync(c => c.ApplicationId == id);

            if (existingContract != null)
            {
                return RedirectToAction(nameof(CompanySign),
                    new { id = existingContract.ContractId });
            }

            var model = new SendContractViewModel
            {
                ApplicationId = application.ApplicationId,
                PostId = application.PostId,

                CandidateName = application.FirstName + " " + application.LastName,
                Email = application.Email,

                JobTitle = application.JobPosts.JobTitle,
                JobDescription = application.JobPosts.Responsibilities, //like the job description

                EmploymentType = application.JobPosts.EmploymentType,

                ContractTerms = application.JobPosts.ContractTerms
            };

            // =====================================================
            // SEND EMAIL BASED ON APPLICATION STATUS
            // =====================================================

            try
            {
                switch (inModel.Status)
                {
                    case InterviewStatus.Pre_Approved:

                        _services.SendContract(
                            application.Email,
                            application.FirstName);

                        break;

                }
            }
            catch (Exception ex)
            {
                // Don't delete/reject the application just because the email failed.
                Console.WriteLine($"Email failed: {ex.Message}");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendContract(SendContractViewModel model)
        {
            //if (!ModelState.IsValid)
            //    return View(model);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                return Content(string.Join("\n", errors));
            }

            Contract contract = new Contract
            {
                ApplicationId = model.ApplicationId,

                PostId = model.PostId,

                CompanySignature = model.CompanySignature,

                CompanySignedOn = DateTime.Now,

                DateSent = DateTime.Now,

                ContractStat = ContractStatus.Sent
            };

            db.Contracts.Add(contract);

            await db.SaveChangesAsync();

            return RedirectToAction(nameof(ShortlistedCandidates));
        }

        //this is the vioew where the HR will see where the candidate signed
        public async Task<IActionResult> CompanySign(int id)
        {
            var contract = await db.Contracts
                .Include(c => c.ApplicationForm)
                .Include(c => c.JobPost)
                .FirstOrDefaultAsync(c => c.ContractId == id);

            if (contract == null)
                return NotFound();

            var model = new SendContractViewModel
            {
                ContractId = contract.ContractId,

                ApplicationId = contract.ApplicationId,

                PostId = contract.PostId,

                CandidateName =
                    contract.ApplicationForm.FirstName + " " +
                    contract.ApplicationForm.LastName,

                Email = contract.ApplicationForm.Email,

                JobTitle = contract.JobPost.JobTitle,

                JobDescription = contract.JobPost.Responsibilities,

                EmploymentType = contract.JobPost.EmploymentType,

                ContractTerms = contract.JobPost.ContractTerms,

                CompanySignature = contract.CompanySignature,

                Status = contract.ContractStat
            };

            return View(model);
        }

    }
}