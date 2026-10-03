using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.SkytechWorkers;
using SkyTech.Services;

namespace SkyTech.Controllers.Api
{
    public class EligibilityCheckBody
    {
        public int LeaveTypeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool HasSupportingDocument { get; set; }
    }

    [ApiController]
    [Route("api/leave")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class LeaveApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly LeaveRequestService _leaveRequestService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public LeaveApiController(
            ApplicationDbContext db,
            LeaveRequestService leaveRequestService,
            IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _leaveRequestService = leaveRequestService;
            _webHostEnvironment = webHostEnvironment;
        }

        private int GetStaffId() => int.Parse(User.FindFirstValue("UserId")!);

        [HttpGet("types")]
        public async Task<IActionResult> GetLeaveTypes()
        {
            var types = await _db.LeaveTypes
                .Where(l => l.IsActive)
                .OrderBy(l => l.LeaveTypeName)
                .Select(l => new
                {
                    leaveTypeId = l.LeaveTypeId,
                    leaveTypeName = l.LeaveTypeName,
                    requiresDocument = l.RequiresDocument,
                    requiresBalance = l.RequiresBalance,
                    isPaid = l.IsPaid,
                    minimumNoticeDays = l.MinimumNoticeDays,
                    maximumConsecutiveDays = l.MaximumConsecutiveDays,
                    minimumEmploymentMonths = l.MinimumEmploymentMonths
                })
                .ToListAsync();

            return Ok(types);
        }

        [HttpGet("balances")]
        public async Task<IActionResult> GetBalances()
        {
            var staffId = GetStaffId();

            await _leaveRequestService.EnsureCurrentLeaveBalancesAsync(staffId);

            var balances = await _db.StaffLeaveBalances
                .Where(b =>
                    b.StaffId == staffId &&
                    DateTime.Today >= b.CycleStartDate.Date &&
                    DateTime.Today <= b.CycleEndDate.Date)
                .Include(b => b.LeaveType)
                .ToListAsync();

            var pendingRequests = await _db.LeaveRequests
                .Where(l => l.StaffId == staffId && l.Status == LeaveRequestStatus.Pending)
                .ToListAsync();

            var result = balances.Select(b =>
            {
                int pendingDays = pendingRequests
                    .Where(l => l.LeaveTypeId == b.LeaveTypeId)
                    .Sum(l => l.WorkingDaysRequested);

                return new
                {
                    leaveTypeId = b.LeaveTypeId,
                    leaveTypeName = b.LeaveType.LeaveTypeName,
                    totalAllocatedDays = b.TotalAllocatedDays,
                    usedDays = b.UsedDays,
                    pendingDays = pendingDays,
                    availableDays = b.TotalAllocatedDays - b.UsedDays - pendingDays
                };
            }).ToList();

            return Ok(result);
        }

        [HttpPost("check-eligibility")]
        public async Task<IActionResult> CheckEligibility([FromBody] EligibilityCheckBody body)
        {
            var staffId = GetStaffId();

            var validationResult = await _leaveRequestService.ValidateLeaveRequestAsync(
                staffId,
                body.LeaveTypeId,
                body.StartDate,
                body.EndDate,
                body.HasSupportingDocument);

            return Ok(new
            {
                isEligible = validationResult.IsEligible,
                workingDaysRequested = validationResult.WorkingDaysRequested,
                availableDays = validationResult.AvailableDays,
                remainingDaysAfterRequest = validationResult.RemainingDaysAfterRequest,
                errors = validationResult.Errors
            });
        }

        [HttpPost("request")]
        public async Task<IActionResult> SubmitRequest(
            [FromForm] int leaveTypeId,
            [FromForm] string reason,
            [FromForm] DateTime startDate,
            [FromForm] DateTime endDate,
            IFormFile? supportingDocument)
        {
            var staffId = GetStaffId();

            if (string.IsNullOrWhiteSpace(reason))
                return BadRequest(new { message = "Please provide a reason for the request." });

            var validationResult = await _leaveRequestService.ValidateLeaveRequestAsync(
                staffId, leaveTypeId, startDate, endDate, supportingDocument != null);

            if (!validationResult.IsEligible)
            {
                return BadRequest(new { message = "Request not eligible.", errors = validationResult.Errors });
            }

            string? supportingDocumentPath = null;

            if (supportingDocument != null && supportingDocument.Length > 0)
            {
                var uploadsFolder = Path.Combine(
                    _webHostEnvironment.WebRootPath, "uploads", "leave-documents");

                Directory.CreateDirectory(uploadsFolder);

                var extension = Path.GetExtension(supportingDocument.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var physicalPath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await supportingDocument.CopyToAsync(stream);
                }

                supportingDocumentPath = $"/uploads/leave-documents/{uniqueFileName}";
            }

            var leaveRequest = new LeaveRequest
            {
                StaffId = staffId,
                LeaveTypeId = leaveTypeId,
                Reason = reason,
                StartDate = startDate.Date,
                EndDate = endDate.Date,
                WorkingDaysRequested = validationResult.WorkingDaysRequested,
                SupportingDocumentPath = supportingDocumentPath,
                Status = LeaveRequestStatus.Pending,
                SubmittedAt = DateTime.Now
            };

            _db.LeaveRequests.Add(leaveRequest);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Your leave request has been submitted successfully and is awaiting assessment." });
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var staffId = GetStaffId();

            var requests = await _db.LeaveRequests
                .Where(l => l.StaffId == staffId)
                .Include(l => l.LeaveType)
                .OrderByDescending(l => l.SubmittedAt)
                .Select(l => new
                {
                    leaveRequestId = l.LeaveRequestId,
                    leaveTypeName = l.LeaveType.LeaveTypeName,
                    status = l.Status.ToString(),
                    reason = l.Reason,
                    startDate = l.StartDate,
                    endDate = l.EndDate,
                    workingDaysRequested = l.WorkingDaysRequested,
                    submittedAt = l.SubmittedAt,
                    hasSupportingDocument = l.SupportingDocumentPath != null,
                    reviewNote = l.ReviewNote,
                    reviewedAt = l.ReviewedAt
                })
                .ToListAsync();

            return Ok(requests);
        }
    }
}