using System;
using SkyTech.Models.AttendanceMods;

namespace SkyTech.ViewModels
{
    public class AttendanceRowViewModel
    {
        public DateTime Date { get; set; }
        public TimeSpan ScheduledStart { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public double? HoursWorked { get; set; }
        public AttendanceStatus Status { get; set; }
    }
}