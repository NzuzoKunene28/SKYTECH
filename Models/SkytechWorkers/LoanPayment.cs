using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models.SkytechWorkers
{
    /// <summary>
    /// A single repayment against a Loan (UC-05, step 4). BalanceAfterPayment
    /// is snapshotted rather than recomputed later, so the balance-over-time
    /// graph doesn't shift if InterestRate or TotalRepayable is ever edited
    /// retroactively.
    /// </summary>
    public class LoanPayment
    {
        [Key]
        public int LoanPaymentId { get; set; }

        [ForeignKey(nameof(Loan))]
        public int LoanId { get; set; }
        [ValidateNever]
        public Loan Loan { get; set; } = null!;

        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public PaymentMethod Method { get; set; }

        public DateTime PaidOn { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? TransactionReference { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceAfterPayment { get; set; }
    }
}