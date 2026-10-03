using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.SkytechWorkers;
using SkyTech.Models.AttendanceMods;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using SkyTech.Services;
using System.IO;

namespace SkyTech.Controllers
{
    public class StaffRosterController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly FaceRecognitionService _faceService;

        public StaffRosterController(
            ApplicationDbContext db,
            IWebHostEnvironment webHostEnvironment,
            FaceRecognitionService faceService)
        {
            this.db = db;
            _webHostEnvironment = webHostEnvironment;
            _faceService = faceService;
        }

        public IActionResult StaffDash()
        {
            int? staffId = HttpContext.Session.GetInt32("UserId");
            if (staffId == null) return RedirectToAction("Login", "Authentication");

            var todayShift = db.RosterShifts
                .FirstOrDefault(rs => rs.StaffId == staffId.Value
                                    && rs.WorkDate.Date == DateTime.Today);

            Attendance todayAttendance = null;
            if (todayShift != null)
            {
                todayAttendance = db.Attendances
                    .FirstOrDefault(a => a.RosterShiftId == todayShift.RosterShiftId);
            }

            ViewBag.HasShiftToday = todayShift != null;
            ViewBag.IsCheckedIn = todayAttendance?.CheckInTime != null && todayAttendance?.CheckOutTime == null;
            ViewBag.TodayCheckInTime = todayAttendance?.CheckInTime;
            ViewBag.TodayCheckOutTime = todayAttendance?.CheckOutTime;

            var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            var thisWeekAttendance = db.Attendances
                .Where(a => a.StaffId == staffId.Value && a.RosterShift.WorkDate >= startOfWeek)
                .ToList();

            ViewBag.HoursThisWeek = thisWeekAttendance
                .Where(a => a.CheckOutTime.HasValue)
                .Sum(a => (a.CheckOutTime!.Value - a.CheckInTime!.Value).TotalHours);

            ViewBag.DaysAttendedThisWeek = thisWeekAttendance.Count(a => a.CheckInTime != null);

            return View();
        }


        public async Task<IActionResult> MyRoster()
        {
            int? staffId = HttpContext.Session.GetInt32("UserId");
            if (staffId == null) return RedirectToAction("Login", "Authentication");

            var loggedInStaff = await db.Staffs
                .Include(s => s.Positions)
                .FirstOrDefaultAsync(s => s.StaffId == staffId.Value);

            if (loggedInStaff == null) return RedirectToAction("Login", "Authentication");

            // Find the most recent PUBLISHED roster for this staff member's branch
            var roster = await db.Rosters
                .Include(r => r.Branch)
                .Include(r => r.RosterShifts).ThenInclude(s => s.Staff)
                .Include(r => r.RosterShifts).ThenInclude(s => s.Position)
                .Where(r => r.BranchId == loggedInStaff.BranchId &&
                            r.Status == RosterStatus.Published)
                .OrderByDescending(r => r.WeekStartDate)
                .FirstOrDefaultAsync();

            if (roster == null)
            {
                ViewBag.NoRoster = true;
                ViewBag.CurrentStaffId = loggedInStaff.StaffId;
                return View();
            }

            // Only shifts for THIS staff member's position — but showing everyone in that position
            var positionShifts = roster.RosterShifts
                .Where(s => s.PositionId == loggedInStaff.PositionId)
                .ToList();

            ViewBag.PositionName = loggedInStaff.Positions?.PositionName;
            ViewBag.WeekStartDate = roster.WeekStartDate;
            ViewBag.BranchName = roster.Branch.BranchName;
            ViewBag.CurrentStaffId = loggedInStaff.StaffId;

            return View(positionShifts);
        }
        [HttpGet]
        public IActionResult EnrollFace()
        {
            int? staffId = HttpContext.Session.GetInt32("UserId");
            if (staffId == null) return RedirectToAction("Login", "Authentication");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollFace(IFormFile photo)
        {
            int? staffId = HttpContext.Session.GetInt32("UserId");
            if (staffId == null) return RedirectToAction("Login", "Authentication");

            if (photo == null || photo.Length == 0)
            {
                TempData["Error"] = "Please choose a photo.";
                return RedirectToAction("EnrollFace");
            }

            using var ms = new MemoryStream();
            await photo.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var embedding = _faceService.ExtractEmbedding(bytes);
            if (embedding == null)
            {
                TempData["Error"] = "No face was detected in that photo. Please try again with a clear, well-lit photo of your face.";
                return RedirectToAction("EnrollFace");
            }

            var folder = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "StaffReference");
            Directory.CreateDirectory(folder);
            var fileName = $"{staffId}_{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
            var filePath = Path.Combine(folder, fileName);
            await System.IO.File.WriteAllBytesAsync(filePath, bytes);

            var staff = await db.Staffs.FindAsync(staffId.Value);
            staff.ReferencePhotoPath = $"/Uploads/StaffReference/{fileName}";
            staff.ReferenceEmbedding = FaceRecognitionService.SerializeEmbedding(embedding);
            await db.SaveChangesAsync();

            TempData["Success"] = "Reference photo saved successfully.";
            return RedirectToAction("StaffDash");
        }

    }
}