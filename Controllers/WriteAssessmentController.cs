using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.Interviews;
using SkyTech.Models.JobPostServices;
using SkyTech.ViewModels;

namespace SkyTech.Controllers
{
    [AuthorizeUser]
    public class WriteAssessmentController : Controller
    {

        private readonly ApplicationDbContext db;
        private readonly EmailServices _services;


        public WriteAssessmentController(ApplicationDbContext db, EmailServices services)
        {
            _services = services;
            this.db = db;
        }


          public async Task<IActionResult> MyTests()
        {

            int? applicantId = HttpContext.Session.GetInt32("UserId");

            
            var application = db.ApplicationForms
                .Include(a => a.JobPosts)
                .Include(a => a.TestResults)
                .FirstOrDefault(a => a.CustomerId == applicantId.Value);

            if (application == null)
            {
                return RedirectToAction("Careers", "CustomerApplications");
            }

            // Check whether this job has a published assessment
            var assessment = db.Assessments
                .FirstOrDefault(a =>
                    a.PostId == application.PostId &&
                    a.IsPublished);


            // Find the interview belonging to this applicant's TestResult
            Interview? interview = null;
            if (application.TestResults != null)
            {
                interview = await db.Interviews
                    .FirstOrDefaultAsync(i =>
                        i.ResultId == application.TestResults.ResultId);
            }

            var model = new ApplicantDashboardViewModel
            {
                FirstName = application.FirstName,
                JobTitle = application.JobPosts!.JobTitle,
                JobStat = application.JobStat,

                Score = application.TestResults?.Score ?? 0,
                InterviewStat = interview?.Status,

                CanWriteTest =
                application.JobStat == JobApplicationStat.Shortlisted
                && !application.TestWritten
                && assessment != null
             };

            return View(model);
        }



        public IActionResult WriteTest()
        {
            int? applicantId = HttpContext.Session.GetInt32("UserId");

            //if (applicantId == null)
            //{
            //    return RedirectToAction("Login", "Account");
            //}

            // Find the applicant's application
            var application = db.ApplicationForms
                .Include(a => a.JobPosts)
                .FirstOrDefault(a => a.CustomerId == applicantId.Value);

            if (application == null)
            {
                return Content("You have not applied for any vacancy.");
            }

            if (application.JobStat != Models.JobPostServices.JobApplicationStat.Shortlisted)
            {
                return Content("You are not eligible to write this assessment.");
            }

            if (application.TestWritten)
            {
                return Content("You have already completed this assessment.");
            }

            // Find the published assessment for the job
            var assessment = db.Assessments
                .Include(a => a.QuizQuestions)
                .FirstOrDefault(a =>
                    a.PostId == application.PostId &&
                    a.IsPublished);

            if (assessment == null)
            {
                return Content("No assessment has been published for this vacancy yet.");
            }

            var model = new QuizViewModel();

            foreach (var question in assessment.QuizQuestions)
            {
                model.Questions.Add(new QuizQuestionViewModel
                {
                    QuestionId = question.QuestionId,
                    QuestionText = question.QuestionText,
                    OptionA = question.OptionA,
                    OptionB = question.OptionB,
                    OptionC = question.OptionC,
                    OptionD = question.OptionD
                });
            }
            ViewBag.TimeLimit = assessment.TimeLimit;
           
            return View(model);
        }



        [HttpPost]
        public IActionResult SubmitTest(QuizViewModel model)
        {
            int? applicantId = HttpContext.Session.GetInt32("UserId");

            //if (applicantId == null)
            //{
            //    return RedirectToAction("Login", "Account");
            //}

            var application = db.ApplicationForms
                .FirstOrDefault(a => a.CustomerId == applicantId.Value);

            if (application == null)
            {
                return Content("Application not found.");
            }

            int score = 0;

            foreach (var answer in model.Questions)
            {
                var question = db.QuizQuestions
                    .FirstOrDefault(q => q.QuestionId == answer.QuestionId);

                if (question != null &&
                    answer.SelectedAnswer == question.CorrectAnswer)
                {
                    score++;
                }
            }

            var assessment = db.Assessments
            .FirstOrDefault(a => a.PostId == application.PostId && a.IsPublished);

            double percentage = (double)score / model.Questions.Count * 100;

            bool passed = percentage >= assessment!.PassMark;


            application.TestWritten = true;
            application.TestScore = score;

            application.JobStat = passed
                ? JobApplicationStat.Passed_Test
                : JobApplicationStat.Failed_Test;

            var result = new Models.TestResult
            {
                ApplicationId = application.ApplicationId,
                Score = score,
                Passed = passed,
                DateWritten = DateTime.Now
            };


           

            db.TestResults.Add(result);



            db.SaveChanges();

            // =====================================================
            // SEND EMAIL BASED ON APPLICATION STATUS
            // =====================================================

            try
            {
                switch (application.JobStat)
                {
                    case JobApplicationStat.Passed_Test:

                        _services.JobApplicationPassedTest(
                            application.Email,
                            application.FirstName);

                        break;


                    case JobApplicationStat.Failed_Test:

                        _services.JobApplicationFailedTest(
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

            var resultModel = new TestResultViewModel
            {
                Score = score,
                TotalQuestions = model.Questions.Count,
                Passed = passed,
                Message = passed
                    ? "Congratulations! You passed the assessment. Please wait for an email regarding the next steps in your application"
                    : "Unfortunately, you did not achieve the required pass mark."
            };


            return View("Result", resultModel);
        }

      

    }
}
