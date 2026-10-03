using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.AttendanceMods;
using SkyTech.ViewModels;

namespace SkyTech.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AttendanceController(ApplicationDbContext context)
        {
            _db = context;
        }

        [HttpGet]
        public IActionResult MyAttendance()
        {
            int? staffId = HttpContext.Session.GetInt32("UserId");
            if (staffId == null)
                return RedirectToAction("Login", "Authentication");

            var history = _db.Attendances
                .Include(a => a.RosterShift)
                .Where(a => a.StaffId == staffId.Value)
                .OrderByDescending(a => a.RosterShift.WorkDate)
                .ToList()
                .Select(a => new AttendanceRowViewModel
                {
                    Date = a.RosterShift.WorkDate,
                    ScheduledStart = a.RosterShift.StartTime,
                    CheckInTime = a.CheckInTime,
                    CheckOutTime = a.CheckOutTime,
                    HoursWorked = a.CheckOutTime.HasValue
                        ? (a.CheckOutTime.Value - a.CheckInTime.Value).TotalHours
                        : (double?)null,
                    Status = AttendanceStatusCalculator.GetStatus(a.RosterShift, a)
                })
                .ToList();

            return View(history);
        }
    }
}