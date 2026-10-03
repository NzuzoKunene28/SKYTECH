using SkyTech.Models;

namespace SkyTech.ViewModels
{
    public class AssessmentListViewModel
    {
        public int AssessmentId { get; set; }

        public string AssessmentTitle { get; set; } = string.Empty;

        public string JobTitle { get; set; } = string.Empty;

        public int QuestionCount { get; set; }

        public int PassMark { get; set; }

        public int TimeLimit { get; set; }

        public bool IsPublished { get; set; }
    }
}
