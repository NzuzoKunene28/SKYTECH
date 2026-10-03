using SkyTech.Models.SkytechWorkers;
//using SkyTech.Models.SkytechWorkers.SkyTech.Models.SkytechWorkers;

namespace SkyTech.Services
{
    public class CollateralVerificationRequest
    {
        public byte[] OriginalImageBytes { get; set; } = Array.Empty<byte>();
        public string OriginalImageContentType { get; set; } = "image/jpeg";

        public byte[] NewImageBytes { get; set; } = Array.Empty<byte>();
        public string NewImageContentType { get; set; } = "image/jpeg";

        public string Description { get; set; } = string.Empty;
        public string? OriginalAiMatchedCategory { get; set; }
        public CollateralCondition DeclaredConditionAtApplication { get; set; }
    }

    public class CollateralVerificationAnalysisResult
    {
        public CollateralVerificationResult Result { get; set; }
        public int? Confidence { get; set; }
        public string Notes { get; set; } = string.Empty;
        public bool CallFailed { get; set; }
    }

    /// <summary>
    /// Compares a staff-captured photo of the physical collateral against
    /// the customer's original submission photo. Mirrors
    /// ICollateralImageAnalysisService's request/response + OpenRouter
    /// implementation pattern — same vision call, a comparison prompt
    /// instead of a classification prompt.
    /// </summary>
    public interface ICollateralVerificationService
    {
        Task<CollateralVerificationAnalysisResult> VerifyAsync(
            CollateralVerificationRequest request,
            CancellationToken cancellationToken = default);
    }
}