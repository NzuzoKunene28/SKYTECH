using System.ComponentModel.DataAnnotations;

namespace SkyTech.Models.SkytechWorkers
{
    /// <summary>
    /// Self-seeded, mocked stand-in for a real criminal-record clearance
    /// lookup. Keyed by NationalId for the same reason as CreditRecord.
    /// </summary>
    public class CriminalRecord
    {
        [Key]
        [StringLength(20)]
        public string NationalId { get; set; } = string.Empty;

        public ClearanceStatus ClearanceStatus { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
