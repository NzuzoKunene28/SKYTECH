using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SkyTech.Models;
using SkyTech.Services;

namespace SkyTech.Controllers.Api
{
    [ApiController]
    [Route("api/enrollment")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class EnrollmentApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly FaceRecognitionService _faceService;

        public EnrollmentApiController(ApplicationDbContext db, IWebHostEnvironment env, FaceRecognitionService faceService)
        {
            _db = db;
            _env = env;
            _faceService = faceService;
        }

        private int GetStaffId() => int.Parse(User.FindFirstValue("UserId")!);

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var staff = await _db.Staffs.FindAsync(GetStaffId());
            return Ok(new { hasReferencePhoto = !string.IsNullOrEmpty(staff.ReferenceEmbedding) });
        }

        [HttpPost("face")]
        public async Task<IActionResult> EnrollFace(IFormFile photo)
        {
            var staffId = GetStaffId();

            if (photo == null || photo.Length == 0)
                return BadRequest(new { message = "No photo was provided." });

            byte[] photoBytes;
            using (var ms = new MemoryStream())
            {
                await photo.CopyToAsync(ms);
                photoBytes = ms.ToArray();
            }

            var embedding = _faceService.ExtractEmbedding(photoBytes);
            if (embedding == null)
                return BadRequest(new { message = "No face was detected in that photo. Please try again with better lighting." });

            var folder = Path.Combine(_env.WebRootPath, "Uploads", "StaffReference");
            Directory.CreateDirectory(folder);
            var fileName = $"{staffId}_{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
            var filePath = Path.Combine(folder, fileName);
            await System.IO.File.WriteAllBytesAsync(filePath, photoBytes);

            var staff = await _db.Staffs.FindAsync(staffId);
            staff.ReferencePhotoPath = $"/Uploads/StaffReference/{fileName}";
            staff.ReferenceEmbedding = FaceRecognitionService.SerializeEmbedding(embedding);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Reference photo enrolled successfully." });
        }
    }
}