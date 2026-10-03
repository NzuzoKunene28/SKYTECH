using SkyTech.Models.SkytechWorkers;
using SkyTech.Services;

namespace SkyTech.Models.ViewModels
{
    public class AssessmentViewModel
    {
        public int LoanAssessmentId { get; set; }
        public LoanApplication LoanApplication { get; set; } = null!;


        public int CreditScoreAtApplication { get; set; }
        public int CreditScoreAtAssessment { get; set; }
        public int CreditScoreDelta { get; set; }
        public bool StillMeetsCreditThreshold { get; set; }

        public ClearanceStatus ClearanceStatusAtAssessment { get; set; }

        public List<FraudFlag> FraudFlags { get; set; } = new();
    //    public List<CollateralVerification> CollateralVerifications { get; set; } = new();

        public bool AnyUnresolvedCriticalFlags { get; set; }
     //   public bool AllCollateralVerified { get; set; }

        //public CollateralVerification? VerificationFor(int collateralItemId) =>
        //    CollateralVerifications.FirstOrDefault(v => v.CollateralItemId == collateralItemId);

        public static AssessmentViewModel FromEntity(LoanAssessment a) => new()
        {
            LoanAssessmentId = a.LoanAssessmentId,
            LoanApplication = a.LoanApplication,
            CreditScoreAtApplication = a.LoanApplication.CreditScoreAtApplication,
            CreditScoreAtAssessment = a.CreditScoreAtAssessment,
            CreditScoreDelta = a.CreditScoreDelta,
            StillMeetsCreditThreshold = a.StillMeetsCreditThreshold,
            ClearanceStatusAtAssessment = a.ClearanceStatusAtAssessment,
            FraudFlags = a.FraudFlags.ToList(),
         //   CollateralVerifications = a.CollateralVerifications.ToList(),
            AnyUnresolvedCriticalFlags = a.AnyUnresolvedCriticalFlags,
           // AllCollateralVerified = a.AllCollateralVerified
        };

        public List<CollateralImageCheckResult> CollateralImageChecks { get; set; } = new();

    }
}