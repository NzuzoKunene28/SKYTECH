using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models.SkytechWorkers
{
    /// <summary>
    /// Repayment record for a disbursed LoanApplication (UC-05). Created
    /// once at disbursement time — see LoanDisbursementController.Disburse().
    /// One LoanApplication has at most one Loan, since a customer cannot
    /// re-disburse against an application that's already Disbursed.
    /// </summary>
    public class Loan
    {
        [Key]
        public int LoanId { get; set; }

        [ForeignKey(nameof(LoanApplication))]
        public int LoanApplicationId { get; set; }
        [ValidateNever]
        public LoanApplication LoanApplication { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrincipalAmount { get; set; }

        /// <summary>
        /// ASSUMPTION: flat interest rate applied once at disbursement
        /// (not compounding, not reducing-balance). Not specified in the
        /// UC-05 design notes — flagged for confirmation before this goes
        /// further, since it changes TotalRepayable and the balance graph.
        /// </summary>
        [Range(typeof(decimal), "0", "1")]
        [Column(TypeName = "decimal(5,4)")]
        public decimal InterestRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalRepayable { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OutstandingBalance { get; set; }

        public DateTime DisbursedOn { get; set; } = DateTime.UtcNow;

        /// <summary>ASSUMPTION: 6-month repayment term. Not specified in design notes.</summary>
        public DateTime DueDate { get; set; }

        public LoanRepaymentStatus Status { get; set; } = LoanRepaymentStatus.Active;

        public DateTime? SettledOn { get; set; }

        public ICollection<LoanPayment> Payments { get; set; } = new List<LoanPayment>();

        [NotMapped]
        public decimal AmountPaid => TotalRepayable - OutstandingBalance;


        // ── NEW: collateral collection tracking ──
        public bool CollateralCollected { get; set; } = false;
        public DateTime? CollateralCollectedOn { get; set; }
        public int? CollectedByOwnerId { get; set; } // ASSUMPTION: tracks which shop owner confirmed it, for accountability

        // ── NEW: QR-based collection verification ──
        public string CollectionToken { get; set; } = Guid.NewGuid().ToString("N"); // unique per loan, generated at creation
    }
}