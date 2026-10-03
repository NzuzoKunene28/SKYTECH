using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.SkytechWorkers;
using SkyTech.Services;

namespace SkyTech.Controllers
{
    public class RosterController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly IRosterSchedulingService _rosterService;

        public RosterController(ApplicationDbContext db, IRosterSchedulingService rosterService)
        {
            this.db = db;
            _rosterService = rosterService;
        }

        // GET: the HR form to trigger generation
        //public async Task<IActionResult> GenerateWeekly()
        //{
        //    ViewBag.Branches = new SelectList(await db.Branches.ToListAsync(), "BranchId", "BranchName");
        //    ViewBag.Positions = new SelectList(await db.StaffPositions.ToListAsync(), "PositionId", "PositionName");
        //    return View();
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> GenerateWeekly(
        //    int branchId,
        //    int positionId,
        //    DateTime weekStartDate,
        //    TimeSpan startTime,
        //    TimeSpan endTime,
        //    int staffNeededPerDay,
        //    string[] selectedDays)
        //{
        //    if (selectedDays == null || selectedDays.Length == 0)
        //    {
        //        ModelState.AddModelError("", "Select at least one day.");
        //        ViewBag.Branches = new SelectList(await db.Branches.ToListAsync(), "BranchId", "BranchName");
        //        ViewBag.Positions = new SelectList(await db.StaffPositions.ToListAsync(), "PositionId", "PositionName");
        //        return View();
        //    }

        //    var activeDays = selectedDays
        //        .Select(d => Enum.Parse<DayOfWeek>(d))
        //        .ToArray();

        //    int shiftsCreated = await _rosterService.GenerateWeeklyShiftsAsync(
        //        branchId, positionId, weekStartDate, startTime, endTime, activeDays, staffNeededPerDay);

        //    var roster = await db.Rosters.FirstOrDefaultAsync(r =>
        //        r.BranchId == branchId && r.WeekStartDate == weekStartDate);

        //    TempData["successful"] = $"{shiftsCreated} shifts created.";
        //    return RedirectToAction("RosterDetails", new { rosterId = roster.RosterId });
        //}


        // GET
        public async Task<IActionResult> GenerateWeekly()
        {
            ViewBag.Branches = new SelectList(await db.Branches.ToListAsync(), "BranchId", "BranchName");
            var positions = await db.StaffPositions.OrderBy(p => p.PositionName).ToListAsync();
            return View(positions); // model = list of all positions
        }

        private async Task PrepareRosterView()
        {
            ViewBag.Branches = new SelectList(
                await db.Branches.ToListAsync(),
                "BranchId",
                "BranchName"
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateWeekly(
            int branchId,
            DateTime weekStartDate,
            TimeSpan startTime,
            TimeSpan endTime,
            int[] positionIds,
            int[] staffNeeded)
        {
            var activeDays = new[]
            {
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    };

            // Make sure both arrays match
            if (positionIds == null ||
                staffNeeded == null ||
                positionIds.Length != staffNeeded.Length)
            {
                ModelState.AddModelError(
                    "",
                    "There was a problem with the position information."
                );

                await PrepareRosterView();

                return View("GenerateWeekly", await db.StaffPositions.ToListAsync());
            }

            // ============================================
            // CHECK STAFF BEFORE CREATING ANY SHIFTS
            // ============================================

            for (int i = 0; i < positionIds.Length; i++)
            {
                int positionId = positionIds[i];
                int requiredStaff = staffNeeded[i];

                // Ignore positions HR doesn't need
                if (requiredStaff <= 0)
                    continue;

                // Count ACTIVE staff for this branch + position
                int availableStaff = await db.Staffs
                    .CountAsync(s =>
                        s.BranchId == branchId &&
                        s.PositionId == positionId &&
                        s.StaffAvailability == IsTheStaffMemberAvailable.Active
                    );

                // Get position name for a useful error message
                var positionName = await db.StaffPositions
                    .Where(p => p.PositionId == positionId)
                    .Select(p => p.PositionName)
                    .FirstOrDefaultAsync();

                if (requiredStaff > availableStaff)
                {
                    ModelState.AddModelError(
                        $"staffNeeded[{i}]",
                        $"You requested {requiredStaff} {positionName} staff member(s), " +
                        $"but only {availableStaff} active {positionName} staff member(s) " +
                        $"are available at this branch."
                    );
                }
            }

            // ============================================
            // STOP IF ANY POSITION HAS TOO MANY STAFF
            // ============================================

            if (!ModelState.IsValid)
            {
                await PrepareRosterView();

                return View("GenerateWeekly", await db.StaffPositions.ToListAsync());
            }

            // ============================================
            // GENERATE ROSTER
            // ============================================

            int totalShiftsCreated = 0;
            int? rosterId = null;

            for (int i = 0; i < positionIds.Length; i++)
            {
                if (staffNeeded[i] <= 0)
                    continue;

                int shiftsCreated = await _rosterService.GenerateWeeklyShiftsAsync(
                    branchId,
                    positionIds[i],
                    weekStartDate,
                    startTime,
                    endTime,
                    activeDays,
                    staffNeeded[i]
                );

                totalShiftsCreated += shiftsCreated;

                if (rosterId == null)
                {
                    var roster = await db.Rosters.FirstOrDefaultAsync(r =>
                        r.BranchId == branchId &&
                        r.WeekStartDate == weekStartDate);

                    rosterId = roster?.RosterId;
                }
            }

            TempData["successful"] =
                $"{totalShiftsCreated} shifts created.";

            return RedirectToAction(
                "RosterDetails",
                new { rosterId }
            );
        }


        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> GenerateWeekly(
        //int branchId,
        //DateTime weekStartDate,
        //TimeSpan startTime,
        //TimeSpan endTime,
        //int[] positionIds,
        //int[] staffNeeded)
        //{
        //    var activeDays = new[]
        //    {
        //    DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        //    DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
        //};

        //    int totalShiftsCreated = 0;
        //    int? rosterId = null;

        //    for (int i = 0; i < positionIds.Length; i++)
        //    {
        //        if (staffNeeded[i] <= 0) continue;

        //        int shiftsCreated = await _rosterService.GenerateWeeklyShiftsAsync(
        //            branchId, positionIds[i], weekStartDate, startTime, endTime, activeDays, staffNeeded[i]);

        //        totalShiftsCreated += shiftsCreated;

        //        if (rosterId == null)
        //        {
        //            var roster = await db.Rosters.FirstOrDefaultAsync(r =>
        //                r.BranchId == branchId && r.WeekStartDate == weekStartDate);
        //            rosterId = roster?.RosterId;
        //        }
        //    }

        //    TempData["successful"] = $"{totalShiftsCreated} shifts created.";
        //    return RedirectToAction("RosterDetails", new { rosterId });
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> GenerateWeekly(
        //    int branchId,
        //    DateTime weekStartDate,
        //    TimeSpan startTime,
        //    TimeSpan endTime,
        //    string[] selectedDays,
        //    int[] positionIds,
        //    int[] staffNeeded)
        //{
        //    if (selectedDays == null || selectedDays.Length == 0)
        //    {
        //        ModelState.AddModelError("", "Select at least one day.");
        //        ViewBag.Branches = new SelectList(await db.Branches.ToListAsync(), "BranchId", "BranchName");
        //        var positions = await db.StaffPositions.OrderBy(p => p.PositionName).ToListAsync();
        //        return View(positions);
        //    }

        //    var activeDays = selectedDays.Select(d => Enum.Parse<DayOfWeek>(d)).ToArray();

        //    int totalShiftsCreated = 0;
        //    int? rosterId = null;

        //    // positionIds[] and staffNeeded[] are parallel arrays — same index = same position row
        //    for (int i = 0; i < positionIds.Length; i++)
        //    {
        //        if (staffNeeded[i] <= 0) continue; // HR left this position at 0 — skip it entirely

        //        int shiftsCreated = await _rosterService.GenerateWeeklyShiftsAsync(
        //            branchId, positionIds[i], weekStartDate, startTime, endTime, activeDays, staffNeeded[i]);

        //        totalShiftsCreated += shiftsCreated;

        //        if (rosterId == null)
        //        {
        //            var roster = await db.Rosters.FirstOrDefaultAsync(r =>
        //                r.BranchId == branchId && r.WeekStartDate == weekStartDate);
        //            rosterId = roster?.RosterId;
        //        }
        //    }

        //    TempData["successful"] = $"{totalShiftsCreated} shifts created across {positionIds.Count(_ => true)} positions.";
        //    return RedirectToAction("RosterDetails", new { rosterId });
        //}

        // HR view: full week, every shift, grouped by day
        public async Task<IActionResult> RosterDetails(int rosterId)
        {
            var roster = await db.Rosters
                .Include(r => r.Branch)
                .Include(r => r.RosterShifts).ThenInclude(s => s.Staff)
                .Include(r => r.RosterShifts).ThenInclude(s => s.Position)
                .FirstOrDefaultAsync(r => r.RosterId == rosterId);

            if (roster == null) return NotFound();
            return View(roster);
        }

        // HR view: list of all rosters ever generated
        public async Task<IActionResult> RosterList()
        {
            var rosters = await db.Rosters
                .Include(r => r.Branch)
                .OrderByDescending(r => r.WeekStartDate)
                .ToListAsync();

            return View(rosters);
        }

        //Publish roster action method
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishRoster(int rosterId)
        {
            var roster = await db.Rosters.FindAsync(rosterId);
            if (roster == null) return NotFound();

            roster.Status = RosterStatus.Published;
            await db.SaveChangesAsync();

            TempData["successful"] = "Roster published — employees can now view their shifts.";
            return RedirectToAction("RosterDetails", new { rosterId });
        }
    }
}