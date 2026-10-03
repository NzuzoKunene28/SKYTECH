using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SkyTech.Models;
using SkyTech.Models.AttendanceMods;
using SkyTech.Services;

namespace SkyTech.Controllers.Api
{
    [ApiController]
    [Route("api/attendance")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class AttendanceApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly FaceRecognitionService _faceService;
        private readonly double _faceMatchThreshold;

        public AttendanceApiController(
            ApplicationDbContext db,
            IWebHostEnvironment env,
            FaceRecognitionService faceService,
            IConfiguration config)
        {
            _db = db;
            _env = env;
            _faceService = faceService;
            _faceMatchThreshold = double.Parse(config["FaceMatch:Threshold"] ?? "0.5");
        }

        private int GetStaffId() => int.Parse(User.FindFirstValue("UserId")!);

        public class CheckRequest
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string DeviceId { get; set; }
            public IFormFile Photo { get; set; }
        }

        [HttpPost("checkin")]
        public async Task<IActionResult> CheckIn([FromForm] CheckRequest req)
        {
            var staffId = GetStaffId();

            // South African date, the same clock the shifts endpoints use.
            var today = SaTime.Today;

            var rosterShift = await _db.RosterShifts
                .FirstOrDefaultAsync(rs => rs.StaffId == staffId
                                        && rs.WorkDate.Date == today);

            if (rosterShift == null)
                return NotFound(new { message = "No shift scheduled for today." });

            var existing = await _db.Attendances
                .FirstOrDefaultAsync(a => a.RosterShiftId == rosterShift.RosterShiftId);

            if (existing?.CheckInTime != null)
                return BadRequest(new { message = "Already checked in for this shift." });

            var staff = await _db.Staffs.FindAsync(staffId);

            var workplaceBranch = await _db.Branches.FindAsync(staff.BranchId);
            if (workplaceBranch?.Latitude == null || workplaceBranch.Longitude == null || workplaceBranch.RadiusMeters == null)
                return StatusCode(500, new { message = "This branch does not have geofence coordinates configured yet." });

            var distance = GeoDistanceHelper.DistanceMeters(
                req.Latitude, req.Longitude, workplaceBranch.Latitude.Value, workplaceBranch.Longitude.Value);

            if (distance > workplaceBranch.RadiusMeters.Value)
                return BadRequest(new { message = "You are outside the workplace radius.", distanceMeters = distance });

            // ── Face verification ──────────────────────────────────────
            // Read the photo bytes ONCE, reuse for both verification and
            // saving to disk — re-reading an IFormFile's stream twice is
            // unreliable depending on how ASP.NET Core buffered the upload.
            byte[] photoBytes;
            using (var ms = new MemoryStream())
            {
                await req.Photo.CopyToAsync(ms);
                photoBytes = ms.ToArray();
            }

            if (string.IsNullOrEmpty(staff.ReferenceEmbedding))
            {
                return BadRequest(new { message = "You haven't enrolled a reference photo yet." });
            }

            var liveEmbedding = _faceService.ExtractEmbedding(photoBytes);
            if (liveEmbedding == null)
            {
                return BadRequest(new { message = "No face was detected in your check-in photo. Please try again with better lighting." });
            }

            var referenceEmbedding = FaceRecognitionService.DeserializeEmbedding(staff.ReferenceEmbedding);
            var similarity = FaceRecognitionService.CosineSimilarity(liveEmbedding, referenceEmbedding);

            if (similarity < _faceMatchThreshold)
            {
                return BadRequest(new { message = "Face verification failed. This doesn't match your enrolled reference photo.", similarity });
            }

            string photoPath = await SavePhotoBytes(photoBytes, req.Photo.FileName, staffId, "checkin");

            var attendance = existing ?? new Attendance
            {
                RosterShiftId = rosterShift.RosterShiftId,
                StaffId = staffId
            };

            attendance.CheckInTime = SaTime.Now;
            attendance.CheckInLat = req.Latitude;
            attendance.CheckInLng = req.Longitude;
            attendance.CheckInPhotoPath = photoPath;
            attendance.DeviceId = req.DeviceId;

            if (existing == null)
                _db.Attendances.Add(attendance);

            await _db.SaveChangesAsync();

            return Ok(new { message = "Checked in successfully.", checkInTime = attendance.CheckInTime, similarity });
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> CheckOut([FromForm] CheckRequest req)
        {
            var staffId = GetStaffId();

            var today = SaTime.Today;

            var rosterShift = await _db.RosterShifts
                .FirstOrDefaultAsync(rs => rs.StaffId == staffId
                                        && rs.WorkDate.Date == today);

            if (rosterShift == null)
                return NotFound(new { message = "No shift scheduled for today." });

            var attendance = await _db.Attendances
                .FirstOrDefaultAsync(a => a.RosterShiftId == rosterShift.RosterShiftId);

            if (attendance?.CheckInTime == null)
                return BadRequest(new { message = "You must check in before checking out." });

            if (attendance.CheckOutTime != null)
                return BadRequest(new { message = "Already checked out for this shift." });

            if (attendance.DeviceId != req.DeviceId)
                return Unauthorized(new { message = "Device does not match the device used to check in." });

            var staff = await _db.Staffs.FindAsync(staffId);
            var workplaceBranch = await _db.Branches.FindAsync(staff.BranchId);

            if (workplaceBranch?.Latitude == null || workplaceBranch.Longitude == null || workplaceBranch.RadiusMeters == null)
                return StatusCode(500, new { message = "This branch does not have geofence coordinates configured yet." });

            var distance = GeoDistanceHelper.DistanceMeters(
                req.Latitude, req.Longitude, workplaceBranch.Latitude.Value, workplaceBranch.Longitude.Value);

            if (distance > workplaceBranch.RadiusMeters.Value)
                return BadRequest(new { message = "You are outside the workplace radius.", distanceMeters = distance });

            // ── Face verification ──────────────────────────────────────
            // Same check as CheckIn — device binding alone only proves it's
            // the same phone, not the same person holding it. Read the
            // photo bytes ONCE, reuse for both verification and saving.
            byte[] photoBytes;
            using (var ms = new MemoryStream())
            {
                await req.Photo.CopyToAsync(ms);
                photoBytes = ms.ToArray();
            }

            if (string.IsNullOrEmpty(staff.ReferenceEmbedding))
            {
                return BadRequest(new { message = "You haven't enrolled a reference photo yet." });
            }

            var liveEmbedding = _faceService.ExtractEmbedding(photoBytes);
            if (liveEmbedding == null)
            {
                return BadRequest(new { message = "No face was detected in your check-out photo. Please try again with better lighting." });
            }

            var referenceEmbedding = FaceRecognitionService.DeserializeEmbedding(staff.ReferenceEmbedding);
            var similarity = FaceRecognitionService.CosineSimilarity(liveEmbedding, referenceEmbedding);

            if (similarity < _faceMatchThreshold)
            {
                return BadRequest(new { message = "Face verification failed. This doesn't match your enrolled reference photo.", similarity });
            }

            string photoPath = await SavePhotoBytes(photoBytes, req.Photo.FileName, staffId, "checkout");

            attendance.CheckOutTime = SaTime.Now;
            attendance.CheckOutLat = req.Latitude;
            attendance.CheckOutLng = req.Longitude;
            attendance.CheckOutPhotoPath = photoPath;

            await _db.SaveChangesAsync();

            var hoursWorked = (attendance.CheckOutTime.Value - attendance.CheckInTime.Value).TotalHours;

            return Ok(new { message = "Checked out successfully.", checkOutTime = attendance.CheckOutTime, hoursWorked, similarity });
        }

        private async Task<string> SavePhotoBytes(byte[] photoBytes, string originalFileName, int staffId, string prefix)
        {
            var folder = Path.Combine(_env.WebRootPath, "Uploads", "Attendance");
            Directory.CreateDirectory(folder);

            var fileName = $"{prefix}_{staffId}_{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
            var filePath = Path.Combine(folder, fileName);

            await System.IO.File.WriteAllBytesAsync(filePath, photoBytes);

            return $"/Uploads/Attendance/{fileName}";
        }

        // Returns the staff member's attendance history, most recent first,
        // for the "My Attendance" tab on the Android app.
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var staffId = GetStaffId();

            var attendances = await _db.Attendances
                .Include(a => a.RosterShift)
                    .ThenInclude(rs => rs.Branch)
                .Where(a => a.StaffId == staffId)
                .OrderByDescending(a => a.RosterShift.WorkDate)
                .ToListAsync();

            var records = attendances.Select(a => new
            {
                workDate = a.RosterShift.WorkDate,
                scheduledStart = a.RosterShift.StartTime.ToString(@"hh\:mm"),
                scheduledEnd = a.RosterShift.EndTime.ToString(@"hh\:mm"),
                branchName = a.RosterShift.Branch != null ? a.RosterShift.Branch.BranchName : "Unassigned",
                checkInTime = a.CheckInTime,
                checkOutTime = a.CheckOutTime,
                hoursWorked = a.CheckInTime != null && a.CheckOutTime != null
                    ? (double?)(a.CheckOutTime.Value - a.CheckInTime.Value).TotalHours
                    : null,
                status = AttendanceStatusCalculator.GetStatus(a.RosterShift, a).ToString()
            }).ToList();

            return Ok(records);
        }
    }
}