using SkyTech.Models;
using SkyTech.Models.Interviews;
using SkyTech.Models.JobPostServices;

namespace SkyTech.ViewModels
{
    public class SendContractViewModel
    {
        public int ApplicationId { get; set; }

        public int PostId { get; set; }

        public int ContractId { get; set; }

        public string? CandidateName { get; set; }

        public string? Email { get; set; }

        public string? JobTitle { get; set; }

        public string? JobDescription { get; set; }

        public string? ContractTerms { get; set; }

        public SelectEmploymentType EmploymentType { get; set; }

        public string CompanySignature { get; set; }

        public ContractStatus Status { get; set; }
    }
}