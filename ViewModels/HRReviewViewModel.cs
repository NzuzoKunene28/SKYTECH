using SkyTech.Models.JobPostServices;

using SkyTech.Models.Interviews;

namespace SkyTech.ViewModels
{
    public class HRReviewViewModel
    {
        public int ApplicationId { get; set; }
        public int? ContractId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string JobTitle { get; set; } = string.Empty;

        public int Score { get; set; }

        public bool Passed { get; set; }

      /*  public string RecruitmentStatus { get; set; } = string.Empty;*/

        public bool IsPublished { get; set; }

        public InterviewStatus? InterviewStat { get; set; }

        public JobApplicationStat JobStat { get; set; }
        public ContractStatus? ContractStatus { get; set; }
        public int MatchedSkillCount { get; set; }
    }
}
