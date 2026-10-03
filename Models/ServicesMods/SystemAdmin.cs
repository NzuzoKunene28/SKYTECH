using System;
using System.ComponentModel.DataAnnotations;

namespace SkyTech.Models
{
    public class SystemAdmin
    {
        [Key]
        public int AdminId { get; set; }

        [Required, MaxLength(100)] public string FirstName { get; set; }
        [MaxLength(100)] public string LastName { get; set; }
        [Required, MaxLength(100)] public string Email { get; set; }
        [Required] public string Password { get; set; }
        public string UserType { get; set; }
        public DateTime RegisteredOn { get; set; }
    }
}