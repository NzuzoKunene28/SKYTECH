using DocumentFormat.OpenXml.Drawing;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SkyTech.Models.SkytechWorkers;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models
{
    public enum IsTheStaffMemberAvailable
    {
        OnLeave, Active, Suspended, Terminated
    }
    public class Staff
    {
        //=====PKs and FKs======
        [Key]
        public int StaffId { get; set; }

        [ForeignKey(nameof(Owner))]
        public int OwnerId { get; set; }
        [ValidateNever]
        public virtual ShopOwner Owner { get; set; }

        [ForeignKey(nameof(Positions))]
        public int PositionId { get; set; }
        [ValidateNever]
        public virtual StaffPosition Positions { get; set; }

        [ForeignKey(nameof(Branches))]
        public int BranchId { get; set; }
        [ValidateNever]
        public virtual Branch Branches { get; set; }

        [ForeignKey(nameof(Roles))]
        public int RoleId { get; set; }
        [ValidateNever]
        public virtual Role Roles { get; set; }

        [Required, MaxLength(100)]
        public string Username { get; set; }
        [Required, MaxLength(100)] public string FirstName { get; set; }
        [Required, MaxLength(100)] public string LastName { get; set; }

        [Required, MaxLength(100), EmailAddress]
        public string Email { get; set; }

        [Required, MaxLength(15)]
        public string PhoneNumber { get; set; }


        public IsTheStaffMemberAvailable StaffAvailability { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [RegularExpression(@".{8,}")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required")]
        [NotMapped]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        public string UserType { get; set; }
        public DateTime DateEmployed { get; set; }

        public SelectEmploymentType EmploymentType { get; set; }

        public string? ReferencePhotoPath { get; set; }
        public string? ReferenceEmbedding { get; set; }
    }
}