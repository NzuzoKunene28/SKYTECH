using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SkyTech.Services
{
    public class OpenRouterSkillMatchService : ISkillMatchService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public OpenRouterSkillMatchService(
            HttpClient http,
            IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public async Task<SkillMatchResult> MatchSkillsAsync(
            string requiredSkillsText,
            string candidateSkillsText)
        {
            if (string.IsNullOrWhiteSpace(requiredSkillsText) ||
                string.IsNullOrWhiteSpace(candidateSkillsText))
            {
                return new SkillMatchResult();
            }

            var prompt = $@"
You are a job application skill-matching system.

Your ONLY job is to compare the skills required by the job with the skills written by the applicant.

IMPORTANT RULES:

1. The job requirement text may contain BOTH skills and experience requirements.
2. IGNORE all experience requirements.
3. IGNORE years of experience.
4. IGNORE qualifications.
5. IGNORE certificates.
6. IGNORE responsibilities.
7. IGNORE job title.
8. IGNORE salary.
9. IGNORE every other requirement that is not a skill.
10. Only compare actual skills.
11. Only return skills that appear in the JOB REQUIREMENTS.
12. Do NOT invent skills.
13. Do NOT create new skills from the applicant's text.
14. A synonym or clearly equivalent skill may count as a match.
15. Do not count the same required skill more than once.
16. Return the ORIGINAL wording of the required skill when possible.
17. The applicant must genuinely demonstrate the skill.

JOB REQUIREMENTS:
-----------------
{requiredSkillsText}

APPLICANT'S SKILLS:
-------------------
{candidateSkillsText}

Now identify ONLY the required skills that are genuinely matched by the applicant.

Return ONLY valid JSON in exactly this format:

{{
    ""matchedSkills"": [
        ""skill from job requirements"",
        ""another skill from job requirements""
    ]
}}

If there are no matching skills, return:

{{
    ""matchedSkills"": []
}}

Do not return a matchCount.
Do not return explanations.
Do not return markdown.
";

            var body = new
            {
                model = _config["OpenRouterSettings:Model"],
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                temperature = 0
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                _config["OpenRouterSettings:Endpoint"])
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json")
            };

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _config["OpenRouterSettings:ApiKey"]);

            var response = await _http.SendAsync(request);

            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"OpenRouter API error {(int)response.StatusCode}: {raw}");
            }

            using var document = JsonDocument.Parse(raw);

            var content = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new Exception("AI returned an empty response.");
            }

            content = content.Trim();

            // Remove markdown fences if the model accidentally returns them
            if (content.StartsWith("```"))
            {
                content = content
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();
            }

            var result = JsonSerializer.Deserialize<SkillMatchResult>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (result == null)
            {
                throw new Exception("Could not read AI skill matching result.");
            }

            // Remove duplicates.
            result.MatchedSkills = result.MatchedSkills
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return result;
        }
    }
}