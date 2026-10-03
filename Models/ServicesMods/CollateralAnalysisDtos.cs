using SkyTech.Models.SkytechWorkers;

namespace SkyTech.Models.ServicesMods
{
    
    public class CollateralImageAnalysisRequest
    {
        public string Description { get; set; } = string.Empty;

        public byte[] ImageBytes { get; set; } = Array.Empty<byte>();

        public string ImageContentType { get; set; } = "image/jpeg";

        public decimal DeclaredValue { get; set; }

        public CollateralCondition Condition { get; set; }

        public string? SerialNumber { get; set; }
    }

    
    public class CollateralImageAnalysisResult
    {
        public AiScanResult Verdict { get; set; } = AiScanResult.Pending;

        public int? Confidence { get; set; }

        public string? MatchedCategory { get; set; }

        public string Notes { get; set; } = string.Empty;

        public bool CallFailed { get; set; }
    }

    public class OpenRouterChatCompletionResponse
    {
        public List<OpenRouterChoice> Choices { get; set; } = new();
    }

    public class OpenRouterChoice
    {
        public OpenRouterMessage Message { get; set; } = new();
    }

    public class OpenRouterMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}