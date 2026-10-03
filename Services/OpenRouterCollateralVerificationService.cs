using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkyTech.Models.ServicesMods;
using SkyTech.Models.SkytechWorkers;
//using SkyTech.Models.SkytechWorkers.SkyTech.Models.SkytechWorkers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SkyTech.Services
{
    /// <summary>
    /// Calls OpenRouter's chat-completions endpoint with TWO images — the
    /// customer's original collateral photo and a new photo staff just took
    /// of the physical item — and asks the model to judge whether they're
    /// the same item and whether condition still matches.
    ///
    /// ASSUMPTION: reuses the same "OpenRouterSettings" config section as
    /// OpenRouterCollateralImageAnalysisService. Adds one optional key for
    /// a dedicated model slug, falling back to the existing
    /// CollateralVisionModel if not set, since this is the same kind of
    /// vision task on the same provider:
    ///
    ///   "OpenRouterSettings": {
    ///     "ApiKey": "...",
    ///     "BaseUrl": "https://openrouter.ai/api/v1",
    ///     "CollateralVisionModel": "nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free",
    ///     "CollateralVerificationModel": ""  // optional override, falls back to CollateralVisionModel
    ///   }
    /// </summary>
    public class OpenRouterCollateralVerificationService : ICollateralVerificationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenRouterCollateralVerificationService> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _model;

        private const string SystemPrompt =
            "You are a collateral re-verification screener for a loan company. You will be shown TWO photos " +
            "of what is claimed to be the SAME physical item: the FIRST photo was submitted by the customer " +
            "when they applied for the loan online; the SECOND photo was just taken by staff of the physical " +
            "item the customer brought into the shop in person. " +
            "You will also be told the item's description, the category it was originally matched to, and the " +
            "condition the customer declared at application time. " +
            "Your job is to judge TWO separate things: " +
            "(1) IDENTITY — do the two photos show the same physical item? Compare distinguishing visual " +
            "features (shape, color, brand markings, visible scratches or wear patterns, accessories shown). " +
            "Different lighting, angle, or background across the two photos is normal and does NOT by itself " +
            "mean it's a different item. " +
            "(2) CONDITION — if it is the same item, does its condition in the second photo look consistent " +
            "with, or worse than, the condition shown/declared in the first photo? Ordinary case-on/case-off " +
            "differences are not condition changes; new visible cracks, dents, or missing parts are. " +
            "Respond with ONLY a JSON object, no other text, no markdown fences, in this exact shape: " +
            "{\"result\": \"Match\" | \"Mismatch\" | \"ConditionDegraded\", \"confidence\": <integer 0-100>, " +
            "\"notes\": \"<one or two sentence explanation for staff\"}. " +
            "Use \"Mismatch\" only when you believe it is a DIFFERENT physical item, not merely a different " +
            "condition. Use \"ConditionDegraded\" when it is the SAME item but its condition has visibly worsened " +
            "since the first photo. Use \"Match\" when it is the same item in the same or better condition. " +
            "\"confidence\" reflects how confident you are in your visual read, not a probability that the loan " +
            "will be approved. If your confidence is below 60, say so plainly in your notes — a human will make " +
            "the final call regardless of what you put in \"result\".";

        public OpenRouterCollateralVerificationService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<OpenRouterCollateralVerificationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _apiKey = configuration["OpenRouterSettings:ApiKey"]
                ?? throw new InvalidOperationException("OpenRouterSettings:ApiKey is not configured.");
            _baseUrl = configuration["OpenRouterSettings:BaseUrl"] ?? "https://openrouter.ai/api/v1";

            var overrideModel = configuration["OpenRouterSettings:CollateralVerificationModel"];
            _model = string.IsNullOrWhiteSpace(overrideModel)
                ? (configuration["OpenRouterSettings:CollateralVisionModel"] ?? "nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free")
                : overrideModel;
        }

        public async Task<CollateralVerificationAnalysisResult> VerifyAsync(
            CollateralVerificationRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var originalDataUrl = ToDataUrl(request.OriginalImageBytes, request.OriginalImageContentType);
                var newDataUrl = ToDataUrl(request.NewImageBytes, request.NewImageContentType);

                var categoryText = string.IsNullOrWhiteSpace(request.OriginalAiMatchedCategory)
                    ? "not previously matched"
                    : request.OriginalAiMatchedCategory;

                var userText =
                    $"Item description: {request.Description}\n" +
                    $"Originally matched category: {categoryText}\n" +
                    $"Condition declared at application: {request.DeclaredConditionAtApplication}\n" +
                    "First image = customer's original submission. Second image = staff's new photo just taken.";

                var payload = new
                {
                    model = _model,
                    messages = new object[]
                    {
                        new { role = "system", content = SystemPrompt },
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new { type = "text", text = userText },
                                new { type = "image_url", image_url = new { url = originalDataUrl } },
                                new { type = "image_url", image_url = new { url = newDataUrl } }
                            }
                        }
                    },
                    max_tokens = 400
                };

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions");
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("OpenRouter collateral verification failed with {StatusCode}: {Body}", response.StatusCode, errorBody);
                    return new CollateralVerificationAnalysisResult
                    {
                        Result = CollateralVerificationResult.NeedsManualReview,
                        Notes = "The automated re-verification could not be completed. Please review manually.",
                        CallFailed = true
                    };
                }

                var completion = await response.Content.ReadFromJsonAsync<OpenRouterChatCompletionResponse>(cancellationToken: cancellationToken);
                var rawContent = completion?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;

                _logger.LogInformation("OpenRouter verification RAW RESPONSE: {RawContent}", rawContent);

                return ParseModelResponse(rawContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling OpenRouter for collateral verification.");
                return new CollateralVerificationAnalysisResult
                {
                    Result = CollateralVerificationResult.NeedsManualReview,
                    Notes = "The automated re-verification could not be completed due to an unexpected error. Please review manually.",
                    CallFailed = true
                };
            }
        }

        private static string ToDataUrl(byte[] bytes, string contentType) =>
            $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";

        /// <summary>
        /// Same fallback approach as OpenRouterCollateralImageAnalysisService:
        /// strip markdown fences if present, and route to NeedsManualReview
        /// (never guess Match/Mismatch) if parsing fails or confidence is
        /// too low to trust.
        /// </summary>
        private CollateralVerificationAnalysisResult ParseModelResponse(string rawContent)
        {
            var cleaned = rawContent.Trim();
            if (cleaned.StartsWith("```"))
            {
                cleaned = cleaned.Trim('`');
                var newlineIndex = cleaned.IndexOf('\n');
                if (newlineIndex >= 0 && cleaned[..newlineIndex].Trim().Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = cleaned[(newlineIndex + 1)..];
                }
            }

            try
            {
                using var doc = JsonDocument.Parse(cleaned);
                var root = doc.RootElement;
                var resultText = root.TryGetProperty("result", out var r) ? r.GetString() : null;
                var notes = root.TryGetProperty("notes", out var n) ? n.GetString() : null;
                int? confidence = root.TryGetProperty("confidence", out var c) && c.TryGetInt32(out var confInt)
                    ? Math.Clamp(confInt, 0, 100)
                    : null;

                var result = resultText?.Trim().ToLowerInvariant() switch
                {
                    "match" => CollateralVerificationResult.Match,
                    "mismatch" => CollateralVerificationResult.Mismatch,
                    "conditiondegraded" => CollateralVerificationResult.ConditionDegraded,
                    _ => CollateralVerificationResult.NeedsManualReview
                };

                // Same code-level safety net as the classification service:
                // low confidence can't be trusted either way, force manual review.
                const int confidenceThreshold = 60;
                if (confidence is null || confidence < confidenceThreshold)
                {
                    result = CollateralVerificationResult.NeedsManualReview;
                }

                return new CollateralVerificationAnalysisResult
                {
                    Result = result,
                    Confidence = confidence,
                    Notes = notes ?? "No explanation was returned by the model.",
                    CallFailed = false
                };
            }
            catch (JsonException)
            {
                _logger.LogWarning("Could not parse OpenRouter verification response as JSON, routing to manual review. Raw: {Raw}", rawContent);

                return new CollateralVerificationAnalysisResult
                {
                    Result = CollateralVerificationResult.NeedsManualReview,
                    Notes = "The automated re-verification's response could not be interpreted reliably. A staff member will review this item manually.",
                    CallFailed = false
                };
            }
        }
    }
}