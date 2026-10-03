
using SkyTech.Models;
using System.ComponentModel.DataAnnotations;

namespace SkyTech.ViewModels
{
    public class AddQuestionViewModel
    {
        public int AssessmentId { get; set; }

        public string AssessmentTitle { get; set; } = "";

        [Required]
        public string QuestionText { get; set; } = "";

        [Required]
        public string OptionA { get; set; } = "";

        [Required]
        public string OptionB { get; set; } = "";

        [Required]
        public string OptionC { get; set; } = "";

        [Required]
        public string OptionD { get; set; } = "";

        [Required]
        public string CorrectAnswer { get; set; } = "A";

        public List<QuizQuestion> ExistingQuestions { get; set; }
            = new();
    }
}
