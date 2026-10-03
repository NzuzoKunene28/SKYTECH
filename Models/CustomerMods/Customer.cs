using SkyTech.Models;
﻿using SkyTech.Models.Repair_Item_Services;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;

namespace SkyTech.Models
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }

        [Required, MaxLength(100)]
        public string FirstName { get; set; }

        [MaxLength(100)]
        public string LastName { get; set; }

        [EmailAddress, StringLength(255)]
        public string Email { get; set; }

        [Phone, StringLength(10)]
        public string PhoneNumber { get; set; }

        [MaxLength(200)]
        public string Address { get; set; }

        [StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
        public string Password { get; set; }

        [NotMapped]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        public string UserType { get; set; }
        public DateTime RegisteredOn { get; set; }

        public bool? TestWritten { get; set; }

        public virtual ICollection<ProposeItem> ProposeItems { get; set; } = new List<ProposeItem>();



        public ICollection<Repair_Request> RepairRequests { get; set; }
    }
}
