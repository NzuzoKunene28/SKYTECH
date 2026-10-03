using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.SkytechWorkers;
using SkyTech.Services;
using SkyTech.ViewModels;

namespace SkyTech.Controllers
{
    [AuthorizeUser]
    public class LeaveRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LeaveRequestService _leaveRequestService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public LeaveRequestController(
            ApplicationDbContext context,
            LeaveRequestService leaveRequestService,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _leaveRequestService = leaveRequestService;
            _webHostEnvironment = webHostEnvironment;
        }


        // =========================================================
        // GET CURRENT LOGGED-IN STAFF MEMBER
        // =========================================================

        private int? GetCurrentStaffId()
        {
            var userType =
                HttpContext.Session.GetString("UserType");

            if (userType != "Staff")
                return null;

            return HttpContext.Session.GetInt32("UserId");
        }


        // =========================================================
        // DISPLAY REQUEST LEAVE PAGE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> RequestLeave()
        {
            var staffId = GetCurrentStaffId();

            if (staffId == null)
            {
                return RedirectToAction(
                    "LogIn",
                    "Authentication");
            }


            var staff = await _context.Staffs
                .FirstOrDefaultAsync(
                    s => s.StaffId == staffId.Value);

            if (staff == null)
            {
                return RedirectToAction(
                    "LogIn",
                    "Authentication");
            }


            // Get all active leave types
            var leaveTypes = await _context.LeaveTypes
                .Where(l => l.IsActive)
                .OrderBy(l => l.LeaveTypeName)
                .ToListAsync();


            // Make sure this staff member has all
            // required current leave balances.
            await _leaveRequestService
                .EnsureCurrentLeaveBalancesAsync(
                    staffId.Value);


            // Get current leave balances.
            var balances =
                await _context.StaffLeaveBalances
                    .Where(b =>
                        b.StaffId == staffId.Value &&
                        DateTime.Today >=
                            b.CycleStartDate.Date &&
                        DateTime.Today <=
                            b.CycleEndDate.Date)
                    .Include(b => b.LeaveType)
                    .ToListAsync();


            // Pending leave also reduces available days.
            var pendingRequests =
                await _context.LeaveRequests
                    .Where(l =>
                        l.StaffId == staffId.Value &&
                        l.Status ==
                            LeaveRequestStatus.Pending)
                    .ToListAsync();


            var model =
                new RequestLeaveViewModel
                {
                    StaffName =
                        $"{staff.FirstName} {staff.LastName}",

                    LeaveTypes =
                        leaveTypes
                            .Select(l =>
                                new SelectListItem
                                {
                                    Value =
                                        l.LeaveTypeId.ToString(),

                                    Text =
                                        l.LeaveTypeName
                                })
                            .ToList(),

                    LeaveBalances =
                        balances
                            .Select(b =>
                            {
                                int pendingDays =
                                    pendingRequests
                                        .Where(l =>
                                            l.LeaveTypeId ==
                                            b.LeaveTypeId)
                                        .Sum(l =>
                                            l.WorkingDaysRequested);


                                int availableDays =
                                    b.TotalAllocatedDays
                                    - b.UsedDays
                                    - pendingDays;


                                return new
                                    LeaveBalanceSummaryViewModel
                                {
                                    LeaveTypeId =
                                            b.LeaveTypeId,

                                    LeaveTypeName =
                                            b.LeaveType
                                                .LeaveTypeName,

                                    TotalAllocatedDays =
                                            b.TotalAllocatedDays,

                                    UsedDays =
                                            b.UsedDays,

                                    PendingDays =
                                            pendingDays,

                                    AvailableDays =
                                            availableDays
                                };
                            })
                            .ToList(),

                    StartDate =
                        DateTime.Today,

                    EndDate =
                        DateTime.Today
                };


            return View(model);
        }


        // =========================================================
        // GET REQUIREMENTS FOR SELECTED LEAVE TYPE
        // =========================================================
        // The Request Leave page calls this when the employee
        // selects a leave type.
        //
        // It does NOT change or save anything.
        // It only reads the LeaveType table and returns the policy
        // information needed by the interface.
        // =========================================================

        [HttpGet]
        public async Task<IActionResult>
            GetLeaveTypeRequirements(int leaveTypeId)
        {
            var staffId = GetCurrentStaffId();

            if (staffId == null)
            {
                return Unauthorized();
            }


            var leaveType =
                await _context.LeaveTypes
                    .Where(l =>
                        l.LeaveTypeId == leaveTypeId &&
                        l.IsActive)
                    .Select(l => new
                    {
                        leaveTypeName =
                            l.LeaveTypeName,

                        requiresDocument =
                            l.RequiresDocument,

                        requiresBalance =
                            l.RequiresBalance,

                        isPaid =
                            l.IsPaid,

                        minimumNoticeDays =
                            l.MinimumNoticeDays,

                        maximumConsecutiveDays =
                            l.MaximumConsecutiveDays,

                        minimumEmploymentMonths =
                            l.MinimumEmploymentMonths
                    })
                    .FirstOrDefaultAsync();


            if (leaveType == null)
            {
                return NotFound();
            }


            return Json(leaveType);
        }


        // =========================================================
        // DISPLAY EMPLOYEE LEAVE HISTORY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> LeaveHistory()
        {
            var staffId = GetCurrentStaffId();

            if (staffId == null)
            {
                return RedirectToAction(
                    "LogIn",
                    "Authentication");
            }


            var requests =
                await _context.LeaveRequests
                    .Where(l =>
                        l.StaffId == staffId.Value)
                    .Include(l => l.LeaveType)
                    .OrderByDescending(l =>
                        l.SubmittedAt)
                    .ToListAsync();


            return View(requests);
        }


        // =========================================================
        // CHECK ELIGIBILITY
        // =========================================================
        // This does NOT save the request.
        //
        // The employee first checks whether the request meets
        // all leave rules.
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            CheckEligibility(
                RequestLeaveViewModel model)
        {
            var staffId = GetCurrentStaffId();

            if (staffId == null)
            {
                return Unauthorized();
            }


            var staff =
                await _context.Staffs
                    .FirstOrDefaultAsync(
                        s =>
                            s.StaffId ==
                            staffId.Value);


            if (staff == null)
            {
                return Unauthorized();
            }


            // Normal ViewModel validation.
            if (!ModelState.IsValid)
            {
                var modelErrors =
                    ModelState
                        .Where(x =>
                            x.Value != null &&
                            x.Value.Errors.Count > 0)
                        .SelectMany(x =>
                            x.Value!.Errors)
                        .Select(x =>
                            string.IsNullOrWhiteSpace(
                                x.ErrorMessage)

                                ? "Please check the information entered."

                                : x.ErrorMessage)
                        .ToList();


                return Json(new
                {
                    isEligible = false,
                    errors = modelErrors
                });
            }


            // Check leave business rules.
            var validationResult =
                await _leaveRequestService
                    .ValidateLeaveRequestAsync(
                        staffId.Value,
                        model.LeaveTypeId,
                        model.StartDate,
                        model.EndDate,
                        model.SupportingDocument != null);


            if (!validationResult.IsEligible)
            {
                return Json(new
                {
                    isEligible = false,

                    workingDaysRequested =
                        validationResult
                            .WorkingDaysRequested,

                    availableDays =
                        validationResult
                            .AvailableDays,

                    remainingDaysAfterRequest =
                        validationResult
                            .RemainingDaysAfterRequest,

                    errors =
                        validationResult.Errors
                });
            }


            // Eligible.
            // Nothing is saved at this stage.
            return Json(new
            {
                isEligible = true,

                workingDaysRequested =
                    validationResult
                        .WorkingDaysRequested,

                availableDays =
                    validationResult
                        .AvailableDays,

                remainingDaysAfterRequest =
                    validationResult
                        .RemainingDaysAfterRequest,

                errors =
                    new List<string>()
            });
        }


        // =========================================================
        // FINAL LEAVE REQUEST SUBMISSION
        // =========================================================
        // Everything is validated again before saving.
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            RequestLeave(
                RequestLeaveViewModel model)
        {
            var staffId = GetCurrentStaffId();

            if (staffId == null)
            {
                return RedirectToAction(
                    "LogIn",
                    "Authentication");
            }


            var staff =
                await _context.Staffs
                    .FirstOrDefaultAsync(
                        s =>
                            s.StaffId ==
                            staffId.Value);


            if (staff == null)
            {
                return RedirectToAction(
                    "LogIn",
                    "Authentication");
            }


            // =====================================================
            // REFILL VIEW INFORMATION
            // =====================================================

            model.StaffName =
                $"{staff.FirstName} {staff.LastName}";


            model.LeaveTypes =
                await _context.LeaveTypes
                    .Where(l =>
                        l.IsActive)
                    .OrderBy(l =>
                        l.LeaveTypeName)
                    .Select(l =>
                        new SelectListItem
                        {
                            Value =
                                l.LeaveTypeId
                                    .ToString(),

                            Text =
                                l.LeaveTypeName
                        })
                    .ToListAsync();


            // Make sure required balances exist.
            await _leaveRequestService
                .EnsureCurrentLeaveBalancesAsync(
                    staffId.Value);


            var balances =
                await _context.StaffLeaveBalances
                    .Where(b =>
                        b.StaffId ==
                            staffId.Value &&

                        DateTime.Today >=
                            b.CycleStartDate.Date &&

                        DateTime.Today <=
                            b.CycleEndDate.Date)
                    .Include(b =>
                        b.LeaveType)
                    .ToListAsync();


            var pendingRequests =
                await _context.LeaveRequests
                    .Where(l =>
                        l.StaffId ==
                            staffId.Value &&

                        l.Status ==
                            LeaveRequestStatus.Pending)
                    .ToListAsync();


            model.LeaveBalances =
                balances
                    .Select(b =>
                    {
                        int pendingDays =
                            pendingRequests
                                .Where(l =>
                                    l.LeaveTypeId ==
                                    b.LeaveTypeId)
                                .Sum(l =>
                                    l.WorkingDaysRequested);


                        int availableDays =
                            b.TotalAllocatedDays
                            - b.UsedDays
                            - pendingDays;


                        return new
                            LeaveBalanceSummaryViewModel
                        {
                            LeaveTypeId =
                                    b.LeaveTypeId,

                            LeaveTypeName =
                                    b.LeaveType
                                        .LeaveTypeName,

                            TotalAllocatedDays =
                                    b.TotalAllocatedDays,

                            UsedDays =
                                    b.UsedDays,

                            PendingDays =
                                    pendingDays,

                            AvailableDays =
                                    availableDays
                        };
                    })
                    .ToList();


            // =====================================================
            // NORMAL FORM VALIDATION
            // =====================================================

            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // =====================================================
            // RECHECK BUSINESS RULES
            // =====================================================
            // CheckEligibility is only a preview.
            //
            // We must validate again here because information may
            // have changed between checking and final submission.
            // =====================================================

            var validationResult =
                await _leaveRequestService
                    .ValidateLeaveRequestAsync(
                        staffId.Value,
                        model.LeaveTypeId,
                        model.StartDate,
                        model.EndDate,
                        model.SupportingDocument != null);


            model.WorkingDaysRequested =
                validationResult
                    .WorkingDaysRequested;


            model.AvailableDays =
                validationResult
                    .AvailableDays;


            model.RemainingDaysAfterRequest =
                validationResult
                    .RemainingDaysAfterRequest;


            if (!validationResult.IsEligible)
            {
                foreach (
                    var error
                    in validationResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error);
                }


                return View(model);
            }


            // =====================================================
            // SAVE SUPPORTING DOCUMENT
            // =====================================================

            string? supportingDocumentPath =
                null;


            if (model.SupportingDocument != null &&
                model.SupportingDocument.Length > 0)
            {
                var uploadsFolder =
                    Path.Combine(
                        _webHostEnvironment
                            .WebRootPath,

                        "uploads",

                        "leave-documents");


                Directory.CreateDirectory(
                    uploadsFolder);


                var extension =
                    Path.GetExtension(
                        model.SupportingDocument
                            .FileName);


                var uniqueFileName =
                    $"{Guid.NewGuid()}{extension}";


                var physicalPath =
                    Path.Combine(
                        uploadsFolder,
                        uniqueFileName);


                using (
                    var stream =
                        new FileStream(
                            physicalPath,
                            FileMode.Create))
                {
                    await model
                        .SupportingDocument
                        .CopyToAsync(stream);
                }


                supportingDocumentPath =
                    $"/uploads/leave-documents/{uniqueFileName}";
            }


            // =====================================================
            // CREATE LEAVE REQUEST
            // =====================================================

            var leaveRequest =
                new LeaveRequest
                {
                    StaffId =
                        staffId.Value,

                    LeaveTypeId =
                        model.LeaveTypeId,

                    Reason =
                        model.Reason,

                    StartDate =
                        model.StartDate.Date,

                    EndDate =
                        model.EndDate.Date,

                    WorkingDaysRequested =
                        validationResult
                            .WorkingDaysRequested,

                    SupportingDocumentPath =
                        supportingDocumentPath,

                    Status =
                        LeaveRequestStatus.Pending,

                    SubmittedAt =
                        DateTime.Now
                };


            _context.LeaveRequests.Add(
                leaveRequest);


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Your leave request has been submitted successfully and is awaiting assessment.";


            return RedirectToAction(
                nameof(RequestLeave));
        }
    }
}