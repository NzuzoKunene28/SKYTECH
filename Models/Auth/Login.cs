using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SkyTech.Models
{
    public class Login
    {
        [Required(ErrorMessage = "Enter your username!")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
        public string Password { get; set; }
    }
}

