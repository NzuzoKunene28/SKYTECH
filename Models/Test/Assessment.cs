using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SkyTech.Models;
using SkyTech.Models.JobPostServices;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models
{
    public class Assessment
    {
        [Key]
        public int AssessmentId { get; set; }

        [ForeignKey(nameof(JobPosts))]
        public int PostId { get; set; }
        [ValidateNever]
        public virtual JobPost JobPosts { get; set; }

        public string JobTitle { get; set; } = "";

        
        public bool IsPublished { get; set; } = false;

        [Required]
        [Range(1, 100, ErrorMessage = "Pass mark must be between 1 and 100.")]
        public int PassMark { get; set; }

        [Required]
        [Range(1, 180)]
        public int TimeLimit { get; set; }
        [ValidateNever]
        public ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();
        [ValidateNever]
        public ICollection<ApplicationForm> ApplicationForms { get; set; } //= new List<ApplicationForm>();


    }
}