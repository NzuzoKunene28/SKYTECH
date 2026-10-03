using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.SkytechWorkers;
using SkyTech.Services;
using SkyTech.ViewModels;

namespace SkyTech.Controllers
{
    [AuthorizeUser]
    public class LeaveAssessmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPushNotificationService _push;

        public LeaveAssessmentController(
            ApplicationDbContext context,
            IPushNotificationService push)
        {
            _context = context;
            _push = push;
        }

        // =========================================================
        // GET CURRENT HR ID
        // =========================================================

        private int? GetCurrentHrId()
        {
            var userType =
                HttpContext.Session.GetString("UserType");

            if (userType != "HR")
                return null;

            return HttpContext.Session.GetInt32("UserId");
        }

        // =========================================================
        // DISPLAY PENDING LEAVE REQUESTS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> PendingRequests()
        {
            var currentHrId = GetCurrentHrId();

            if (currentHrId == null)
            {
                return RedirectToAction(
                    "LogIn",
                    "Authentication");
            }

            var requests = await _context.LeaveRequests
                .Where(request =>
                    request.Status ==
                    LeaveRequestStatus.Pending)
                .Include(request => request.Staff)
                    .ThenInclude(staff => staff.Positions)
                        .ThenInclude(position =>
                            position.Departments)
                .Include(request => request.LeaveType)
                .OrderBy(request => request.StartDate)
                .ThenBy(request => request.SubmittedAt)
                .ToListAsync();

            var model = requests.Select(request =>
                new AssessLeaveViewModel
                {
                    LeaveRequestId =
                        request.LeaveRequestId,

                    StaffName =
                        request.Staff == null
                            ? "Unknown employee"
                            : request.Staff.FirstName + " " +
                              request.Staff.LastName,

                    DepartmentName =
                        request.Staff?.Positions?.Departments
                            ?.DepartmentName
                        ?? "Not assigned",

                    PositionName =
                        request.Staff?.Positions?.PositionName
                        ?? "Not assigned",

                    LeaveTypeName =
                        request.LeaveType?.LeaveTypeName
                        ?? "Unknown leave type",

                    StartDate = request.StartDate,
                    EndDate = request.EndDate,

                    WorkingDaysRequested =
                        request.WorkingDaysRequested,

                    SubmittedAt = request.SubmittedAt,
                    Status = request.Status
                })
                .ToList();

            return View(model);
        }

        // =========================================================
        // DISPLAY ONE REQUEST FOR ASSESSMENT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Assess(int id)
        {
            var currentHrId = GetCurrentHrId();

            if (currentHrId == null)
            {
                return RedirectToAction(
                    "LogIn",
                    "Authentication");
            }

            var request = await _context.LeaveRequests
                .Include(item => item.Staff)
                    .ThenInclude(staff => staff.Positions)
                        .ThenInclude(position =>
                            position.Departments)
                .Include(item => item.LeaveType)
                .FirstOrDefaultAsync(item =>
                    item.LeaveRequestId == id);

            if (request == null)
                return NotFound();

            var model = await BuildAssessmentModelAsync(request);

            return View(model);
        }

        // =========================================================
        // BUILD THE FULL ASSESSMENT PICTURE
        // =========================================================

        private async Task<AssessLeaveViewModel> BuildAssessmentModelAsync(
            LeaveRequest request)
        {
            var model = new AssessLeaveViewModel
            {
                LeaveRequestId = request.LeaveRequestId,

                StaffName =
                    request.Staff == null
                        ? "Unknown employee"
                        : (request.Staff.FirstName + " " +
                           request.Staff.LastName).Trim(),

                DepartmentName =
                    request.Staff?.Positions?.Departments
                        ?.DepartmentName
                    ?? "Not assigned",

                PositionName =
                    request.Staff?.Positions?.PositionName
                    ?? "Not assigned",

                LeaveTypeName =
                    request.LeaveType?.LeaveTypeName
                    ?? "Unknown leave type",

                StartDate = request.StartDate,
                EndDate = request.EndDate,
                WorkingDaysRequested = request.WorkingDaysRequested,
                SubmittedAt = request.SubmittedAt,
                Status = request.Status,

                Reason = request.Reason,
                StaffEmail = request.Staff?.Email ?? "",
                StaffPhone = request.Staff?.PhoneNumber ?? "",
                EmployedSince = request.Staff?.DateEmployed,

                DocumentRequired =
                    request.LeaveType?.RequiresDocument ?? false,

                ReviewNote = request.ReviewNote,
                ReviewedAt = request.ReviewedAt
            };

            // ── Supporting document ──────────────────────────────
            if (!string.IsNullOrWhiteSpace(request.SupportingDocumentPath))
            {
                var path = request.SupportingDocumentPath
                    .Trim()
                    .Replace('\\', '/');

                model.SupportingDocumentUrl =
                    path.StartsWith("/") ||
                    path.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? path
                        : "/" + path;

                model.SupportingDocumentName = Path.GetFileName(path);
            }

            // ── Leave balance ────────────────────────────────────
            DateTime from = request.StartDate.Date;

            model.RequiresBalance =
                request.LeaveType != null &&
                request.LeaveType.RequiresBalance;

            if (model.RequiresBalance)
            {
                var balance = await _context.StaffLeaveBalances
                    .FirstOrDefaultAsync(item =>
                        item.StaffId == request.StaffId &&
                        item.LeaveTypeId == request.LeaveTypeId &&
                        from >= item.CycleStartDate.Date &&
                        from <= item.CycleEndDate.Date);

                if (balance != null)
                {
                    int otherPendingDays =
                        await _context.LeaveRequests
                            .Where(item =>
                                item.StaffId == request.StaffId &&
                                item.LeaveTypeId == request.LeaveTypeId &&
                                item.LeaveRequestId != request.LeaveRequestId &&
                                item.Status == LeaveRequestStatus.Pending &&
                                item.StartDate.Date >= balance.CycleStartDate.Date &&
                                item.StartDate.Date <= balance.CycleEndDate.Date)
                            .SumAsync(item =>
                                (int?)item.WorkingDaysRequested)
                            ?? 0;

                    model.HasBalance = true;
                    model.BalanceAllocated = balance.TotalAllocatedDays;
                    model.BalanceUsed = balance.UsedDays;
                    model.BalanceOtherPending = otherPendingDays;
                    model.BalanceAvailable =
                        balance.TotalAllocatedDays -
                        balance.UsedDays -
                        otherPendingDays;
                    model.BalanceAfterApproval =
                        model.BalanceAvailable -
                        request.WorkingDaysRequested;
                }
            }

            // ── Staffing impact ──────────────────────────────────
            await PopulateStaffingAsync(request, model);

            return model;
        }

        // =========================================================
        // STAFFING CHECK (shared by the Assess page and Approve)
        // =========================================================
        //
        // For every day of the requested leave, works out how many staff
        // in the employee's department would still be in if this request
        // is approved, and compares that with the department's minimum.

        private async Task PopulateStaffingAsync(
            LeaveRequest request,
            AssessLeaveViewModel model)
        {
            var department = request.Staff?.Positions?.Departments;
            string departmentName = department?.DepartmentName ?? "";
            int minimumRequired = department?.MinimumStaffRequired ?? 0;

            model.MinimumStaffRequired = minimumRequired;
            model.MinimumConfigured = minimumRequired > 0;

            if (string.IsNullOrWhiteSpace(departmentName))
            {
                model.StaffingVerdict = "Unknown";
                model.StaffingMessage =
                    "This employee has no department assigned, so the " +
                    "staffing impact can't be checked.";
                return;
            }

            DateTime from = request.StartDate.Date;
            DateTime to = request.EndDate.Date;
            int requesterId = request.StaffId;
            int requestId = request.LeaveRequestId;

            // Everyone in the department who is still employed
            // (suspended and terminated staff don't count).
            int activeStaff = await _context.Staffs
                .CountAsync(staff =>
                    staff.Positions.Departments.DepartmentName == departmentName &&
                    staff.StaffAvailability != IsTheStaffMemberAvailable.Suspended &&
                    staff.StaffAvailability != IsTheStaffMemberAvailable.Terminated);

            // Other people in the department with APPROVED leave
            // overlapping these dates.
            var overlapping = await _context.LeaveRequests
                .Include(item => item.Staff)
                .Include(item => item.LeaveType)
                .Where(item =>
                    item.LeaveRequestId != requestId &&
                    item.StaffId != requesterId &&
                    item.Status == LeaveRequestStatus.Approved &&
                    item.StartDate.Date <= to &&
                    item.EndDate.Date >= from &&
                    item.Staff.Positions.Departments.DepartmentName == departmentName)
                .OrderBy(item => item.StartDate)
                .ToListAsync();

            model.OverlappingPendingCount = await _context.LeaveRequests
                .CountAsync(item =>
                    item.LeaveRequestId != requestId &&
                    item.StaffId != requesterId &&
                    item.Status == LeaveRequestStatus.Pending &&
                    item.StartDate.Date <= to &&
                    item.EndDate.Date >= from &&
                    item.Staff.Positions.Departments.DepartmentName == departmentName);

            var days = new List<StaffingDayViewModel>();
            int peak = 0;
            int guard = 0;

            for (DateTime day = from; day <= to && guard < 120; day = day.AddDays(1), guard++)
            {
                var others = overlapping
                    .Where(item =>
                        item.StartDate.Date <= day &&
                        item.EndDate.Date >= day)
                    .ToList();

                int otherCount = others
                    .Select(item => item.StaffId)
                    .Distinct()
                    .Count();

                int remaining = activeStaff - (otherCount + 1);

                days.Add(new StaffingDayViewModel
                {
                    Date = day,
                    OtherOnLeaveCount = otherCount,
                    OtherOnLeaveNames = others
                        .Select(item => item.Staff != null
                            ? (item.Staff.FirstName + " " + item.Staff.LastName).Trim()
                            : "Unknown")
                        .Distinct()
                        .ToList(),
                    RemainingStaff = remaining,
                    IsUnderstaffed = minimumRequired > 0 && remaining < minimumRequired
                });

                peak = Math.Max(peak, otherCount);
            }

            model.ActiveStaff = activeStaff;
            model.AlreadyApprovedPeak = peak;
            model.MaximumAllowedOnLeave = minimumRequired > 0
                ? Math.Max(0, activeStaff - minimumRequired)
                : activeStaff;
            model.MoreCanBeApproved =
                Math.Max(0, model.MaximumAllowedOnLeave - peak);

            model.StaffingDays = days;
            model.UnderstaffedDayCount = days.Count(d => d.IsUnderstaffed);

            model.OverlappingApprovedLeaves = overlapping
                .Select(item => new OverlappingLeaveViewModel
                {
                    StaffName = item.Staff != null
                        ? (item.Staff.FirstName + " " + item.Staff.LastName).Trim()
                        : "Unknown",
                    LeaveTypeName = item.LeaveType?.LeaveTypeName ?? "Leave",
                    StartDate = item.StartDate,
                    EndDate = item.EndDate
                })
                .ToList();

            if (!model.MinimumConfigured)
            {
                model.StaffingVerdict = "Unknown";
                model.StaffingMessage =
                    "No minimum staffing level is set for the " +
                    departmentName +
                    " department, so the staffing impact can't be flagged.";
            }
            else if (model.UnderstaffedDayCount > 0)
            {
                var first = days.First(d => d.IsUnderstaffed);

                model.StaffingVerdict = "Blocked";
                model.StaffingMessage =
                    "Approving this request would leave only " +
                    first.RemainingStaff + " " + departmentName +
                    " staff on " + first.Date.ToString("ddd, dd MMM yyyy") +
                    " — the minimum required is " + minimumRequired + "." +
                    (model.UnderstaffedDayCount > 1
                        ? " " + model.UnderstaffedDayCount +
                          " days in this period are affected."
                        : "");
            }
            else
            {
                int minRemaining = days.Count > 0
                    ? days.Min(d => d.RemainingStaff)
                    : activeStaff;

                model.StaffingVerdict = "Safe";
                model.StaffingMessage =
                    "Safe to approve. At least " + minRemaining + " " +
                    departmentName +
                    " staff remain on every day of this leave (minimum required: " +
                    minimumRequired + ").";
            }
        }

        // =========================================================
        // APPROVE LEAVE REQUEST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(
            int leaveRequestId,
            string reviewNote)
        {
            var currentHrId = GetCurrentHrId();

            if (currentHrId == null)
            {
                return RedirectToAction(
                    "LogIn",
                    "Authentication");
            }

            var request = await _context.LeaveRequests
                .Include(item => item.Staff)
                    .ThenInclude(staff => staff.Positions)
                        .ThenInclude(position =>
                            position.Departments)
                .Include(item => item.LeaveType)
                .FirstOrDefaultAsync(item =>
                    item.LeaveRequestId == leaveRequestId);

            if (request == null)
                return NotFound();

            if (request.Status != LeaveRequestStatus.Pending)
            {
                TempData["ErrorMessage"] =
                    "Only pending leave requests can be approved.";

                return RedirectToAction(
                    nameof(Assess),
                    new { id = leaveRequestId });
            }

            if (request.StartDate.Date >
                request.EndDate.Date)
            {
                TempData["ErrorMessage"] =
                    "The leave request contains invalid dates.";

                return RedirectToAction(
                    nameof(Assess),
                    new { id = leaveRequestId });
            }

            if (request.WorkingDaysRequested <= 0)
            {
                TempData["ErrorMessage"] =
                    "The requested number of working days is invalid.";

                return RedirectToAction(
                    nameof(Assess),
                    new { id = leaveRequestId });
            }

            /*
			 * Find the employee's active leave balance.
			 */
            var balance = await _context.StaffLeaveBalances
                .Include(item => item.LeaveType)
                .FirstOrDefaultAsync(item =>
                    item.StaffId == request.StaffId &&
                    item.LeaveTypeId == request.LeaveTypeId &&
                    request.StartDate.Date >=
                        item.CycleStartDate.Date &&
                    request.StartDate.Date <=
                        item.CycleEndDate.Date);

            /*
			 * Check the balance only when the selected
			 * leave type requires a balance.
			 */
            if (request.LeaveType != null &&
                request.LeaveType.RequiresBalance)
            {
                if (balance == null)
                {
                    TempData["ErrorMessage"] =
                        "No active leave balance was found " +
                        "for this employee and leave type.";

                    return RedirectToAction(
                        nameof(Assess),
                        new { id = leaveRequestId });
                }

                int otherPendingDays =
                    await _context.LeaveRequests
                        .Where(item =>
                            item.StaffId == request.StaffId &&
                            item.LeaveTypeId ==
                                request.LeaveTypeId &&
                            item.LeaveRequestId !=
                                request.LeaveRequestId &&
                            item.Status ==
                                LeaveRequestStatus.Pending &&
                            item.StartDate.Date >=
                                balance.CycleStartDate.Date &&
                            item.StartDate.Date <=
                                balance.CycleEndDate.Date)
                        .SumAsync(item =>
                            (int?)item.WorkingDaysRequested)
                        ?? 0;

                int availableDays =
                    balance.TotalAllocatedDays -
                    balance.UsedDays -
                    otherPendingDays;

                if (request.WorkingDaysRequested >
                    availableDays)
                {
                    TempData["ErrorMessage"] =
                        "The employee only has " +
                        availableDays +
                        " available day(s), but requested " +
                        request.WorkingDaysRequested +
                        " day(s).";

                    return RedirectToAction(
                        nameof(Assess),
                        new { id = leaveRequestId });
                }
            }

            /*
			 * Department staffing check — the same calculation the
			 * Assess page shows, so the page and this button agree.
			 */
            var staffingCheck = new AssessLeaveViewModel();
            await PopulateStaffingAsync(request, staffingCheck);

            if (staffingCheck.StaffingVerdict == "Blocked")
            {
                TempData["ErrorMessage"] =
                    "Leave cannot be approved. " +
                    staffingCheck.StaffingMessage;

                return RedirectToAction(
                    nameof(Assess),
                    new { id = leaveRequestId });
            }

            /*
			 * Get every roster shift assigned to the employee
			 * during the requested leave period.
			 */
            var shiftsDuringLeave =
                await _context.RosterShifts
                    .Where(shift =>
                        shift.StaffId == request.StaffId &&
                        shift.WorkDate.Date >=
                            request.StartDate.Date &&
                        shift.WorkDate.Date <=
                            request.EndDate.Date)
                    .ToListAsync();

            try
            {
                /*
				 * Deduct the approved leave days.
				 */
                if (request.LeaveType != null &&
                    request.LeaveType.RequiresBalance &&
                    balance != null)
                {
                    balance.UsedDays +=
                        request.WorkingDaysRequested;
                }

                /*
				 * Update the leave request.
				 */
                request.Status =
                    LeaveRequestStatus.Approved;

                request.ReviewNote =
                    string.IsNullOrWhiteSpace(reviewNote)
                        ? "Approved by HR."
                        : reviewNote.Trim();

                request.ReviewedAt = DateTime.Now;

                /*
				 * Remove the employee's roster shifts during
				 * the approved leave period.
				 */
                if (shiftsDuringLeave.Any())
                {
                    _context.RosterShifts.RemoveRange(
                        shiftsDuringLeave);
                }

                string leaveTypeName =
                    request.LeaveType != null
                        ? request.LeaveType.LeaveTypeName
                        : "Leave";

                /*
				 * Notify the employee (in-app list).
				 */
                _context.Notifications.Add(new Notification
                {
                    StaffId = request.StaffId,

                    Title = "Leave Request Approved",

                    Message =
                        "Your " +
                        leaveTypeName +
                        " leave request from " +
                        request.StartDate.ToString("dd MMM yyyy") +
                        " to " +
                        request.EndDate.ToString("dd MMM yyyy") +
                        " was approved. " +
                        shiftsDuringLeave.Count +
                        " roster shift(s) were removed.",

                    CreatedAt = DateTime.Now,
                    IsRead = false
                });

                /*
				 * SaveChanges is automatically transactional.
				 * If one operation fails, none of the changes
				 * in this call will be committed.
				 */
                await _context.SaveChangesAsync();

                /*
				 * Push notification to the employee's phone.
				 * Only sent after the decision has been saved.
				 * Never throws, so it can't undo the approval.
				 */
                await _push.SendToStaffAsync(
                    request.StaffId,
                    "Leave request approved",
                    "Your " + leaveTypeName + " leave request (" +
                        request.StartDate.ToString("dd MMM") + " – " +
                        request.EndDate.ToString("dd MMM") +
                        ") was approved.",
                    "leave_approved",
                    request.LeaveRequestId.ToString(),
                    TimeSpan.FromDays(2));

                TempData["SuccessMessage"] =
                    "Leave request approved successfully. " +
                    shiftsDuringLeave.Count +
                    " roster shift(s) were removed.";

                return RedirectToAction(
                    nameof(PendingRequests));
            }
            catch (DbUpdateException exception)
            {
                TempData["ErrorMessage"] =
                    "Approval failed while updating the database: " +
                    exception.GetBaseException().Message;

                return RedirectToAction(
                    nameof(Assess),
                    new { id = leaveRequestId });
            }
            catch (Exception exception)
            {
                TempData["ErrorMessage"] =
                    "Approval failed: " +
                    exception.GetBaseException().Message;

                return RedirectToAction(
                    nameof(Assess),
                    new { id = leaveRequestId });
            }
        }

        // =========================================================
        // REJECT LEAVE REQUEST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            int leaveRequestId,
            string reviewNote)
        {
            var currentHrId = GetCurrentHrId();

            if (currentHrId == null)
            {
                return RedirectToAction(
                    "LogIn",
                    "Authentication");
            }

            var request = await _context.LeaveRequests
                .Include(item => item.Staff)
                .Include(item => item.LeaveType)
                .FirstOrDefaultAsync(item =>
                    item.LeaveRequestId == leaveRequestId);

            if (request == null)
                return NotFound();

            if (request.Status != LeaveRequestStatus.Pending)
            {
                TempData["ErrorMessage"] =
                    "Only pending leave requests can be rejected.";

                return RedirectToAction(
                    nameof(Assess),
                    new { id = leaveRequestId });
            }

            try
            {
                request.Status = LeaveRequestStatus.Rejected;

                request.ReviewNote =
                    string.IsNullOrWhiteSpace(reviewNote)
                        ? "Rejected by HR."
                        : reviewNote.Trim();

                request.ReviewedAt = DateTime.Now;

                string leaveTypeName =
                    request.LeaveType != null
                        ? request.LeaveType.LeaveTypeName
                        : "Leave";

                _context.Notifications.Add(new Notification
                {
                    StaffId = request.StaffId,

                    Title = "Leave Request Rejected",

                    Message =
                        "Your " +
                        leaveTypeName +
                        " leave request from " +
                        request.StartDate.ToString("dd MMM yyyy") +
                        " to " +
                        request.EndDate.ToString("dd MMM yyyy") +
                        " was rejected. Review note: " +
                        request.ReviewNote,

                    CreatedAt = DateTime.Now,
                    IsRead = false
                });

                await _context.SaveChangesAsync();

                /*
				 * Push notification to the employee's phone.
				 */
                await _push.SendToStaffAsync(
                    request.StaffId,
                    "Leave request rejected",
                    "Your " + leaveTypeName + " leave request (" +
                        request.StartDate.ToString("dd MMM") + " – " +
                        request.EndDate.ToString("dd MMM") +
                        ") was rejected. Reason: " + request.ReviewNote,
                    "leave_rejected",
                    request.LeaveRequestId.ToString(),
                    TimeSpan.FromDays(2));

                TempData["SuccessMessage"] =
                    "Leave request rejected successfully.";

                return RedirectToAction(
                    nameof(PendingRequests));
            }
            catch (Exception exception)
            {
                TempData["ErrorMessage"] =
                    "Rejection failed: " +
                    exception.GetBaseException().Message;

                return RedirectToAction(
                    nameof(Assess),
                    new { id = leaveRequestId });
            }
        }
    }
}