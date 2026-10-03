using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.SkytechWorkers;

namespace SkyTech.Controllers.Api
{
    public class ShiftChangeRequestBody
    {
        public int RosterShiftId { get; set; }
        public RequestType Type { get; set; }
        public string Reason { get; set; } = string.Empty;

        // Used only when Type == TimeChange
        public DateTime? RequestedWorkDate { get; set; }
        public TimeSpan? RequestedStartTime { get; set; }
        public TimeSpan? RequestedEndTime { get; set; }

        // Used only when Type == ShiftSwap
        public int? SwapWithStaffId { get; set; }
        public int? SwapWithRosterShiftId { get; set; }
    }

    [ApiController]
    [Route("api/shiftchange")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ShiftChangeApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ShiftChangeApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        private int GetStaffId() => int.Parse(User.FindFirstValue("UserId")!);

        // Colleagues in the same branch as the given shift — used to
        // populate the "swap with" picker on the mobile form.
        [HttpGet("colleagues")]
        public async Task<IActionResult> GetColleagues(int rosterShiftId)
        {
            var staffId = GetStaffId();

            var shift = await _db.RosterShifts
                .FirstOrDefaultAsync(rs => rs.RosterShiftId == rosterShiftId);

            if (shift == null)
                return NotFound(new { message = "Shift not found." });

            var colleagues = await _db.Staffs
                .Where(s => s.BranchId == shift.BranchId && s.StaffId != staffId)
                .Select(s => new
                {
                    staffId = s.StaffId,
                    fullName = s.FirstName + " " + s.LastName
                })
                .ToListAsync();

            return Ok(colleagues);
        }

        [HttpPost("request")]
        public async Task<IActionResult> RequestChange([FromBody] ShiftChangeRequestBody body)
        {
            var staffId = GetStaffId();

            if (string.IsNullOrWhiteSpace(body.Reason))
                return BadRequest(new { message = "Please provide a reason for the request." });

            if (body.Type == RequestType.ShiftSwap && body.SwapWithStaffId == null)
                return BadRequest(new { message = "Select who you'd like to swap with." });

            var shift = await _db.RosterShifts.FindAsync(body.RosterShiftId);
            if (shift == null)
                return NotFound(new { message = "Shift not found." });

            var request = new ShiftChangeRequest
            {
                RosterShiftId = body.RosterShiftId,
                StaffId = staffId,
                Type = body.Type,
                Reason = body.Reason,
                RequestedWorkDate = body.RequestedWorkDate,
                RequestedStartTime = body.RequestedStartTime,
                RequestedEndTime = body.RequestedEndTime,
                SwapWithStaffId = body.SwapWithStaffId,
                SwapWithRosterShiftId = body.SwapWithRosterShiftId
            };

            _db.ShiftChangeRequests.Add(request);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Your shift change request has been submitted." });
        }

        [HttpGet("my-requests")]
        public async Task<IActionResult> MyRequests()
        {
            var staffId = GetStaffId();

            var requests = await _db.ShiftChangeRequests
                .Include(r => r.RosterShift)
                .Include(r => r.SwapWithStaff)
                .Where(r => r.StaffId == staffId)
                .OrderByDescending(r => r.SubmittedAt)
                .Select(r => new
                {
                    requestId = r.ShiftChangeRequestId,
                    type = r.Type.ToString(),
                    status = r.Status.ToString(),
                    reason = r.Reason,
                    submittedAt = r.SubmittedAt,
                    shiftWorkDate = r.RosterShift.WorkDate,
                    shiftStart = r.RosterShift.StartTime.ToString(@"hh\:mm"),
                    shiftEnd = r.RosterShift.EndTime.ToString(@"hh\:mm"),
                    requestedWorkDate = r.RequestedWorkDate,
                    requestedStartTime = r.RequestedStartTime != null
                        ? r.RequestedStartTime.Value.ToString(@"hh\:mm")
                        : null,
                    requestedEndTime = r.RequestedEndTime != null
                        ? r.RequestedEndTime.Value.ToString(@"hh\:mm")
                        : null,
                    swapWithName = r.SwapWithStaff != null
                        ? r.SwapWithStaff.FirstName + " " + r.SwapWithStaff.LastName
                        : null,
                    reviewNote = r.ReviewNote
                })
                .ToListAsync();

            return Ok(requests);
        }
    }
}