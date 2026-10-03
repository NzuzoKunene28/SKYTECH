using SkyTech.Models.ServicesMods;

namespace SkyTech.Services
{
    /// <summary>
    /// Scans a single collateral photo and returns an accept/reject verdict
    /// plus the model's reasoning. Mirrors the existing IAIJobPostDraftService
    /// / ICvExtractionService interface + OpenRouter-implementation pattern.
    /// </summary>
    public interface ICollateralImageAnalysisService
    {
        Task<CollateralImageAnalysisResult> AnalyzeAsync(CollateralImageAnalysisRequest request, CancellationToken cancellationToken = default);
    }
}
