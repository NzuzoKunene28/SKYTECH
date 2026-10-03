using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SkyTech.Models.SkytechWorkers
{
    public enum RosterStatus { Draft, Published }

    public class Roster
    {
        [Key]
        public int RosterId { get; set; }

        [ForeignKey(nameof(Branch))]
        public int BranchId { get; set; }
        [ValidateNever]
        public virtual Branch Branch { get; set; }

        [Required]
        public DateTime WeekStartDate { get; set; } // Monday

        [Required]
        public DateTime WeekEndDate { get; set; }   // Sunday

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public RosterStatus Status { get; set; } = RosterStatus.Draft;

        [ValidateNever]
        public virtual ICollection<RosterShift> RosterShifts { get; set; }
    }
}