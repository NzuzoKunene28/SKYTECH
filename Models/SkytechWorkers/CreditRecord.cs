using System.ComponentModel.DataAnnotations;

namespace SkyTech.Models.SkytechWorkers
{
    /// <summary>
    /// Self-seeded, mocked stand-in for a real credit bureau lookup.
    /// Keyed by NationalId rather than a surrogate int PK, since it is
    /// looked up directly (never navigated to via FK) and NationalId is
    /// already unique per person.
    /// </summary>
    public class CreditRecord
    {
        [Key]
        [StringLength(20)]
        public string NationalId { get; set; } = string.Empty;

        /// <summary>ASSUMPTION: score on a 0-999 scale (common SA credit bureau range).</summary>
        [Range(0, 999)]
        public int CreditScore { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
