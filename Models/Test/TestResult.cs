using SkyTech.Models.JobPostServices;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SkyTech.Models
{
    public class TestResult
    {
        [Key]
        public int ResultId { get; set; }

        [ForeignKey(nameof(ApplicationForms))]
        public int ApplicationId { get; set; }
        public int Score { get; set; }
        public bool Passed { get; set; }
        public DateTime DateWritten { get; set; }
        public virtual ApplicationForm ApplicationForms { get; set; }
    }
}
