using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
//using SkyTech.Models.SkytechWorkers.SkyTech.Models.SkytechWorkers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models.SkytechWorkers
{
    /// <summary>
    /// Result of comparing a staff-captured photo of the physical item the
    /// customer brought in against the photo they originally submitted at
    /// application time. Distinguishes "different item" from "same item,
    /// condition has degraded" since those need different staff responses.
    /// </summary>
    public class CollateralVerification
    {
        [Key]
        public int CollateralVerificationId { get; set; }

        [ForeignKey(nameof(LoanAssessment))]
        public int LoanAssessmentId { get; set; }
        [ValidateNever]
        public LoanAssessment LoanAssessment { get; set; } = null!;

        [ForeignKey(nameof(CollateralItem))]
        public int CollateralItemId { get; set; }
        [ValidateNever]
        public CollateralItem CollateralItem { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string VerificationImagePath { get; set; } = string.Empty;

        public CollateralVerificationResult Result { get; set; } = CollateralVerificationResult.Pending;

        [Range(0, 100)]
        public int? MatchConfidencePercent { get; set; }

        public string? AiNotes { get; set; }

        /// <summary>Shop owner can override the AI verdict (e.g. bad lighting caused a false mismatch) — must justify why.</summary>
        public bool StaffOverride { get; set; }

        [StringLength(500)]
        public string? StaffOverrideReason { get; set; }
    }
}