using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.SkytechWorkers;
using SkyTech.Services;
//using SkyTech.Models.SkytechWorkers.SkyTech.Models.SkytechWorkers;

namespace SkyTech.Services
{
    /// <summary>
    /// Runs the automated fraud checks for a loan application at assessment
    /// time. All checks run against data already in this database — no
    /// external price/fraud API, since none has been decided on yet.
    /// </summary>
    public class FraudDetectionService : IFraudDetectionService
    {
        private readonly ApplicationDbContext _db;

        /// <summary>ASSUMPTION: Hamming-distance threshold below which two perceptual hashes are considered "the same image". 64-bit aHash, so 10 is a common starting point (~15% bit difference).</summary>
        private const int DuplicateHashThreshold = 10;

        /// <summary>ASSUMPTION: a declared value more than this multiple away from the category average is flagged.</summary>
        private const decimal ValueMismatchMultiplier = 2.5m;

        private static readonly LoanApplicationStatus[] ActiveStatuses =
        {
            LoanApplicationStatus.Submitted,
            LoanApplicationStatus.UnderStaffReview,
            LoanApplicationStatus.Approved,
            LoanApplicationStatus.Disbursed
        };

        public FraudDetectionService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<FraudFlag>> DetectAsync(
            LoanApplication application,
            int loanAssessmentId,
            CancellationToken cancellationToken = default)
        {
            var flags = new List<FraudFlag>();

            flags.AddRange(await DetectDuplicateCollateralAsync(application, loanAssessmentId, cancellationToken));
            flags.AddRange(await DetectValueMismatchAsync(application, loanAssessmentId, cancellationToken));
            flags.AddRange(await DetectConcurrentActiveLoansAsync(application, loanAssessmentId, cancellationToken));

            foreach (var flag in flags)
            {
                flag.LoanAssessmentId = loanAssessmentId;
            }

            return flags;
        }



        private async Task<List<FraudFlag>> DetectDuplicateCollateralAsync(
        LoanApplication application, int loanAssessmentId, CancellationToken ct)
        {
            var flags = new List<FraudFlag>();

            var thisAppItemHashes = application.CollateralItems
                .Where(i => !string.IsNullOrEmpty(i.ImagePerceptualHash))
                .Select(i => i.ImagePerceptualHash!)
                .ToList();

            if (thisAppItemHashes.Count == 0)
                return flags;

            // NOTE: intentionally checks against collateral images from EVERY
            // application regardless of status (Submitted, UnderStaffReview,
            // Approved, Rejected, Disbursed) — a rejected application's photo can
            // still be reused fraudulently elsewhere, so it must stay in scope.
            var otherItems = await _db.CollateralItems
                .Include(i => i.LoanApplication)
                .Where(i => i.LoanApplicationId != application.LoanApplicationId
                    && i.ImagePerceptualHash != null)
                .ToListAsync(ct);

            foreach (var hash in thisAppItemHashes)
            {
                foreach (var other in otherItems)
                {
                    if (HammingDistance(hash, other.ImagePerceptualHash!) <= DuplicateHashThreshold)
                    {
                        flags.Add(new FraudFlag
                        {
                            Type = FraudFlagType.DuplicateCollateral,
                            Severity = FraudFlagSeverity.Critical,
                            Details = $"Collateral image closely matches an item on loan application " +
                                      $"#{other.LoanApplicationId} ({other.Description}), uploaded on " +
                                      $"{other.UploadedOn:dd MMM yyyy HH:mm} — that application's status is " +
                                      $"{other.LoanApplication.Status}.",
                            RelatedLoanApplicationId = other.LoanApplicationId
                        });
                    }
                }
            }

            return flags;
        }

        private async Task<List<FraudFlag>> DetectValueMismatchAsync(
            LoanApplication application, int loanAssessmentId, CancellationToken ct)
        {
            var flags = new List<FraudFlag>();

            foreach (var item in application.CollateralItems)
            {
                if (string.IsNullOrEmpty(item.AiMatchedCategory))
                    continue; // nothing to compare against

                var categoryAverage = await _db.CollateralItems
                    .Where(i => i.AiMatchedCategory == item.AiMatchedCategory
                        && i.LoanApplicationId != application.LoanApplicationId)
                    .Select(i => (decimal?)i.DeclaredValue)
                    .AverageAsync(ct);

                if (categoryAverage is null or 0)
                    continue; // no history for this category yet

                if (item.DeclaredValue > categoryAverage.Value * ValueMismatchMultiplier)
                {
                    flags.Add(new FraudFlag
                    {
                        Type = FraudFlagType.ValueMismatch,
                        Severity = FraudFlagSeverity.Warning,
                        Details = $"'{item.Description}' declared at {item.DeclaredValue:C}, well above the {item.AiMatchedCategory} category average of {categoryAverage:C}."
                    });
                }
            }

            return flags;
        }

        private async Task<List<FraudFlag>> DetectConcurrentActiveLoansAsync(
            LoanApplication application, int loanAssessmentId, CancellationToken ct)
        {
            var flags = new List<FraudFlag>();

            var otherActiveLoans = await _db.LoanApplications
                .Where(a => a.NationalId == application.NationalId
                    && a.LoanApplicationId != application.LoanApplicationId
                    && ActiveStatuses.Contains(a.Status))
                .ToListAsync(ct);

            foreach (var other in otherActiveLoans)
            {
                flags.Add(new FraudFlag
                {
                    Type = FraudFlagType.ConcurrentActiveLoans,
                    Severity = FraudFlagSeverity.Warning,
                    Details = $"This applicant has another active loan application (#{other.LoanApplicationId}, {other.Status}, {other.RequestedAmount:C}).",
                    RelatedLoanApplicationId = other.LoanApplicationId
                });
            }

            return flags;
        }

        /// <summary>Bit-distance between two equal-length hex-encoded perceptual hashes.</summary>
        private static int HammingDistance(string hashA, string hashB)
        {
            if (hashA.Length != hashB.Length) return int.MaxValue;

            var a = Convert.ToUInt64(hashA, 16);
            var b = Convert.ToUInt64(hashB, 16);
            var xor = a ^ b;

            return System.Numerics.BitOperations.PopCount(xor);
        }

        public async Task<List<CollateralImageCheckResult>> CheckImageDuplicatesAsync(
          LoanApplication application, CancellationToken ct = default)
        {
            var results = new List<CollateralImageCheckResult>();

            // Pull every other application's hashed collateral once, up front — cheaper
            // than re-querying per item.
            var otherItems = await _db.CollateralItems
                .Include(i => i.LoanApplication)
                .Where(i => i.LoanApplicationId != application.LoanApplicationId
                    && i.ImagePerceptualHash != null)
                .ToListAsync(ct);

            foreach (var item in application.CollateralItems)
            {
                var result = new CollateralImageCheckResult
                {
                    CollateralItemId = item.CollateralItemId,
                    ImagePath = item.ImagePath,
                    Description = item.Description
                };

                if (!string.IsNullOrEmpty(item.ImagePerceptualHash))
                {
                    foreach (var other in otherItems)
                    {
                        if (HammingDistance(item.ImagePerceptualHash, other.ImagePerceptualHash!) <= DuplicateHashThreshold)
                        {
                            result.Matches.Add(new CollateralImageMatch
                            {
                                MatchedLoanApplicationId = other.LoanApplicationId,
                                MatchedDescription = other.Description,
                                MatchedUploadedOn = other.UploadedOn,
                                MatchedApplicationStatus = other.LoanApplication.Status
                            });
                        }
                    }
                }

                results.Add(result);
            }

            return results;
        }


    }
}