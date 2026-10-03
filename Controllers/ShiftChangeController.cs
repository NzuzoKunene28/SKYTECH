using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.SkytechWorkers;

namespace SkyTech.Controllers
{
    public class ShiftChangeController : Controller
    {
        private readonly ApplicationDbContext db;

        public ShiftChangeController(ApplicationDbContext db)
        {
            this.db = db;
        }

         // TODO: replace with your actual logged-in-staff lookup (session/claims/etc.)
        private int GetCurrentStaffId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        // ===== EMPLOYEE SIDE =====

        // GET: employee opens the form for a specific shift
        public async Task<IActionResult> RequestChange(int rosterShiftId)
        {
            var shift = await db.RosterShifts
                .Include(s => s.Position)
                .Include(s => s.Branch)
                .FirstOrDefaultAsync(s => s.RosterShiftId == rosterShiftId);

            if (shift == null) return NotFound();

            ViewBag.Colleagues = new SelectList(
                await db.Staffs
                    .Where(s => s.BranchId == shift.BranchId && s.StaffId != shift.StaffId)
                    .ToListAsync(),
                "StaffId", "FirstName");

            return View(shift);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestChange(
            int rosterShiftId,
            RequestType type,
            string reason,
            DateTime? requestedWorkDate,
            TimeSpan? requestedStartTime,
            TimeSpan? requestedEndTime,
            int? swapWithStaffId,
            int? swapWithRosterShiftId)
        {
            var staffId = GetCurrentStaffId();
            if (staffId == 0) return Unauthorized();

            bool missingReason = string.IsNullOrWhiteSpace(reason);
            bool missingSwapTarget = type == RequestType.ShiftSwap && swapWithStaffId == null;

            if (missingReason || missingSwapTarget)
            {
                ModelState.AddModelError("", missingSwapTarget
                    ? "Select who you'd like to swap with."
                    : "Please provide a reason for the request.");

                var shift = await db.RosterShifts
                    .Include(s => s.Position).Include(s => s.Branch)
                    .FirstOrDefaultAsync(s => s.RosterShiftId == rosterShiftId);

                ViewBag.Colleagues = new SelectList(
                    await db.Staffs.Where(s => s.BranchId == shift.BranchId).ToListAsync(),
                    "StaffId", "FirstName");

                return View(shift);
            }

            var request = new ShiftChangeRequest
            {
                RosterShiftId = rosterShiftId,
                StaffId = staffId,
                Type = type,
                Reason = reason,
                RequestedWorkDate = requestedWorkDate,
                RequestedStartTime = requestedStartTime,
                RequestedEndTime = requestedEndTime,
                SwapWithStaffId = swapWithStaffId,
                SwapWithRosterShiftId = swapWithRosterShiftId
            };

            db.ShiftChangeRequests.Add(request);
            await db.SaveChangesAsync();

            TempData["successful"] = "Your shift change request has been submitted.";
            return RedirectToAction("MyRequests");
        }

        // GET: employee views their own request history
        public async Task<IActionResult> MyRequests()
        {
            var staffId = GetCurrentStaffId();
            var requests = await db.ShiftChangeRequests
                .Include(r => r.RosterShift).ThenInclude(s => s.Position)
                .Where(r => r.StaffId == staffId)
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();

            return View(requests);
        }

        // ===== HR SIDE =====

        // GET: HR reviews all pending requests
        public async Task<IActionResult> PendingRequests()
        {
            var requests = await db.ShiftChangeRequests
                .Include(r => r.RosterShift).ThenInclude(s => s.Position)
                .Include(r => r.RosterShift).ThenInclude(s => s.Branch)
                .Include(r => r.RequestedBy)
                .Include(r => r.SwapWithStaff)
                .Where(r => r.Status == ShiftChangeStatus.Pending)
                .OrderBy(r => r.SubmittedAt)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string reviewNote)
        {
            var request = await db.ShiftChangeRequests
                .Include(r => r.RosterShift)
                .Include(r => r.SwapWithRosterShift)
                .FirstOrDefaultAsync(r => r.ShiftChangeRequestId == id);

            if (request == null) return NotFound();

            switch (request.Type)
            {
                case RequestType.TimeChange:
                    if (request.RequestedWorkDate.HasValue)
                        request.RosterShift.WorkDate = request.RequestedWorkDate.Value;
                    if (request.RequestedStartTime.HasValue)
                        request.RosterShift.StartTime = request.RequestedStartTime.Value;
                    if (request.RequestedEndTime.HasValue)
                        request.RosterShift.EndTime = request.RequestedEndTime.Value;
                    break;

                case RequestType.ShiftSwap:
                    if (request.SwapWithRosterShiftId.HasValue && request.SwapWithRosterShift != null)
                    {
                        var originalStaffId = request.RosterShift.StaffId;
                        request.RosterShift.StaffId = request.SwapWithRosterShift.StaffId;
                        request.SwapWithRosterShift.StaffId = originalStaffId;
                    }
                    else if (request.SwapWithStaffId.HasValue)
                    {
                        request.RosterShift.StaffId = request.SwapWithStaffId.Value;
                    }
                    break;

                //case RequestType.DayOff:
                //case RequestType.Cancellation:
                    request.RosterShift.Status = RosterShiftStatus.Cancelled;
                    break;
            }

            request.Status = ShiftChangeStatus.Approved;
            request.ReviewedByHRId = GetCurrentStaffId();
            request.ReviewNote = reviewNote;
            request.ReviewedAt = DateTime.Now;

            await db.SaveChangesAsync();

            TempData["successful"] = "Shift change approved and applied to the roster.";
            return RedirectToAction("PendingRequests");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reviewNote)
        {
            var request = await db.ShiftChangeRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.Status = ShiftChangeStatus.Rejected;
            request.ReviewedByHRId = GetCurrentStaffId();
            request.ReviewNote = reviewNote;
            request.ReviewedAt = DateTime.Now;

            await db.SaveChangesAsync();

            TempData["successful"] = "Shift change request rejected.";
            return RedirectToAction("PendingRequests");
        }
    }
}