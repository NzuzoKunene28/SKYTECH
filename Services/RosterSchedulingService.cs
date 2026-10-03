// Services/RosterSchedulingService.cs
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.SkytechWorkers;

namespace SkyTech.Services
{
    public class RosterSchedulingService : IRosterSchedulingService
    {
        private readonly ApplicationDbContext db;

        public RosterSchedulingService(ApplicationDbContext db)
        {
            this.db = db;
        }

        public async Task<int> GenerateWeeklyShiftsAsync(
            int branchId,
            int positionId,
            DateTime weekStartDate,
            TimeSpan startTime,
            TimeSpan endTime,
            DayOfWeek[] activeDays,
            int staffNeededPerDay)
        {
            var weekEndDate = weekStartDate.AddDays(6);

            // Prevent duplicate rosters for the same branch + week
            var roster = await db.Rosters.FirstOrDefaultAsync(r =>
                r.BranchId == branchId && r.WeekStartDate == weekStartDate);

            if (roster == null)
            {
                roster = new Roster
                {
                    BranchId = branchId,
                    WeekStartDate = weekStartDate,
                    WeekEndDate = weekEndDate,
                    Status = RosterStatus.Draft
                };
                db.Add(roster);
                await db.SaveChangesAsync();
            }

            // Get (or initialize) the rotation queue for this branch+position
            var rotationEntries = await db.StaffRotationOrders
                .Where(o => o.BranchId == branchId && o.PositionId == positionId)
                .OrderBy(o => o.QueuePosition)
                .ToListAsync();

            var eligibleStaffIds = await db.Staffs
                .Where(s => s.BranchId == branchId &&
                            s.PositionId == positionId &&
                            s.StaffAvailability == IsTheStaffMemberAvailable.Active)
                .Select(s => s.StaffId)
                .ToListAsync();

            // Add any staff who don't have a rotation entry yet (new hires) to the back of the queue
            var missingStaffIds = eligibleStaffIds.Except(rotationEntries.Select(r => r.StaffId)).ToList();
            int nextQueuePos = rotationEntries.Any() ? rotationEntries.Max(r => r.QueuePosition) + 1 : 0;
            foreach (var staffId in missingStaffIds)
            {
                var entry = new StaffRotationOrder
                {
                    BranchId = branchId,
                    PositionId = positionId,
                    StaffId = staffId,
                    QueuePosition = nextQueuePos++
                };
                db.Add(entry);
                rotationEntries.Add(entry);
            }

            // Remove rotation entries for staff no longer eligible (left, transferred, suspended)
            rotationEntries = rotationEntries.Where(r => eligibleStaffIds.Contains(r.StaffId)).ToList();

            await db.SaveChangesAsync();

            if (!rotationEntries.Any())
                return 0; // nobody eligible — nothing to schedule

            // Build the working queue in current rotation order
            var queue = rotationEntries.OrderBy(r => r.QueuePosition).ToList();

            var hoursWorkedThisWeek = rotationEntries.ToDictionary(r => r.StaffId, r => 0.0);
            int shiftsCreated = 0;
            var shiftHours = (endTime - startTime).TotalHours;

            for (int offset = 0; offset < 7; offset++)
            {
                var date = weekStartDate.AddDays(offset);
                if (!activeDays.Contains(date.DayOfWeek)) continue;

                int countForToday = Math.Min(staffNeededPerDay, queue.Count);
                var todayAssigned = queue.Take(countForToday).ToList();

                foreach (var entry in todayAssigned)
                {
                    db.Add(new RosterShift
                    {
                        RosterId = roster.RosterId,
                        StaffId = entry.StaffId,
                        WorkDate = date,
                        StartTime = startTime,
                        EndTime = endTime,
                        PositionId = positionId,
                        BranchId = branchId,
                        Status = RosterShiftStatus.Scheduled
                    });

                    hoursWorkedThisWeek[entry.StaffId] += shiftHours;
                    shiftsCreated++;

                    // move this person to the back of the queue
                    queue.Remove(entry);
                    queue.Add(entry);
                }
            }

            await db.SaveChangesAsync();

            // End of week: whoever worked the LEAST goes to the front for next time
            var orderedByHoursAscending = rotationEntries
                .OrderBy(r => hoursWorkedThisWeek[r.StaffId])
                .ToList();

            for (int i = 0; i < orderedByHoursAscending.Count; i++)
                orderedByHoursAscending[i].QueuePosition = i;

            await db.SaveChangesAsync();

            return shiftsCreated;
        }
    }
}