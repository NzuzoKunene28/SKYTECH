using Microsoft.EntityFrameworkCore;
using SkyTech.JobPostServices;
using System;
using System.Threading.Tasks;
using SkyTech.Models;

namespace SkyTech.Models.Interviews
{
    public class RecruitmentService
    {
        private readonly ApplicationDbContext db;
        private readonly JitsiService _jitsiService;
       
        public RecruitmentService(ApplicationDbContext context, JitsiService jitsiService)
        {
            db = context;
            _jitsiService = jitsiService;
            
        }

        //public async Task<bool> ProcessTestResult(int testId, DateTime scheduledDate)
        //{
        //   // int? currentId = HttpContext.Session.GetInt32("UserId");

        //    var test = await db.TestResults.FindAsync(testId);
        //    if (test == null || test.Passed == false) return false;

        //    var cus = db.ApplicationForms.FirstOrDefault(a => a.CustomerId == test.CustomerId);

        //    var interview = new Interview
        //    {
        //        CustomerId = test.CustomerId,
        //        TestId = test.TestId,
        //        Status = InterviewStatus.Scheduled,
        //        ScheduledDate = scheduledDate,
        //        JitsiMeetingLink = "",
        //        EvaluationNotes = "",
        //        ReviewedByHR = ""
        //    };

        //    db.Interviews.Add(interview);
        //    await db.SaveChangesAsync();

        //    interview.JitsiMeetingLink = _jitsiService.GenerateMeetingLink(interview.InterviewId, interview.CustomerId);
        //    await db.SaveChangesAsync();

        //    return true;
        //}
        public async Task<bool> ProcessTestResult(int resultId, DateTime scheduledDate)
        {
            var testResult = await db.TestResults
                .Include(tr => tr.ApplicationForms)
                .FirstOrDefaultAsync(tr => tr.ResultId == resultId);

            if (testResult == null)
                return false;

            // Applicant must have passed the assessment
            if (!testResult.Passed)
                return false;

            var application = testResult.ApplicationForms;

            if (application == null)
                return false;

            // Make sure an interview hasn't already been created
            var existingInterview = await db.Interviews
                .FirstOrDefaultAsync(i => i.ResultId == testResult.ResultId);

            if (existingInterview != null)
                return false;

            var interview = new Interview
            {
                CustomerId = application.CustomerId,
                ResultId = testResult.ResultId,

                Status = InterviewStatus.Scheduled,
                ScheduledDate = scheduledDate,

                JitsiMeetingLink = "",
                EvaluationNotes = "",
                ReviewedByHR = ""
            };

            db.Interviews.Add(interview);
            await db.SaveChangesAsync();

            interview.JitsiMeetingLink =
                _jitsiService.GenerateMeetingLink(
                    interview.InterviewId,
                    interview.CustomerId);

            await db.SaveChangesAsync();

            return true;
        }
        public async Task EvaluateInterview(int interviewId, int score, string notes, bool shortlist)
        {
            var interview = await db.Interviews.FindAsync(interviewId);
            if (interview == null) return;

            interview.EvaluationScore = score;
            interview.EvaluationNotes = notes;
            interview.EvaluatedOn = DateTime.Now;
            interview.Status = shortlist ? InterviewStatus.Pre_Approved : InterviewStatus.Unsuccessful;

            await db.SaveChangesAsync();
        }

        //public async Task<Contract> HireCandidate(int interviewId, string jobTitle, decimal salary, DateTime startDate)
        //{
        //    var interview = await db.Interviews
        //        .Include(i => i.Customer)
        //        .FirstOrDefaultAsync(i => i.InterviewId == interviewId);

        //    if (interview == null) return null;

        //    interview.Status = InterviewStatus.Employed;

        //    var contract = new Contract
        //    {
        //        CustomerId = interview.CustomerId,
        //        InterviewId = interview.InterviewId,
        //        JobTitle = jobTitle,
        //        Salary = salary,
        //        StartDate = startDate,
        //        Status = ContractStatus.Generated,
        //        ContractFilePath = ""
        //    };
        //    db.Contracts.Add(contract);
        //    await db.SaveChangesAsync();

        //    contract.Customer = interview.Customer;
        //    contract.ContractFilePath = _pdfService.GenerateContractPdf(contract);
        //    await db.SaveChangesAsync();

        //    return contract;
        //}
        //public async Task<bool> SignContract(int contractId)
        //{
        //    var contract = await db.Contracts
        //        .Include(c => c.Customer)
        //        .FirstOrDefaultAsync(c => c.ContractId == contractId);

        //    if (contract == null || contract.Status != ContractStatus.Generated)
        //        return false;

        //    contract.Status = ContractStatus.Signed;
        //    contract.SignedOn = DateTime.Now;

        //    contract.Customer.UserType = "Employee";

        //    await db.SaveChangesAsync();
        //    return true;
        //}

        //public async Task<bool> CloseOffer(int interviewId)
        //{
        //    var contract = await db.Contracts
        //        .FirstOrDefaultAsync(c => c.InterviewId == interviewId);

        //    if (contract == null) return false;

        //    contract.Status = ContractStatus.Closed;
        //    await db.SaveChangesAsync();
        //    return true;
        //}
    }
}