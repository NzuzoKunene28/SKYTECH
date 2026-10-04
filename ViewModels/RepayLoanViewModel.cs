using System.ComponentModel.DataAnnotations;
using SkyTech.Models.SkytechWorkers;

namespace SkyTech.Models.ViewModels
{
    public class RepayLoanViewModel
    {
        public int LoanId { get; set; }
        public decimal OutstandingBalance { get; set; }
        public decimal TotalRepayable { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime DueDate { get; set; }

        // ── NEW: full collateral items instead of just a joined string ──
        public List<CollateralDisplayItem> Collateral { get; set; } = new();

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Enter an amount greater than 0.")]
        [Display(Name = "Payment Amount")]
        public decimal PaymentAmount { get; set; }

        [Required(ErrorMessage = "Select a payment method.")]
        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Card;


        [Required(ErrorMessage = "Enter your transfer reference to confirm this is your loan.")]
        [Display(Name = "Transfer Reference")]
        public string TransferReferenceInput { get; set; } = string.Empty;

        public List<BalancePointViewModel> BalanceHistory { get; set; } = new();

        public string CollectionToken { get; set; } = string.Empty;
    }

    public class CollateralDisplayItem
    {
        public string Description { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public decimal DeclaredValue { get; set; }
        public CollateralCondition Condition { get; set; }
    }

    public class BalancePointViewModel
    {
        public DateTime Date { get; set; }
        public decimal Balance { get; set; }
    }
}