// Models/JobRoleKnowledge.cs
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SkyTech.Models.SkytechWorkers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models.JobPostServices
{
    public class JobRoleKnowledge
    {
        [Key]
        public int RoleId { get; set; }

        // ── NEW ──
        [ForeignKey(nameof(StaffPosition))]
        public int PositionId { get; set; }
        [ValidateNever]
        public virtual StaffPosition StaffPosition { get; set; }

        public string? JobTitle { get; set; }


        public int DepartmentId { get; set; }
        [ValidateNever]
        public Department? Department { get; set; }  // ← Add ?

        [ForeignKey(nameof(HRs))]
        public int? HRId { get; set; }
        /* public virtual  HR HRs { get; set; }*/

        public int MaxHeadcount { get; set; }


        [Required(ErrorMessage = "Responsibilities are required")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Company Responsibilities (one per line)")]
        public string CompanyResponsibilities { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Required Skills (one per line)")]
        public string RequiredSkills { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Required Experience (one per line)")]
        public string RequiredExperience { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Required Qualifications (one per line)")]
        public string RequiredQualifications { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Other Company Information")]
        public string OtherCompanyInformation { get; set; }

        // Lets HR retire a role without deleting history
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;


        [ValidateNever]
        public ICollection<HR> HRs { get; set; } = new List<HR>();
    }
}