using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace SkyTech.Models.ViewModels
{
    public class DisburseViewModel
    {
        public int LoanApplicationId { get; set; }
        public decimal RequestedAmount { get; set; }

        [Required(ErrorMessage = "Enter the account holder's name.")]
        [StringLength(100)]
        [Display(Name = "Account Holder Name")]
        public string AccountHolderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter the account number.")]
        [StringLength(50)]
        [Display(Name = "Account Number")]
        public string AccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Select a bank.")]
        [Display(Name = "Bank")]
        public string BankCode { get; set; } = string.Empty;

        public List<SelectListItem> AvailableBanks { get; set; } = new();
    }
}