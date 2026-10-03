using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;

namespace SkyTech.Controllers.Api
{
    [ApiController]
    [Route("api/staff")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class StaffApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public StaffApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        private int GetStaffId() => int.Parse(User.FindFirstValue("UserId")!);

        // Backs the Profile screen's "Details" card (Employee ID / Email /
        // Phone) and its name/branch/role header. Verified against the
        // real Staff entity: Email, PhoneNumber, the Branches navigation
        // property, and Positions.Title (StaffPosition) are all confirmed.
        [HttpGet("me")]
        public IActionResult GetMe()
        {
            var staffId = GetStaffId();

            var staff = _db.Staffs
                .Include(s => s.Positions)
                .FirstOrDefault(s => s.StaffId == staffId);

            if (staff == null)
                return NotFound(new { message = "Staff record not found." });

            var branch = _db.Branches.Find(staff.BranchId);

            return Ok(new
            {
                staffId = staff.StaffId,
                fullName = staff.FirstName + " " + staff.LastName,
                employeeCode = $"SKY-{staff.StaffId:D5}",
                email = staff.Email,
                phone = staff.PhoneNumber,
                role = staff.Positions != null ? staff.Positions.PositionName: "Staff",
                branchName = branch != null ? branch.BranchName : "Unassigned"
            });
        }
    }
}