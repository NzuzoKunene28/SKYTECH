using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
//using SkyTech.Models.SkytechWorkers.SkyTech.Models.SkytechWorkers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models.SkytechWorkers
{
    /// <summary>
    /// A shop owner's assessment pass over a submitted LoanApplication
    /// (UC-04). Created when a shop owner opens an application from the
    /// queue; holds the re-checked credit score, fraud flags, and the
    /// per-item physical collateral verifications gathered during the
    /// session, plus the final decision.
    /// </summary>
    public class LoanAssessment
    {
        [Key]
        public int LoanAssessmentId { get; set; }

        [ForeignKey(nameof(LoanApplication))]
        public int LoanApplicationId { get; set; }
        [ValidateNever]
        public LoanApplication LoanApplication { get; set; } = null!;

        [ForeignKey(nameof(ShopOwner))]
        public int OwnerId { get; set; }
        [ValidateNever]
        public ShopOwner ShopOwner { get; set; } = null!;

        // ── Credit re-check (then vs. now) ──────────────────────────
        public int CreditScoreAtAssessment { get; set; }

        [NotMapped]
        public int CreditScoreDelta =>
            CreditScoreAtAssessment - LoanApplication.CreditScoreAtApplication;

        public ClearanceStatus ClearanceStatusAtAssessment { get; set; }

        /// <summary>
        /// ASSUMPTION: re-uses the same pass/fail rule as the customer-side
        /// eligibility gate (RunEligibilityCheckAsync). If the qualifying
        /// threshold ever diverges between application-time and
        /// assessment-time, extract that rule into a shared method rather
        /// than duplicating the threshold constant here.
        /// </summary>
        public bool StillMeetsCreditThreshold { get; set; }

        // ── Outcome ──────────────────────────────────────────────────
        public LoanApplicationStatus Decision { get; set; } = LoanApplicationStatus.UnderStaffReview;

        [StringLength(1000)]
        public string? DecisionNotes { get; set; }

        public DateTime StartedOn { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedOn { get; set; }

        public ICollection<CollateralVerification> CollateralVerifications { get; set; } = new List<CollateralVerification>();
        public ICollection<FraudFlag> FraudFlags { get; set; } = new List<FraudFlag>();

        [NotMapped]
        public bool AnyUnresolvedCriticalFlags =>
            FraudFlags.Any(f => f.Severity == FraudFlagSeverity.Critical && !f.Dismissed);

        [NotMapped]
        public bool AllCollateralVerified =>
            CollateralVerifications.All(v => v.Result != CollateralVerificationResult.Pending);
    }
}