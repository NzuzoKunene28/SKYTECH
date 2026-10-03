namespace SkyTech.Services
{
        public interface ISkillMatchService
        {
            Task<SkillMatchResult> MatchSkillsAsync(string requiredSkillsText, string candidateSkillsText);
        }
}
