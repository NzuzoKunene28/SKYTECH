using System;
using SkyTech.Models.SkytechWorkers;

namespace SkyTech.Models.AttendanceMods
{
    public enum AttendanceStatus { OnTime, Late, Absent, Pending }

    // Status is computed on read, never stored.
    public static class AttendanceStatusCalculator
    {
        private static readonly TimeSpan GracePeriod = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan AbsentCutoff = TimeSpan.FromHours(1);

        public static AttendanceStatus GetStatus(RosterShift shift, Attendance attendance)
        {
            var scheduledStart = shift.WorkDate.Date + shift.StartTime;
            var graceEnd = scheduledStart.Add(GracePeriod);
            var absentCutoff = scheduledStart.Add(AbsentCutoff);

            if (attendance?.CheckInTime != null)
            {
                return attendance.CheckInTime.Value <= graceEnd
                    ? AttendanceStatus.OnTime
                    : AttendanceStatus.Late;
            }

            return DateTime.Now > absentCutoff
                ? AttendanceStatus.Absent
                : AttendanceStatus.Pending;
        }
    }
}