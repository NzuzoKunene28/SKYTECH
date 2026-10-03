using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SkyTech.Models.SkytechWorkers
{
    public enum RequestType { ShiftSwap, TimeChange}
    public enum ShiftChangeStatus { Pending, Approved, Rejected }

    public class ShiftChangeRequest
    {
        [Key]
        public int ShiftChangeRequestId { get; set; }

        [Required]
        public RequestType Type { get; set; }

        [ForeignKey(nameof(RosterShift))]
        public int RosterShiftId { get; set; }
        [ValidateNever]
        public virtual RosterShift RosterShift { get; set; }

        [ForeignKey(nameof(RequestedBy))]
        public int StaffId { get; set; }
        [ValidateNever]
        public virtual Staff RequestedBy { get; set; }

        [Required, MaxLength(500)]
        public string Reason { get; set; }

        // ---- Used only when Type == TimeChange ----
        public DateTime? RequestedWorkDate { get; set; }
        public TimeSpan? RequestedStartTime { get; set; }
        public TimeSpan? RequestedEndTime { get; set; }

        // ---- Used only when Type == ShiftSwap ----
        [ForeignKey(nameof(SwapWithStaff))]
        public int? SwapWithStaffId { get; set; }
        [ValidateNever]
        public virtual Staff SwapWithStaff { get; set; }

        // Optional: the specific shift of the colleague being swapped with.
        // If null, HR is just reassigning this shift to SwapWithStaff.
        public int? SwapWithRosterShiftId { get; set; }
        [ValidateNever]
        [ForeignKey(nameof(SwapWithRosterShiftId))]
        public virtual RosterShift SwapWithRosterShift { get; set; }

        public ShiftChangeStatus Status { get; set; } = ShiftChangeStatus.Pending;

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(ReviewedBy))]
        public int? ReviewedByHRId { get; set; }
        [ValidateNever]
        public virtual HR ReviewedBy { get; set; }

        [MaxLength(500)]
        public string? ReviewNote { get; set; }

        public DateTime? ReviewedAt { get; set; }
    }
}