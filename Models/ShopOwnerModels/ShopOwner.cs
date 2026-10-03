using SkyTech.Models.Documents;
using SkyTech.Models.SkytechWorkers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models
{
    public enum ApplicationStat { Pending, Approved, Rejected }

    public class ShopOwner
    {
        [Key]
        public int OwnerId { get; set; }

        [ForeignKey(nameof(Branch))]
        public int BranchId { get; set; }

        public virtual Branch Branch { get; set; }

        [Required, StringLength(13, MinimumLength = 13)]
        public string IdBookNumber { get; set; }

        [Required, MaxLength(50)] public string Names { get; set; }
        [Required, MaxLength(50)] public string Surname { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, MaxLength(200)] public string ResidentialAddress { get; set; }
        [Required, MaxLength(100)] public string AccountNumber { get; set; }
        [Required] public string BankType { get; set; }

        [Required, MaxLength(200)] public string ShopAddress { get; set; }

        [Required(ErrorMessage = "Shop/Branch name is required")]
        [MaxLength(100)]
        public string ShopName { get; set; }
        // public string Shoptype { get; set; }

        // Navigation properties for related documents
        public virtual ICollection<BusRegCertificate> BussRegCertificates { get; set; } = new List<BusRegCertificate>();
        public virtual ICollection<OwnerIdentityBook> OwnerIdentityBooks { get; set; } = new List<OwnerIdentityBook>();


        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [NotMapped, DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        public string UserType { get; set; }
        public ApplicationStat ApplicationStatus { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime RegisteredOn { get; set; } = DateTime.Now;

    }

}
