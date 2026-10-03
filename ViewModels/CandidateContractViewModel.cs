using SkyTech.Models;
using SkyTech.Models.Interviews;
using SkyTech.Models.JobPostServices;

namespace SkyTech.ViewModels
{
    public class CandidateContractViewModel
    {
        public int ContractId { get; set; }
        public int ApplicationId { get; set; }
        public string Email { get; set; }

        public string CompanyName { get; set; }

        public string CandidateName { get; set; }

        public string JobTitle { get; set; }

        public string JobDescription { get; set; }

        public string ContractTerms { get; set; }

        public SelectEmploymentType EmploymentType { get; set; }

        public DateTime DateSent { get; set; }

        public string CandidateSignature { get; set; }

        public bool AgreeToTerms { get; set; }

        public ContractStatus ContractStat { get; set; }
    }
}