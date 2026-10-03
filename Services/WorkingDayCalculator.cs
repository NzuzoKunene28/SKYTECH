using Microsoft.EntityFrameworkCore;
using SkyTech.Models;

namespace SkyTech.Services
{
    public class WorkingDayCalculator
    {
        private readonly ApplicationDbContext _context; //need access to database because thats where we stored public holidays

        public WorkingDayCalculator(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> CalculateWorkingDaysAsync(DateTime startDate, DateTime endDate) //wangena uOlalekani with async methods
        {
            if (endDate < startDate)
            {
                return 0;
            }

            var publicHolidays = await _context.PublicHolidays
                .Where(h =>
                    h.IsActive && //only using active holidays
                    h.HolidayDate >= startDate.Date && //holiday date needs to be between employees reuested start and end date so we dont focus on old holidays
                    h.HolidayDate <= endDate.Date)
                .Select(h => h.HolidayDate.Date) //convert holiday date to list
                .ToListAsync();

            int workingDays = 0;//our counter starts at zero

            DateTime currentDate = startDate.Date;

            while (currentDate <= endDate.Date)
            {
                bool isWeekend = //either current date is true or false
                    currentDate.DayOfWeek == DayOfWeek.Saturday ||
                    currentDate.DayOfWeek == DayOfWeek.Sunday;

                bool isPublicHoliday = publicHolidays.Contains(currentDate);//checks if current date appears on public holidays we seeded

                if (!isWeekend && !isPublicHoliday)
                {
                    workingDays++;
                }

                currentDate = currentDate.AddDays(1);//move to next date if you were checkimg monday then move to Tuesday
            }

            return workingDays;//this tells you how many working days employee has requested
                               //you will be able to compare this to leave balance to see if they have enough to request leave
        }
    }
}