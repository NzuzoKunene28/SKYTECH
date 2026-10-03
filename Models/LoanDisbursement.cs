using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models.SkytechWorkers
{
    /// <summary>
    /// Records the payout of an approved loan to the customer's bank
    /// account via PayStackTransferService. One row per successful (or
    /// attempted) disbursement — kept even on failure, for audit.
    /// </summary>
    public class LoanDisbursement
    {
        [Key]
        public int LoanDisbursementId { get; set; }

        [ForeignKey(nameof(LoanApplication))]
        public int LoanApplicationId { get; set; }
        [ValidateNever]
        public LoanApplication LoanApplication { get; set; } = null!;

        [ForeignKey(nameof(ShopOwner))]
        public int OwnerId { get; set; }
        [ValidateNever]
        public ShopOwner ShopOwner { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string AccountHolderName { get; set; } = string.Empty;

        /// <summary>ASSUMPTION: full account number is stored since PayStack
        /// requires it to create the recipient and a shop owner may need to
        /// retry a failed transfer. If this needs masking/encryption at
        /// rest, that's a separate data-protection decision — flag if so.</summary>
        [Required]
        [StringLength(50)]
        public string AccountNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string BankCode { get; set; } = string.Empty;

        [StringLength(100)]
        public string? BankName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountDisbursed { get; set; }

        [StringLength(100)]
        public string? RecipientCode { get; set; }

        [StringLength(100)]
        public string? TransferCode { get; set; }

        [StringLength(50)]
        public string? TransferStatus { get; set; }

        public bool Success { get; set; }

        [StringLength(500)]
        public string? FailureMessage { get; set; }

        public DateTime AttemptedOn { get; set; } = DateTime.UtcNow;
    }
}