using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkyTech.Models;
using SkyTech.Models.ServicesMods; // OpenRouterChatCompletionResponse lives here per OpenRouterCollateralImageAnalysisService

namespace SkyTech.Services
{
    /// <summary>
    /// Calls OpenRouter's chat-completions endpoint with the returned-item's
    /// condition photo + a damage-assessment prompt. Mirrors
    /// OpenRouterCollateralImageAnalysisService's structure (same config
    /// section, same request/response plumbing) — different domain, same
    /// pattern.
    ///
    /// ASSUMPTION: reuses the existing "OpenRouterSettings" section, with one
    /// new key added alongside CollateralVisionModel:
    ///
    ///   "OpenRouterSettings": {
    ///     "ApiKey": "...",
    ///     "BaseUrl": "https://openrouter.ai/api/v1",
    ///     "CollateralVisionModel": "...",
    ///     "LeaseDamageVisionModel": "nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free"
    ///   }
    ///
    /// Defaults to the same free vision model as the collateral scanner if
    /// the key isn't set, since it's already vision-capable and already
    /// proven to work in this codebase.
    /// </summary>

    public class OpenRouterLeaseDamageAssessmentService : ILeaseDamageAssessmentService
    {

        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenRouterLeaseDamageAssessmentService> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _model;

        private const int ConfidenceThreshold = 60;


        private const string SystemPrompt =
            "You are a physical-condition and damage assessor for an equipment leasing company, completing " +
            "an end-of-lease inspection. You will be shown a single photo of a returned item, along with its " +
            "name and the original security deposit amount (in Rand) held against it.\n\n" +
            "Classify the item's condition into exactly one of these four categories:\n" +
            "\"Good\" - no visible damage or wear beyond what's normal for the item's age and use.\n" +
            "\"MinorWear\" - light cosmetic wear consistent with normal use (scuffs, minor scratches), no functional damage.\n" +
            "\"Moderate\" - visible damage such as cracks, dents, or missing minor parts that would need repair or reduce resale value.\n" +
            "\"Severe\" - major damage: broken, clearly non-functional, or missing significant parts.\n\n" +
            "Suggest a damage fee in Rand to deduct from the security deposit, scaled to the severity and " +
            "never exceeding the deposit amount: Good should suggest 0, MinorWear a small fraction of the " +
            "deposit, Moderate a substantial fraction, and Severe up to the full deposit amount.\n\n" +
            "You cannot physically test the item (power it on, check internal function, etc.) from a photo — " +
            "your notes must say that any issue that isn't visible requires staff to verify physically before " +
            "the fee is finalized.\n\n" +
            "Respond with ONLY a JSON object, no other text, no markdown fences, in this exact shape: " +
            "{\"condition\": \"Good\" | \"MinorWear\" | \"Moderate\" | \"Severe\", " +
            "\"confidence\": <integer 0-100>, \"suggestedDamageFee\": <number>, " +
            "\"notes\": \"<one or two sentence explanation>\"}. " +
            "\"confidence\" reflects how confident you are in your read of the photo, not a judgment about " +
            "whether staff should trust the fee. If your confidence is below 60, say so plainly in your " +
            "notes, since a low-confidence read is unreliable and staff will confirm or override the fee " +
            "regardless of what you put in \"condition\".";

        public OpenRouterLeaseDamageAssessmentService(
           HttpClient httpClient,
           IConfiguration configuration,
           ILogger<OpenRouterLeaseDamageAssessmentService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _apiKey = configuration["OpenRouterSettings:ApiKey"]
                ?? throw new InvalidOperationException("OpenRouterSettings:ApiKey is not configured.");
            _baseUrl = configuration["OpenRouterSettings:BaseUrl"] ?? "https://openrouter.ai/api/v1";
            _model = configuration["OpenRouterSettings:LeaseDamageVisionModel"]
                ?? configuration["OpenRouterSettings:CollateralVisionModel"]
                ?? "nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free";
        }

        public async Task<LeaseDamageAssessmentResult> AnalyzeAsync(
           byte[] imageBytes, string imageContentType, string itemName, double securityDeposit)
        {
            try
            {
                var base64Image = Convert.ToBase64String(imageBytes);
                var dataUrl = $"data:{imageContentType};base64,{base64Image}";

                var userText =
                    $"Item name: {itemName}\n" +
                    $"Original security deposit held: R{securityDeposit:N2}";

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
                                new { type = "image_url", image_url = new { url = dataUrl } }
                            }
                        }
                    },
                    max_tokens = 400
                };

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions");
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("OpenRouter lease damage scan failed with {StatusCode}: {Body}", response.StatusCode, errorBody);
                    return FallbackResult();
                }

                var completion = await response.Content.ReadFromJsonAsync<OpenRouterChatCompletionResponse>();
                var rawContent = completion?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;

                _logger.LogInformation("OpenRouter RAW RESPONSE (lease damage): {RawContent}", rawContent);

                return ParseModelResponse(rawContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling OpenRouter for lease damage assessment.");
                return FallbackResult();
            }
        }


        private LeaseDamageAssessmentResult ParseModelResponse(string rawContent)
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

                var conditionText = root.TryGetProperty("condition", out var cond) ? cond.GetString() : null;
                var notes = root.TryGetProperty("notes", out var n) ? n.GetString() : null;
                int confidence = root.TryGetProperty("confidence", out var c) && c.TryGetInt32(out var confInt)
                    ? Math.Clamp(confInt, 0, 100)
                    : 0;
                double suggestedFee = root.TryGetProperty("suggestedDamageFee", out var f) && f.TryGetDouble(out var feeVal)
                    ? Math.Max(0, feeVal)
                    : 0;

                var condition = conditionText?.Trim().ToLowerInvariant() switch
                {
                    "good" => ReturnConditionAssessment.Good,
                    "minorwear" => ReturnConditionAssessment.MinorWear,
                    "moderate" => ReturnConditionAssessment.Moderate,
                    "severe" => ReturnConditionAssessment.Severe,
                    _ => ReturnConditionAssessment.Moderate // unclear read — neutral default, staff confirms regardless
                };

                string finalNotes = notes ?? "No explanation was returned by the model.";
                if (confidence < ConfidenceThreshold)
                {
                    finalNotes += " Note: the AI's confidence in this read is low — please verify the item's condition in person before confirming the fee.";
                }

                return new LeaseDamageAssessmentResult
                {
                    Condition = condition,
                    ConfidencePercent = confidence,
                    SuggestedDamageFee = suggestedFee,
                    Notes = finalNotes
                };
            }
            catch (JsonException)
            {
                _logger.LogWarning("Could not parse OpenRouter response as JSON for lease damage assessment. Raw: {Raw}", rawContent);
                return FallbackResult();
            }
        }

        // Used whenever the call fails outright or the response can't be
        // parsed — a safe, low-confidence, zero-fee default. Staff always
        // confirms the final fee via ConfirmInspectionAsync regardless, so
        // this never gets applied without a human looking at it first.
        private static LeaseDamageAssessmentResult FallbackResult() => new()
        {
            Condition = ReturnConditionAssessment.Moderate,
            ConfidencePercent = 0,
            SuggestedDamageFee = 0,
            Notes = "The automated scan could not be completed. Please assess this item's condition manually."
        };
    }
}
