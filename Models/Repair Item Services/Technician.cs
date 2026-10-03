using SkyTech.Models.Documents;
using SkyTech.Models.Repair_Item_Services;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;


namespace SkyTech.Models.Repair_Services
{
     public enum AvailabilityStatus { Available, Unavailable, Busy }

    public class Technician
    {
        [Key]
        public int TechnicianId { get; set; }

        public string TechnicianFirstName { get; set; }  
        public string TechnicianLastName { get; set; }    

        public string FullName => $"{TechnicianFirstName} {TechnicianLastName}";

        public string Email { get; set; }

        public string Password { get; set; }

        [NotMapped]
        public string ConfirmPassword { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string UserType { get; set; }
        public DateTime RegisteredOn { get; set; }
        public AvailabilityStatus Status { get; set; }

        [ForeignKey("CategoryId")]
        public int CategoryId { get; set; }

        [ValidateNever]
        public virtual Repair_Item_Category Category { get; set; }
    }


}
