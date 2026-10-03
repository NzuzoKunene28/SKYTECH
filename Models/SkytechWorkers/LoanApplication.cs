using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models.SkytechWorkers
{
    /// <summary>
    /// A customer's loan application (UC-03). Eligibility fields are
    /// snapshotted at check time rather than live-joined to CreditRecord/
    /// CriminalRecord, so a later change to the mocked lookup tables does
    /// not silently rewrite the history of a past decision.
    /// </summary>
    public class LoanApplication
    {
        [Key]
        public int LoanApplicationId { get; set; }

        /// <summary>
        /// ASSUMPTION: Customer is an existing entity in this project
        /// (SkyTech.Models.SkytechWorkers.Customer) tied to the logged-in
        /// account. It is not redefined here.
        /// </summary>
        [ForeignKey(nameof(Customer))]
        public int CustomerId { get; set; }
        [ValidateNever]
        public Customer Customer { get; set; } = null!;

        [ForeignKey(nameof(Branch))]
        public int BranchId { get; set; }
        [ValidateNever]
        public Branch Branch { get; set; } = null;

        [Required]
        [StringLength(20)]
        public string NationalId { get; set; } = string.Empty;

        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RequestedAmount { get; set; }

        /// <summary>
        /// ASSUMPTION: stored as a single MONTHLY gross salary figure.
        /// The design notes left "monthly vs. annual" undecided; monthly
        /// was chosen because South African payslips/affordability checks
        /// conventionally quote monthly gross income, and it composes
        /// cleanly with a multiplier-based eligibility rule. If annual
        /// figures are collected from the user, convert on input
        /// (annual / 12) rather than storing both.
        /// </summary>
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlySalary { get; set; }

        /// <summary>
        /// ASSUMPTION: for this phase, salary is informational / for staff
        /// review only and does NOT factor numerically into the automated
        /// EligibilityResult. The design notes flagged this as undecided;
        /// wiring salary into the eligibility formula was left out rather
        /// than silently guessed at, since it changes approval outcomes.
        /// See ComputeEligibility() in LoanApplicationController for where
        /// to add a salary-based rule once the formula is confirmed.
        /// </summary>
        [Required]
        [StringLength(500)]
        public string ProofOfIncomeDocPath { get; set; } = string.Empty;

        public LoanPurpose? Purpose { get; set; }

        /// <summary>
        /// Self-declared on the application form. Combined with credit score
        /// and criminal clearance as a hard pass/fail gate — see
        /// LoanApplicationController.RunEligibilityCheckAsync.
        /// </summary>
        [Required]
        [Display(Name = "Are you currently employed?")]
        public bool IsEmployed { get; set; }

        public int CreditScoreAtApplication { get; set; }

        public ClearanceStatus ClearanceStatusAtApplication { get; set; }

        public EligibilityResult EligibilityResult { get; set; } = EligibilityResult.Pending;

        public LoanApplicationStatus Status { get; set; } = LoanApplicationStatus.Started;

        public DateTime? SubmittedOn { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public ICollection<CollateralItem> CollateralItems { get; set; } = new List<CollateralItem>();

        [NotMapped]
        public decimal TotalDeclaredCollateralValue => CollateralItems?.Sum(c => c.DeclaredValue) ?? 0m;
    }
}