namespace SkyTech.Models.JobPostServices
{
    public class JobPostDraftRequest
    {
        public string JobTitle { get; set; }
        public string Department { get; set; }
        public string Location { get; set; }
        public string EmploymentType { get; set; }
        public double? SalaryMin { get; set; }
        public double? SalaryMax { get; set; }
    }

    public class JobPostDraftResult
    {
        public string Responsibilities { get; set; }
        public string RequiredCerts { get; set; }
        public string RequiredSkillsAndXP { get; set; }
    }

    public interface IAIJobPostDraftService
    {
        Task<JobPostDraftResult> GenerateDraftAsync(JobPostDraftRequest request);
        Task<JobPostDraftResult> GenerateDraftFromRoleAsync(JobRoleKnowledge role);
    }


}
