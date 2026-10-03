using System.ComponentModel.DataAnnotations;
namespace SkyTech.Models
{
    public class QuizQuestion
    {
        [Key]
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string OptionC { get; set; } = string.Empty;
        public string OptionD { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public int AssessmentId { get; set; }

        public Assessment? Assessment { get; set; }
    }
}
