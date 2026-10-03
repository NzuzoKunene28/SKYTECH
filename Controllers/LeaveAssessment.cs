using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.SkytechWorkers;
using SkyTech.ViewModels;

namespace SkyTech.Controllers
{
	[AuthorizeUser]
	public class LeaveAssessmentController : Controller
	{
		private readonly ApplicationDbContext _context;

		public LeaveAssessmentController(
			ApplicationDbContext context)
		{
			_context = context;
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

			var model = new AssessLeaveViewModel
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
			};

			return View(model);
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

			/*
			 * Check department minimum staffing.
			 */
			int minimumRequired =
				request.Staff?.Positions?.Departments
					?.MinimumStaffRequired
				?? 0;

			if (minimumRequired > 0)
			{
				foreach (var employeeShift
					in shiftsDuringLeave)
				{
					int remainingStaff =
						await _context.RosterShifts
							.CountAsync(shift =>
								shift.RosterShiftId !=
									employeeShift.RosterShiftId &&
								shift.WorkDate.Date ==
									employeeShift.WorkDate.Date &&
								shift.BranchId ==
									employeeShift.BranchId &&
								shift.PositionId ==
									employeeShift.PositionId);

					if (remainingStaff < minimumRequired)
					{
						TempData["ErrorMessage"] =
							"Leave cannot be approved because " +
							"removing the employee from the roster on " +
							employeeShift.WorkDate
								.ToString("dd MMM yyyy") +
							" would leave only " +
							remainingStaff +
							" staff member(s). The minimum required is " +
							minimumRequired + ".";

						return RedirectToAction(
							nameof(Assess),
							new { id = leaveRequestId });
					}
				}
			}

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
				 * Notify the employee.
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

				_context.Notifications.Add(new Notification
				{
					StaffId = request.StaffId,

					Title = "Leave Request Rejected",

					Message =
		"Your " +
		request.LeaveType.LeaveTypeName +
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