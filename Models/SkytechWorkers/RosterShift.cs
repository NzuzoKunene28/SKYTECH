using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SkyTech.Models.SkytechWorkers
{
    public enum RosterShiftStatus { Scheduled, Completed, Cancelled }

    public class RosterShift
    {
        [Key]
        public int RosterShiftId { get; set; }

        [ForeignKey(nameof(Roster))]
        public int RosterId { get; set; }
        [ValidateNever]
        public virtual Roster Roster { get; set; }

        [ForeignKey(nameof(Staff))]
        public int StaffId { get; set; }
        [ValidateNever]
        public virtual Staff Staff { get; set; }

        [Required]
        public DateTime WorkDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [ForeignKey(nameof(Position))]
        public int PositionId { get; set; }
        [ValidateNever]
        public virtual StaffPosition Position { get; set; }

        [ForeignKey(nameof(Branch))]
        public int BranchId { get; set; }
        [ValidateNever]
        public virtual Branch Branch { get; set; }

        public RosterShiftStatus Status { get; set; } = RosterShiftStatus.Scheduled;
    }
}