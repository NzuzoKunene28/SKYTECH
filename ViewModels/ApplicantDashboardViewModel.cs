using SkyTech.Models.JobPostServices;
using SkyTech.Models;
using SkyTech.Models.Interviews;

namespace SkyTech.ViewModels
{
    public class ApplicantDashboardViewModel
    {
        public int ApplicationId { get; set; }
        public string FirstName { get; set; } = string.Empty;

        public string JobTitle { get; set; } = string.Empty;

        public string Email { get; set;  }

        public InterviewStatus? InterviewStat { get; set; }

        public string EvaluationNotes { get; set; }

        public int? EvaluationScore { get; set; }
        public DateTime? EvaluatedOn { get; set; }

        public int Score { get; set; }
        public JobApplicationStat JobStat { get; set; } 
        public bool CanWriteTest { get; set; }

        // New properties
        public int? ContractId { get; set; }

        public ContractStatus? ContractStatus { get; set; }
    }
}
