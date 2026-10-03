using SkyTech.Models.SkytechWorkers;

namespace SkyTech.Services
{
    public class CollateralImageMatch
    {
        public int MatchedLoanApplicationId { get; set; }
        public string MatchedDescription { get; set; } = string.Empty;
        public DateTime MatchedUploadedOn { get; set; }
        public LoanApplicationStatus MatchedApplicationStatus { get; set; }
    }

    public class CollateralImageCheckResult
    {
        public int CollateralItemId { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<CollateralImageMatch> Matches { get; set; } = new();
        public bool IsDuplicate => Matches.Count > 0;
    }

    /// <summary>
    /// Runs the automated fraud checks for a loan application at assessment
    /// time. All checks run against data already in this database — no
    /// external price/fraud API, since none has been decided on yet.
    /// </summary>
    public interface IFraudDetectionService
    {
        Task<List<FraudFlag>> DetectAsync(
            LoanApplication application,
            int loanAssessmentId,
            CancellationToken cancellationToken = default);

        /// <summary>Re-checks every collateral photo on this application against
        /// every other application's collateral photos, live (not persisted),
        /// for display on the Fraud Flags page.</summary>
        Task<List<CollateralImageCheckResult>> CheckImageDuplicatesAsync(
            LoanApplication application,
            CancellationToken cancellationToken = default);
    }
}





//using SkyTech.Models.SkytechWorkers;
//using SkyTech.Models;

//namespace SkyTech.Services
//{
//    /// <summary>
//    /// Runs the automated fraud checks for a loan application at assessment
//    /// time. All checks run against data already in this database — no
//    /// external price/fraud API, since none has been decided on yet.
//    /// </summary>
//    public interface IFraudDetectionService
//    {
//        Task<List<FraudFlag>> DetectAsync(
//            LoanApplication application,
//            int loanAssessmentId,
//            CancellationToken cancellationToken = default);
//    }

//    namespace SkyTech.Services
//    {
//        public class CollateralImageMatch
//        {
//            public int MatchedLoanApplicationId { get; set; }
//            public string MatchedDescription { get; set; } = string.Empty;
//            public DateTime MatchedUploadedOn { get; set; }
//            public SkyTech.Models.SkytechWorkers.LoanApplicationStatus MatchedApplicationStatus { get; set; }
//        }

//        public class CollateralImageCheckResult
//        {
//            public int CollateralItemId { get; set; }
//            public string ImagePath { get; set; } = string.Empty;
//            public string Description { get; set; } = string.Empty;
//            public List<CollateralImageMatch> Matches { get; set; } = new();
//            public bool IsDuplicate => Matches.Count > 0;
//        }

//        public interface IFraudDetectionService
//        {
//            Task<List<FraudFlag>> DetectAsync(
//                SkyTech.Models.SkytechWorkers.LoanApplication application,
//                int loanAssessmentId,
//                CancellationToken cancellationToken = default);

//            /// <summary>Re-checks every collateral photo on this application against
//            /// every other application's collateral photos, live (not persisted),
//            /// for display on the Fraud Flags page.</summary>
//            Task<List<CollateralImageCheckResult>> CheckImageDuplicatesAsync(
//                SkyTech.Models.SkytechWorkers.LoanApplication application,
//                CancellationToken cancellationToken = default);
//        }
//    }
//}