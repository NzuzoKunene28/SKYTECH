namespace SkyTech.Models.JobPostServices
{
    public interface ICvExtractionService
    {
        Task<CvExtractionResult> ExtractFromTextAsync(string cvText);
    }
}
