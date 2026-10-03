using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SkyTech.Models;
using SkyTech.Services;

namespace SkyTech.Controllers.Api
{
    // Staff-only now — Technician/DeliveryDriver don't roster or check in/out,
    // confirmed by the roster team's RosterShift being keyed to Staff alone.
    [ApiController]
    [Route("api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly JwtTokenService _jwt;

        public AuthApiController(ApplicationDbContext db, JwtTokenService jwt)
        {
            _db = db;
            _jwt = jwt;
        }

        public class LoginRequest
        {
            // Staff only has Email today (no separate Username field yet) —
            // kept as "Identifier" so nothing breaks if that changes later.
            public string Identifier { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Identifier) || string.IsNullOrWhiteSpace(req?.Password))
                return BadRequest(new { message = "Email and password are required." });

            var staff = _db.Staffs
                .FirstOrDefault(s => s.Username == req.Identifier && s.Password == req.Password);

            if (staff == null)
                return Unauthorized(new { message = "Invalid email or password." });

            // Field names kept as userId/userType (rather than staffId) to match
            // the mobile app's already-written LoginResponse/TokenManager exactly —
            // no Android changes needed for this backend rework.
            var token = _jwt.GenerateToken(staff.StaffId, "Staff");

            // NEW — staffName, so the mobile app can show the real name on
            // Home instead of a "Welcome back" placeholder.
            var staffName = $"{staff.FirstName} {staff.LastName}".Trim();

            return Ok(new { token, userType = "Staff", userId = staff.StaffId, staffName });
        }
    }
}