using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkyTech.Models.ServicesMods;
using SkyTech.Models.SkytechWorkers;

namespace SkyTech.Services
{
    /// <summary>
    /// Calls OpenRouter's chat-completions endpoint with the collateral
    /// photo + a classification prompt, using the free vision-capable model
    /// nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free.
    ///
    /// ASSUMPTION: config lives under an "OpenRouterSettings" section in
    /// appsettings.json, matching the existing services this mirrors:
    ///
    ///   "OpenRouterSettings": {
    ///     "ApiKey": "...",
    ///     "BaseUrl": "https://openrouter.ai/api/v1",
    ///     "CollateralVisionModel": "nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free"
    ///   }
    ///
    /// The model slug is read from config (not hard-coded) per the design
    /// note that the free model lineup rotates and swapping the slug
    /// should be a one-line config change.
    /// </summary>
    public class OpenRouterCollateralImageAnalysisService : ICollateralImageAnalysisService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenRouterCollateralImageAnalysisService> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _model;

        // NOTE: category list is intentionally copied verbatim from the
        // business rules given for this feature (it's the company's own
        // accepted-collateral policy, not third-party content), so the
        // model sees exactly the categories staff already use.
        private const string SystemPrompt =
            "You are a collateral-eligibility screener for a loan application system. " +
            "You will be shown a single photo of an item a customer is offering as loan collateral, " +
            "along with the customer's declared item name, declared value, claimed condition, and " +
            "serial number if provided. " +
            "This company ONLY accepts collateral from these categories: " +
            "(1) Electronics: cellphones, smartphones, tablets, smartwatches, laptops, desktop computers, " +
            "monitors, computer components, cameras, gaming consoles, gaming accessories, TVs, decoders, projectors. " +
            "(2) Audio & Entertainment: speakers, sound systems, amplifiers, subwoofers, home-theatre systems, " +
            "headphones, DJ equipment, mixers, PA equipment, studio equipment. " +
            "(3) Gaming: PlayStation, Xbox, Nintendo, gaming PCs, gaming monitors, controllers, video games, " +
            "VR equipment, gaming accessories. " +
            "(4) Tools & Equipment: power drills, impact drivers, grinders, saws, sanders, welders, compressors, " +
            "generators, hand-tool sets, professional tools. " +
            "(5) Home Appliances: refrigerators, freezers, washing machines, tumble dryers, dishwashers, " +
            "microwaves, ovens, stoves, air conditioners, heaters, vacuum cleaners. " +
            "(6) Small Appliances: kettles, toasters, blenders, air fryers, coffee machines, food processors, " +
            "mixers, irons. " +
            "(7) Cameras & Photography: digital cameras, DSLR cameras, mirrorless cameras, camera lenses, " +
            "camera flashes, tripods, camera accessories. " +
            "(8) Musical Instruments: guitars, keyboards, pianos, drums, amplifiers, microphones, other musical equipment. " +
            "(9) Jewellery & Watches: gold jewellery, silver jewellery, rings, necklaces, bracelets, earrings, " +
            "branded watches, other valuable watches. " +
            "(10) Automotive: car audio, car stereos, speakers, subwoofers, amplifiers, selected automotive " +
            "equipment/accessories. " +
            "(11) DIY & Hardware: hand tools, toolboxes, measuring equipment, ladders, hardware equipment, " +
            "security equipment, DIY equipment. " +
            "(12) Outdoor & Sports: bicycles, camping equipment, fitness equipment, sporting equipment, outdoor equipment. " +
            "(13) Home & Furniture: selected furniture, home décor, household equipment, selected valuable household items. " +
            "(14) Toys & Other: selected toys, collectibles. " +
            "If the item in the photo does NOT clearly belong to one of these categories, you MUST reject it, " +
"and your notes must explicitly say these items are not accepted as collateral at this company. " +
"If it DOES belong to one of these categories, judge it on: whether it's clearly identifiable in " +
"the photo, whether it looks consistent with the condition the customer claimed, and whether it " +
"looks genuine rather than obviously counterfeit, stolen, or illegal. " +
"The customer also self-declares a condition (Excellent, Used but good, or Damaged). If the item " +
"belongs to an accepted category AND the declared or visible condition is Damaged, still set " +
"\"verdict\" to \"Accepted\" (category-eligible items aren't rejected for being damaged), but your " +
"notes MUST clearly warn the customer that damaged items are unlikely to be approved and that " +
"final acceptance depends on staff's physical inspection. Do not soften or omit this warning. " +
"For non-damaged, category-eligible items, do not add this warning. "+
            "You do NOT have internet access and cannot verify a serial number against a real retail price — " +
            "do not attempt this; if a serial number was provided, just note that staff should verify it manually. " +
            "Respond with ONLY a JSON object, no other text, no markdown fences, in this exact shape: " +
            "{\"verdict\": \"Accepted\" | \"Rejected\", \"confidence\": <integer 0-100>, " +
            "\"category\": \"<the matched category name, or null if none>\", " +
            "\"notes\": \"<one or two sentence explanation>\"}. " +
            "\"confidence\" reflects how confident you are in your read of the photo and category match, " +
            "not a probability that the loan will be approved. " +
            "If your confidence is below 60, you are UNCERTAIN — say so plainly in your notes and do not " +
            "state a firm accept or reject conclusion, since a low-confidence read of either kind is unreliable " +
            "and a human will make the final call regardless of what you put in \"verdict\".";

        public OpenRouterCollateralImageAnalysisService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<OpenRouterCollateralImageAnalysisService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _apiKey = configuration["OpenRouterSettings:ApiKey"]
                ?? throw new InvalidOperationException("OpenRouterSettings:ApiKey is not configured.");
            _baseUrl = configuration["OpenRouterSettings:BaseUrl"] ?? "https://openrouter.ai/api/v1";
            _model = configuration["OpenRouterSettings:CollateralVisionModel"]
                ?? "nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free";
        }

        public async Task<CollateralImageAnalysisResult> AnalyzeAsync(
            CollateralImageAnalysisRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var base64Image = Convert.ToBase64String(request.ImageBytes);
                var dataUrl = $"data:{request.ImageContentType};base64,{base64Image}";

                var serialText = string.IsNullOrWhiteSpace(request.SerialNumber) ? "not provided" : request.SerialNumber;
                var userText =
                    $"Item name/description: {request.Description}\n" +
                    $"Declared value: {request.DeclaredValue:C}\n" +
                    $"Condition claimed by customer: {request.Condition}\n" +
                    $"Serial number: {serialText}";

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

                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("OpenRouter collateral scan failed with {StatusCode}: {Body}", response.StatusCode, errorBody);
                    return new CollateralImageAnalysisResult
                    {
                        Verdict = AiScanResult.Pending,
                        Notes = "The automated scan could not be completed. Please retry or flag for manual review.",
                        CallFailed = true
                    };
                }

                var completion = await response.Content.ReadFromJsonAsync<OpenRouterChatCompletionResponse>(cancellationToken: cancellationToken);
                var rawContent = completion?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;

                _logger.LogInformation("OpenRouter RAW RESPONSE: {RawContent}", rawContent);

                return ParseModelResponse(rawContent, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling OpenRouter for collateral image analysis.");
                return new CollateralImageAnalysisResult
                {
                    Verdict = AiScanResult.Pending,
                    Notes = "The automated scan could not be completed due to an unexpected error. Please retry or flag for manual review.",
                    CallFailed = true
                };
            }
        }

        /// <summary>
        /// Parses the model's JSON reply. Falls back to a keyword scan if the
        /// model didn't strictly follow the "JSON only" instruction — free
        /// models occasionally wrap output in prose or markdown fences.
        /// </summary>
        private CollateralImageAnalysisResult ParseModelResponse(string rawContent, CollateralImageAnalysisRequest request)
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
                var verdictText = root.TryGetProperty("verdict", out var v) ? v.GetString() : null;
                var notes = root.TryGetProperty("notes", out var n) ? n.GetString() : null;
                var category = root.TryGetProperty("category", out var cat) ? cat.GetString() : null;
                int? confidence = root.TryGetProperty("confidence", out var c) && c.TryGetInt32(out var confInt)
                    ? Math.Clamp(confInt, 0, 100)
                    : null;

                var verdict = verdictText?.Trim().ToLowerInvariant() switch
                {
                    "accepted" => AiScanResult.Accepted,
                    "rejected" => AiScanResult.Rejected,
                    _ => AiScanResult.NeedsManualReview
                };

                // Code-level safety net, not just a prompt instruction: a
                // confidence below 60 can't be trusted to accept OR reject,
                // so it's forced to NeedsManualReview regardless of what the
                // model put in "verdict". This holds even if the model
                // ignores the prompt's instruction to hedge.
                const int confidenceThreshold = 60;
                if (confidence is null || confidence < confidenceThreshold)
                {
                    verdict = AiScanResult.NeedsManualReview;
                }

                return new CollateralImageAnalysisResult
                {
                    Verdict = verdict,
                    Confidence = confidence,
                    MatchedCategory = string.IsNullOrWhiteSpace(category) ? null : category,
                    //Notes = notes ?? "No explanation was returned by the model.",
                    Notes = AppendDamagedConditionCaveat(notes ?? "No explanation was returned by the model.", verdict, request),
                    CallFailed = false
                };
            }
            catch (JsonException)
            {
                // No structured confidence number exists in this path, so
                // there's no reliable basis for an accept/reject call —
                // route straight to manual review rather than guessing from
                // keywords in unstructured text.
                _logger.LogWarning("Could not parse OpenRouter response as JSON, routing to manual review. Raw: {Raw}", rawContent);

                return new CollateralImageAnalysisResult
                {
                    Verdict = AiScanResult.NeedsManualReview,
                    Notes = "The automated scan's response could not be interpreted reliably. A staff member will review this item manually.",
                    CallFailed = false
                };
            }
        }

        private static string AppendDamagedConditionCaveat(string notes, AiScanResult verdict, CollateralImageAnalysisRequest request)
        {
            const string caveat = "Note: this item was declared as Damaged. Category-eligible items aren't rejected " +
                                   "for being damaged, but damaged collateral is unlikely to be approved once staff " +
                                   "physically inspect it — final acceptance is their decision, not this scan's.";

            var alreadyMentionsDamage = notes.Contains("damag", StringComparison.OrdinalIgnoreCase);

            if (verdict == AiScanResult.Accepted && request.Condition == CollateralCondition.Damaged && !alreadyMentionsDamage)
            {
                return $"{notes} {caveat}";
            }

            return notes;
        }
    }
}