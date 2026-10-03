using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SkyTech.Models.Auth
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string UserEmail { get; set; }
    }

    public class VerifyOtpViewModel
    {
        [Required]
        public string UserEmail { get; set; }

        [Required(ErrorMessage = "OTP is required")]
        [Display(Name = "Enter OTP")]
        public string OtpCode { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string UserEmail { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
        [RegularExpression(@".{8,}", ErrorMessage = "Password must be at least 8 characters long.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }



        [Required(ErrorMessage = "Confirm Password is required")]
        [NotMapped]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }

}