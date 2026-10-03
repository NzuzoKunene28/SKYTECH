using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkyTech.Models.SkytechWorkers;
using FaceONNX;

namespace SkyTech.Models.AttendanceMods
{
    // Points at the roster team's real RosterShift/Staff tables now that they
    // exist — replaces the earlier generic (UserId, UserType) design, which
    // was only ever a stand-in for this. Deliberately does NOT touch
    // RosterShift.Status; that belongs to the roster team's own scheduling
    // semantics, not to "is this person currently checked in."
    public class Attendance
    {
        [Key]
        public int AttendanceId { get; set; }

        [Required, ForeignKey(nameof(RosterShift))]
        public int RosterShiftId { get; set; }
        public virtual RosterShift RosterShift { get; set; }

        // Denormalized so an attendance row is self-contained even if the
        // roster shift is later edited/reassigned.
        [Required]
        public int StaffId { get; set; }

        public DateTime? CheckInTime { get; set; }
        public double? CheckInLat { get; set; }
        public double? CheckInLng { get; set; }
        public string CheckInPhotoPath { get; set; }

        public DateTime? CheckOutTime { get; set; }
        public double? CheckOutLat { get; set; }
        public double? CheckOutLng { get; set; }
        public string? CheckOutPhotoPath { get; set; }

        // Bound at check-in, re-validated at check-out.
        [StringLength(200)]
        public string DeviceId { get; set; }

         
    }
}