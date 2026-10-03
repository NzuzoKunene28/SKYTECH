// Services/OpenRouterCvExtractionService.cs
using SkyTech.Models;
using SkyTech.Models.JobPostServices;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SkyTech.JobPostServices
{
    public class OpenRouterCvExtractionService : ICvExtractionService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public OpenRouterCvExtractionService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public async Task<CvExtractionResult> ExtractFromTextAsync(string cvText)
        {

            var apiKey = _config["OpenRouterSettings:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new Exception("OpenRouterSettings:ApiKey is missing or empty in configuration.");

            var prompt = $@"
Extract the following fields from the CV text below. If a field is not present,
return an empty string for it — do not guess or invent information.

CV Text:
{cvText}

Return ONLY valid JSON, no markdown fences, with these exact keys:
{{
  ""firstName"": """",
  ""lastName"": """",
  ""phoneNumber"": """",
  ""idNumber"": """",
  ""email"": """",
  ""location"": """",
  ""experience"": """",
  ""certificates"": """",
  ""skills"": """",
  ""links"": """"
}}";

            var body = new
            {
                model = _config["OpenRouterSettings:Model"],
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0.1
            };

            var req = new HttpRequestMessage(HttpMethod.Post, _config["OpenRouterSettings:Endpoint"])
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config["OpenRouterSettings:ApiKey"]);

            var response = await _http.SendAsync(req);
            var raw = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new Exception($"OpenRouter API error {(int)response.StatusCode}: {raw}");

            using var doc = JsonDocument.Parse(raw);
            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            content = content!.Trim();
            if (content.StartsWith("```"))
            {
                content = content.Trim('`');
                if (content.StartsWith("json")) content = content.Substring(4);
                content = content.Trim();
            }

            return JsonSerializer.Deserialize<CvExtractionResult>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }
    }
}