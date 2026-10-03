using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;

namespace SkyTech.Controllers
{
    [AuthorizeUser]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // GET CURRENT LOGGED-IN STAFF ID
        // =====================================================

        private int? GetCurrentStaffId()
        {
            var userType =
                HttpContext.Session.GetString("UserType");

            if (userType != "Staff")
            {
                return null;
            }

            return HttpContext.Session.GetInt32("UserId");
        }


        // =====================================================
        // DISPLAY STAFF NOTIFICATIONS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int? staffId = GetCurrentStaffId();

            if (staffId == null)
            {
                return RedirectToAction(
                    "LogIn",
                    "Authentication");
            }

            var notifications =
                await _context.Notifications

                    .Where(n =>
                        n.StaffId == staffId.Value)

                    .OrderByDescending(n =>
                        n.CreatedAt)

                    .ToListAsync();

            return View(notifications);
        }


        // =====================================================
        // MARK ONE NOTIFICATION AS READ
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(
            int notificationId)
        {
            int? staffId = GetCurrentStaffId();

            if (staffId == null)
            {
                return RedirectToAction(
                    "LogIn",
                    "Authentication");
            }

            var notification =
                await _context.Notifications
                    .FirstOrDefaultAsync(n =>
                        n.NotificationId == notificationId &&
                        n.StaffId == staffId.Value);

            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = true;

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}