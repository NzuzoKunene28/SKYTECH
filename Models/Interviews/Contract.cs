using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkyTech.Models.JobPostServices;

namespace SkyTech.Models.Interviews
{
    public enum ContractStatus
    {
        Draft,
        Sent,
        SignedByCandidate,
        SignedByCompany,
        Completed,
        Rejected
    }

    public class Contract
    {
        [Key]
        public int ContractId { get; set; }

        [ForeignKey(nameof(ApplicationForm))]
        public int ApplicationId { get; set; }

        public virtual ApplicationForm ApplicationForm { get; set; }

        [ForeignKey(nameof(JobPost))]
        public int PostId { get; set; }

        public virtual JobPost JobPost { get; set; }

        public DateTime DateSent { get; set; }

        public DateTime? CandidateSignedOn { get; set; }

        public DateTime? CompanySignedOn { get; set; }

        public string? CandidateSignature { get; set; }

        public string? CompanySignature { get; set; }

        public ContractStatus ContractStat { get; set; } = ContractStatus.Draft;
    }
}