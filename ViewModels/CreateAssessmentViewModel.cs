using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace SkyTech.ViewModels
{
    public class CreateAssessmentViewModel
    {
        [Required]
        public int PostId { get; set; }

        [Required]
        public string JobTitle { get; set; } = "";

        [Required]
        [Range(1, 100, ErrorMessage = "Enter a pass mark between 1 and 100.")]
        public int PassMark { get; set; }

        [Required]
        [Range(1, 180)]
        public int TimeLimit { get; set; }

        public List<SelectListItem> JobPosts { get; set; }
            = new();
    }
}
