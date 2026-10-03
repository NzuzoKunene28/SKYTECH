using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.SkytechWorkers;
using SkyTech.Services;

namespace SkyTech.Controllers.Api
{
    [ApiController]
    [Route("api/shifts")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ShiftsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ShiftsApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        private int GetStaffId() => int.Parse(User.FindFirstValue("UserId")!);

        // Shape used for "today" when there is no real RosterShift row
        // (approved leave, or a shift swapped away). Same field names as a
        // real shift so the Android ShiftResponse model handles both.
        private IActionResult SyntheticToday(
            DateTime today, string entryType, string start, string end, string branch, string? note)
        {
            return Ok(new
            {
                shiftId = -1,
                scheduledDate = today,
                scheduledStart = start,
                scheduledEnd = end,
                status = entryType,
                isCheckedIn = false,
                checkInTime = (DateTime?)null,
                checkOutTime = (DateTime?)null,
                branchName = branch,
                entryType,
                note
            });
        }

        [HttpGet("current")]
        public IActionResult GetCurrent()
        {
            var staffId = GetStaffId();
            var today = SaTime.Today;

            var rosterShift = _db.RosterShifts
                .Include(rs => rs.Branch)
                .FirstOrDefault(rs => rs.StaffId == staffId
                                    && rs.WorkDate.Date == today);

            if (rosterShift != null)
            {
                var attendance = _db.Attendances
                    .FirstOrDefault(a => a.RosterShiftId == rosterShift.RosterShiftId);

                return Ok(new
                {
                    shiftId = rosterShift.RosterShiftId,
                    scheduledDate = rosterShift.WorkDate,
                    scheduledStart = rosterShift.StartTime.ToString(@"hh\:mm"),
                    scheduledEnd = rosterShift.EndTime.ToString(@"hh\:mm"),
                    status = rosterShift.Status.ToString(),
                    isCheckedIn = attendance?.CheckInTime != null && attendance?.CheckOutTime == null,
                    checkInTime = attendance?.CheckInTime,
                    checkOutTime = attendance?.CheckOutTime,
                    branchName = rosterShift.Branch != null ? rosterShift.Branch.BranchName : "Unassigned",
                    entryType = "Shift",
                    note = (string?)null
                });
            }

            // No real shift row today — was it removed because of approved leave?
            var leave = _db.LeaveRequests
                .Include(l => l.LeaveType)
                .FirstOrDefault(l => l.StaffId == staffId
                                  && l.Status == LeaveRequestStatus.Approved
                                  && l.StartDate.Date <= today
                                  && l.EndDate.Date >= today);

            if (leave != null)
            {
                return SyntheticToday(
                    today, "OnLeave", "", "", "",
                    leave.LeaveType != null ? leave.LeaveType.LeaveTypeName : "Leave");
            }

            // ...or because the shift was swapped to a colleague?
            var swap = _db.ShiftChangeRequests
                .Include(r => r.RosterShift)
                    .ThenInclude(rs => rs.Branch)
                .Include(r => r.SwapWithStaff)
                .FirstOrDefault(r => r.StaffId == staffId
                                  && r.Status == ShiftChangeStatus.Approved
                                  && r.Type == RequestType.ShiftSwap
                                  && r.RosterShift.WorkDate.Date == today);

            if (swap != null)
            {
                var swapWithName = swap.SwapWithStaff != null
                    ? $"{swap.SwapWithStaff.FirstName} {swap.SwapWithStaff.LastName}".Trim()
                    : "a colleague";

                return SyntheticToday(
                    today, "ShiftSwapped",
                    swap.RosterShift.StartTime.ToString(@"hh\:mm"),
                    swap.RosterShift.EndTime.ToString(@"hh\:mm"),
                    swap.RosterShift.Branch != null ? swap.RosterShift.Branch.BranchName : "Unassigned",
                    $"Swapped with {swapWithName}");
            }

            return NotFound(new { message = "No shift scheduled for today." });
        }

        private class RosterEntry
        {
            public int ShiftId { get; set; } = -1;
            public DateTime WorkDate { get; set; }
            public string ScheduledStart { get; set; } = "";
            public string ScheduledEnd { get; set; } = "";
            public string BranchName { get; set; } = "";
            public string Status { get; set; } = "";
            public bool IsToday { get; set; }
            public string EntryType { get; set; } = "Shift"; // "Shift" | "OnLeave" | "ShiftSwapped"
            public string? Note { get; set; }
        }

        [HttpGet("roster")]
        public IActionResult GetRoster([FromQuery] DateTime? startDate)
        {
            var staffId = GetStaffId();
            var today = SaTime.Today;
            var referenceDate = (startDate ?? today).Date;

            int daysSinceMonday = ((int)referenceDate.DayOfWeek + 6) % 7;
            var startOfWeek = referenceDate.AddDays(-daysSinceMonday);
            var endOfWeek = startOfWeek.AddDays(6);

            var realShifts = _db.RosterShifts
                .Where(rs => rs.StaffId == staffId
                          && rs.WorkDate.Date >= startOfWeek
                          && rs.WorkDate.Date <= endOfWeek)
                .Select(rs => new RosterEntry
                {
                    ShiftId = rs.RosterShiftId,
                    WorkDate = rs.WorkDate,
                    ScheduledStart = rs.StartTime.ToString(@"hh\:mm"),
                    ScheduledEnd = rs.EndTime.ToString(@"hh\:mm"),
                    BranchName = rs.Branch != null ? rs.Branch.BranchName : "Unassigned",
                    Status = rs.Status.ToString(),
                    IsToday = rs.WorkDate.Date == today,
                    EntryType = "Shift",
                    Note = null
                })
                .ToList();

            var coveredDates = new HashSet<DateTime>(realShifts.Select(s => s.WorkDate.Date));
            var entries = new List<RosterEntry>(realShifts);

            // ── Approved leave covering days in this week with no real shift ──
            var approvedLeave = _db.LeaveRequests
                .Include(l => l.LeaveType)
                .Where(l => l.StaffId == staffId
                         && l.Status == LeaveRequestStatus.Approved
                         && l.StartDate.Date <= endOfWeek
                         && l.EndDate.Date >= startOfWeek)
                .ToList();

            foreach (var leave in approvedLeave)
            {
                var rangeStart = leave.StartDate.Date < startOfWeek ? startOfWeek : leave.StartDate.Date;
                var rangeEnd = leave.EndDate.Date > endOfWeek ? endOfWeek : leave.EndDate.Date;

                for (var day = rangeStart; day <= rangeEnd; day = day.AddDays(1))
                {
                    if (coveredDates.Contains(day))
                        continue;

                    entries.Add(new RosterEntry
                    {
                        WorkDate = day,
                        Status = "OnLeave",
                        IsToday = day == today,
                        EntryType = "OnLeave",
                        Note = leave.LeaveType != null ? leave.LeaveType.LeaveTypeName : "Leave"
                    });

                    coveredDates.Add(day);
                }
            }

            // ── Approved shift swaps where this staff gave away their shift ──
            var approvedSwaps = _db.ShiftChangeRequests
                .Include(r => r.RosterShift)
                    .ThenInclude(rs => rs.Branch)
                .Include(r => r.SwapWithStaff)
                .Where(r => r.StaffId == staffId
                         && r.Status == ShiftChangeStatus.Approved
                         && r.Type == RequestType.ShiftSwap
                         && r.RosterShift.WorkDate.Date >= startOfWeek
                         && r.RosterShift.WorkDate.Date <= endOfWeek)
                .ToList();

            foreach (var swap in approvedSwaps)
            {
                var day = swap.RosterShift.WorkDate.Date;

                if (coveredDates.Contains(day))
                    continue;

                var swapWithName = swap.SwapWithStaff != null
                    ? $"{swap.SwapWithStaff.FirstName} {swap.SwapWithStaff.LastName}".Trim()
                    : "a colleague";

                entries.Add(new RosterEntry
                {
                    WorkDate = day,
                    ScheduledStart = swap.RosterShift.StartTime.ToString(@"hh\:mm"),
                    ScheduledEnd = swap.RosterShift.EndTime.ToString(@"hh\:mm"),
                    BranchName = swap.RosterShift.Branch != null ? swap.RosterShift.Branch.BranchName : "Unassigned",
                    Status = "ShiftSwapped",
                    IsToday = day == today,
                    EntryType = "ShiftSwapped",
                    Note = $"Swapped with {swapWithName}"
                });

                coveredDates.Add(day);
            }

            var ordered = entries
                .OrderBy(e => e.WorkDate)
                .Select(e => new
                {
                    shiftId = e.ShiftId,
                    workDate = e.WorkDate,
                    scheduledStart = e.ScheduledStart,
                    scheduledEnd = e.ScheduledEnd,
                    branchName = e.BranchName,
                    status = e.Status,
                    isToday = e.IsToday,
                    entryType = e.EntryType,
                    note = e.Note
                })
                .ToList();

            return Ok(ordered);
        }
    }
}