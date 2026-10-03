using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
//using SkyTech.Models.SkytechWorkers.SkyTech.Models.SkytechWorkers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models.SkytechWorkers
{
    /// <summary>
    /// A single automated fraud signal raised during assessment. Advisory
    /// only — never blocks disbursement on its own, the shop owner reads
    /// it and decides (or dismisses it with a reason, kept for audit).
    /// </summary>
    public class FraudFlag
    {
        [Key]
        public int FraudFlagId { get; set; }

        [ForeignKey(nameof(LoanAssessment))]
        public int LoanAssessmentId { get; set; }
        [ValidateNever]
        public LoanAssessment LoanAssessment { get; set; } = null!;

        public FraudFlagType Type { get; set; }
        public FraudFlagSeverity Severity { get; set; }

        [Required]
        [StringLength(500)]
        public string Details { get; set; } = string.Empty;

        /// <summary>Set for DuplicateCollateral/ConcurrentActiveLoans — the other application involved.</summary>
        public int? RelatedLoanApplicationId { get; set; }

        public bool Dismissed { get; set; }

        [StringLength(500)]
        public string? DismissReason { get; set; }
    }
}