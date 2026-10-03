using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SkyTech.Models;
using SkyTech.Models.JobPostServices;
using SkyTech.Models.SkytechWorkers;

namespace SkyTech.Models
{
    public enum SelectLocation { Cape_Town, Durban, Johannesburg }
    public enum SelectDepartment { Sales_floor, Cash_Desk, Technical, Operation }

    public enum SelectEmploymentType { FullTime, Part_Time, }
    public enum CurrentPostStatus { Draft, Published, Deleted }

   
    public class JobPost
    {
        
        [Key]
        public int PostId { get; set; }
        [ForeignKey(nameof(Hrs))]
        public int HRId {  get; set; }
        [ValidateNever]
        public virtual HR Hrs { get; set; }

        [ForeignKey(nameof(ShopOwner))]
        public int OwnerId {  get; set; }
        [ValidateNever]
        public virtual ShopOwner ShopOwner { get; set; }

        // ── NEW ──
        [ForeignKey(nameof(StaffPosition))]
        public int PositionId { get; set; }
        [ValidateNever]
        public virtual StaffPosition StaffPosition { get; set; }


        public string? JobTitle { get; set; }

        public SelectDepartment Department { get; set; }

        [Required(ErrorMessage = "Location is required")]
        public SelectLocation Location { get; set; }

        [Required(ErrorMessage = "Employment type is required")]
        [Display(Name = "Employment Type")]
        public SelectEmploymentType EmploymentType { get; set; }

        [Range(0, 999999, ErrorMessage = "Please enter a valid salary amount")]
        public double? SalaryMin { get; set; }

        [Range(0, 999999, ErrorMessage = "Please enter a valid salary amount")]
        public double? SalaryMax { get; set; }

        [Required(ErrorMessage = "Responsibilities are required")]
        [StringLength(3000, ErrorMessage = "Responsibilities cannot exceed 3000 characters")]
        [DataType(DataType.MultilineText)]
        public string Responsibilities { get; set; }

        [DataType(DataType.MultilineText)]
        public string? ContractTerms { get; set; } = string.Empty; //onlly appears in the view of the contract

        public string RequiredCerts { get; set; }

        public string RequiredSkillsAndXP { get; set; }

       
        [Required(ErrorMessage = "Closing date is required")]
        [DataType(DataType.Date)]
        public DateTime? ClosingDate { get; set; }

        public CurrentPostStatus PostStatus { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
       
        // public ICollection <Applica>

        // One JobPost has many applications
        [ValidateNever]
        public virtual ICollection<ApplicationForm> ApplicationForms { get; set; }
            = new List<ApplicationForm>();


        // One JobPost can have many assessments
        [ValidateNever]
        public virtual ICollection<Assessment> Assessments { get; set; }
            = new List<Assessment>();
    }
}