using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models.SkytechWorkers
{
    /// <summary>
    /// A single item of collateral offered against a loan application.
    /// One LoanApplication has many CollateralItem rows (UC-03, step 6).
    /// </summary>
    public class CollateralItem
    {
        [Key]
        public int CollateralItemId { get; set; }

        [ForeignKey(nameof(LoanApplication))]
        public int LoanApplicationId { get; set; }
        [ValidateNever]
        public LoanApplication LoanApplication { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DeclaredValue { get; set; }

        /// <summary>Relative path (e.g. under wwwroot/uploads/collateral/) to the uploaded photo.</summary>
        [Required]
        [StringLength(500)]
        public string ImagePath { get; set; } = string.Empty;
        [ValidateNever]
        public AiScanResult AiScanResult { get; set; } = AiScanResult.Pending;

        /// <summary>The AI's reasoning/explanation for its verdict, shown to the user (step 7).</summary>
        public string? AiScanNotes { get; set; }

        /// <summary>Optional — NOT verified against a real retail price by the AI (no free model can reliably do that); staff verify manually against the serial number if present.</summary>
        [StringLength(100)]
        public string? SerialNumber { get; set; }

        [Required]
        public CollateralCondition Condition { get; set; }

        /// <summary>AI's confidence in its read of the photo/category match, 0-100. Null if the scan call failed. Below 60, AiScanResult is forced to NeedsManualReview.</summary>
        [Range(0, 100)]
        public int? AiConfidencePercent { get; set; }

        /// <summary>The accepted-collateral category the AI matched the item to (e.g. "Electronics"). Null if it couldn't confidently match one.</summary>
        [StringLength(100)]
        public string? AiMatchedCategory { get; set; }

        

        /// <summary>Perceptual hash (aHash) of the collateral photo, computed at upload time.
        /// Used only for near-duplicate detection across loans — not a security hash.</summary>
        [StringLength(64)]
        public string? ImagePerceptualHash { get; set; } //Lonake simdingela ukubheka ukuthi i collateral ayikho yini duplucated to prevent i fraud

        public DateTime UploadedOn { get; set; } = DateTime.UtcNow;
    }
}