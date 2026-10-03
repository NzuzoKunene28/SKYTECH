using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using SkyTech.Models.SkytechWorkers;
using System.ComponentModel.DataAnnotations;

namespace SkyTech.Models.ViewModels
{
    /// <summary>
    /// ASSUMPTION: view models live in SkyTech.Models.ViewModels, a
    /// sibling of SkytechWorkers/ServicesMods. Adjust the namespace if
    /// this project already has a different convention for form-binding
    /// models vs. persisted entities.
    /// </summary>
    public class LoanApplyViewModel
    {
        [Required(ErrorMessage = "National ID is required.")]
        [StringLength(20, MinimumLength = 13, ErrorMessage = "Enter a valid national ID.")]
        [Display(Name = "National ID")]
        public string NationalId { get; set; } = string.Empty;

        [Required]
        [Range(1, 10_000_000, ErrorMessage = "Enter a requested amount between 1 and 10,000,000.")]
        [Display(Name = "Requested Loan Amount")]
        public decimal RequestedAmount { get; set; }

        [Required]
        [Range(0, 1_000_000, ErrorMessage = "Enter a valid monthly salary.")]
        [Display(Name = "Monthly Salary (gross)")]
        public decimal MonthlySalary { get; set; }

        [Required(ErrorMessage = "Proof of income (payslip or bank statement) is required.")]
        [Display(Name = "Proof of Income Document")]
        public IFormFile ProofOfIncomeDoc { get; set; } = null!;

        [Display(Name = "Loan Purpose")]
        public LoanPurpose? Purpose { get; set; }

        [Required(ErrorMessage = "Please indicate your employment status.")]
        [Display(Name = "Are you currently employed?")]
        public bool IsEmployed { get; set; }

        [Required(ErrorMessage = "Please select a branch.")]
        [Display(Name = "Branch")]
        public int BranchId { get; set; }

        /// <summary>Populated by the controller (GET Apply) for the dropdown; not bound from the form post itself.</summary>
        public List<SelectListItem>? AvailableBranches { get; set; }
    }

    public class EligibilityResultViewModel
    {
        public int LoanApplicationId { get; set; }
        public EligibilityResult EligibilityResult { get; set; }
        public int CreditScore { get; set; }
        public ClearanceStatus ClearanceStatus { get; set; }
        public decimal RequestedAmount { get; set; }
    }

    public class UploadCollateralViewModel
    {
        public int LoanApplicationId { get; set; }
        public decimal RequestedAmount { get; set; }

        /// <summary>Existing items already uploaded in a previous pass (user can add more before final submit).</summary>
        public List<CollateralItem> ExistingItems { get; set; } = new();

        [Required(ErrorMessage = "Add at least one collateral item.")]
        [MinLength(1, ErrorMessage = "Add at least one collateral item.")]
        public List<CollateralItemInputModel> NewItems { get; set; } = new() { new CollateralItemInputModel() };
    }

    public class CollateralItemInputModel
    {
        [Required(ErrorMessage = "Describe the item.")]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 10_000_000, ErrorMessage = "Enter a valid declared value.")]
        public decimal DeclaredValue { get; set; }

        [Required(ErrorMessage = "A photo of the item is required.")]
        public IFormFile Photo { get; set; } = null!;

        [StringLength(100)]
        [Display(Name = "Serial Number (if available)")]
        public string? SerialNumber { get; set; }

        [Required(ErrorMessage = "Select the item's condition.")]
        public CollateralCondition Condition { get; set; }
    }

    public class ReviewScanViewModel
    {
        public int LoanApplicationId { get; set; }
        public decimal RequestedAmount { get; set; }
        public decimal TotalDeclaredCollateralValue { get; set; }
        public List<CollateralItem> Items { get; set; } = new();

        /// <summary>True if every item has been scanned (Pending = not yet scanned; NeedsManualReview still counts as "done" — a human decides that one, but there's nothing left to wait on).</summary>
        public bool AllScansComplete => Items.All(i => i.AiScanResult != AiScanResult.Pending);

        public bool AnyRejected => Items.Any(i => i.AiScanResult == AiScanResult.Rejected);
        public bool AnyNeedsManualReview => Items.Any(i => i.AiScanResult == AiScanResult.NeedsManualReview);
    }
}