// Services/IRosterSchedulingService.cs
namespace SkyTech.Services
{
    public interface IRosterSchedulingService
    {
        Task<int> GenerateWeeklyShiftsAsync(
            int branchId,
            int positionId,
            DateTime weekStartDate,
            TimeSpan startTime,
            TimeSpan endTime,
            DayOfWeek[] activeDays,
            int staffNeededPerDay);
    }
}