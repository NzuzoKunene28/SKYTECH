using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.SkytechWorkers;

namespace SkyTech.Services
{
    public class LeaveRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly WorkingDayCalculator _workingDayCalculator;

        public LeaveRequestService(
            ApplicationDbContext context,
            WorkingDayCalculator workingDayCalculator)
        {
            _context = context;
            _workingDayCalculator = workingDayCalculator;
        }


        // =========================================================
        // GET DEFAULT ALLOCATION FOR EACH LEAVE TYPE
        // =========================================================

        private int GetAllocatedDays(int leaveTypeId)
        {
            return leaveTypeId switch
            {
                1 => 15, // Annual Leave
                2 => 30, // Sick Leave
                3 => 3,  // Family Responsibility Leave
                _ => 0
            };
        }


        // =========================================================
        // FIND WHICH LEAVE CYCLE A DATE BELONGS TO
        // =========================================================

        private (DateTime CycleStart, DateTime CycleEnd)
            CalculateLeaveCycle( //trying to find out which cycle applying for
                DateTime employmentDate, //the day you were employed thats when your cycle starts
                int cycleMonths,// cycle are in months e.g sick leave is 36months
                DateTime date) //which date we want cycle for e.g 10 June 2030 which falls in cycle 2 since cycle one started in 2026 thats when you were employed
        {
            DateTime cycleStart = employmentDate.Date; //start checking from employees employment

            while (cycleStart //checking to see if another cycle has passed before the date im trying to change and if yes pass it
                .AddMonths(cycleMonths)//add the cycle months to the cycle start date e.g 1 Jan 2026+ 36mo = 1 Jan 2028
                .Date <= date.Date)// we check that 1 jan 2029 and we requested 10 June 2030 and 1 Jan 2029 is less then requested date so we have passed it
            {
                cycleStart =
                    cycleStart.AddMonths(cycleMonths);//here we start checking at 1 jan 2029 and add 36mo again that gives us till 2032
            }                                         //since cycle 2 ends at 2031 and we're looking for  june 2030 that means we are looking for cycle 2

            DateTime cycleEnd = //since cycle 2 starts at 1 Jan 2029
                cycleStart
                    .AddMonths(cycleMonths)//we get first day of next cycle
                    .AddDays(-1); //but 1 Jan 2029 is not the end date of previous cycle so we have to minus one to get proper end date

            return (cycleStart, cycleEnd);
        }


        // =========================================================
        // MAKE SURE A BALANCE EXISTS FOR A LEAVE TYPE
        // =========================================================

        private async Task<StaffLeaveBalance?>
            EnsureLeaveBalanceAsync( //these are the things we give the leave balance
                Staff staff,
                LeaveType leaveType,
                DateTime date)
        {
            // Unpaid Leave and any other leave type that does not
            // use balances should not receive a balance record.
            if (!leaveType.RequiresBalance ||
                !leaveType.CycleMonths.HasValue)
            {
                return null;
            }

            var cycle =
                CalculateLeaveCycle(
                    staff.DateEmployed,
                    leaveType.CycleMonths.Value, //we use value cos we made month nullable but we know here ithas a value
                    date);


            // Check whether this employee already has this balance
            // for this exact leave cycle.
            var existingBalance =
                await _context.StaffLeaveBalances
                    .FirstOrDefaultAsync(b =>
                        b.StaffId == staff.StaffId &&
                        b.LeaveTypeId == leaveType.LeaveTypeId &&
                        b.CycleStartDate.Date ==
                            cycle.CycleStart.Date);


            // Do not create a duplicate balance.
            if (existingBalance != null)
            {
                return existingBalance;
            }


            int allocatedDays =
                GetAllocatedDays(
                    leaveType.LeaveTypeId);


            if (allocatedDays <= 0)
            {
                return null;
            }


            var newBalance =
                new StaffLeaveBalance
                {
                    StaffId =
                        staff.StaffId,

                    LeaveTypeId =
                        leaveType.LeaveTypeId,

                    TotalAllocatedDays =
                        allocatedDays,

                    UsedDays =
                        0,

                    CycleStartDate =
                        cycle.CycleStart,

                    CycleEndDate =
                        cycle.CycleEnd
                };


            _context.StaffLeaveBalances.Add(
                newBalance);


            await _context.SaveChangesAsync();


            return newBalance;
        }


        // =========================================================
        // CREATE CURRENT BALANCES FOR DISPLAY ON REQUEST LEAVE PAGE
        // =========================================================

        public async Task
            EnsureCurrentLeaveBalancesAsync( //when loading request oage it receives employee id
                int staffId)
        {
            var staff =
                await _context.Staffs //trying to see if employee exists
                    .FirstOrDefaultAsync(
                        s => s.StaffId == staffId);


            if (staff == null) //cos we cant create balances for employees who dont exist
            {
                return;
            }


            var leaveTypes = //all the leave types that requires a balance
                await _context.LeaveTypes
                    .Where(l =>
                        l.IsActive &&
                        l.RequiresBalance)
                    .ToListAsync();


            foreach (var leaveType in leaveTypes) //we go trhough each one and check if leave balance exists cos we're creating if it doesnt
            {
                await EnsureLeaveBalanceAsync(
                    staff,
                    leaveType,
                    DateTime.Today);
            }
        }


        // =========================================================
        // VALIDATE LEAVE REQUEST
        // =========================================================

        public async Task<LeaveValidationResult> //this is our main method the controller guves us these properties and then ask if this employee is eligible for leave
            ValidateLeaveRequestAsync(
                int staffId,
                int leaveTypeId,
                DateTime startDate,
                DateTime endDate,
                bool hasSupportingDocument)
        {
            var result = //this is like our empty sheet that we will fill in later cos initially we start with no values
                new LeaveValidationResult();


            // -----------------------------------------------------
            // 1. CHECK STAFF
            // -----------------------------------------------------

            var staff =
                await _context.Staffs
                    .FirstOrDefaultAsync(
                        s => s.StaffId == staffId);


            if (staff == null)
            {
                result.Errors.Add(
                    "Staff member could not be found.");

                return result;
            }


            // -----------------------------------------------------
            // 2. STAFF MUST BE ACTIVE
            // -----------------------------------------------------

            if (staff.StaffAvailability !=
                IsTheStaffMemberAvailable.Active)
            {
                result.Errors.Add(
                    "Only active staff members can submit a leave request.");
            }


            // -----------------------------------------------------
            // 3. CHECK LEAVE TYPE
            // -----------------------------------------------------

            var leaveType =
                await _context.LeaveTypes
                    .FirstOrDefaultAsync(l =>
                        l.LeaveTypeId == leaveTypeId &&
                        l.IsActive);


            if (leaveType == null)
            {
                result.Errors.Add(
                    "The selected leave type is not available.");

                return result;
            }


            // -----------------------------------------------------
            // 4. CHECK DATES
            // -----------------------------------------------------

            if (endDate.Date <
                startDate.Date)
            {
                result.Errors.Add(
                    "The end date cannot be before the start date.");

                return result;
            }


            // -----------------------------------------------------
            // 5. CALCULATE WORKING DAYS
            // -----------------------------------------------------

            int workingDays =
                await _workingDayCalculator
                    .CalculateWorkingDaysAsync(
                        startDate,
                        endDate);


            result.WorkingDaysRequested =
                workingDays;


            if (workingDays <= 0)
            {
                result.Errors.Add( //maybe employee might have selected only the public holiday
                    "The selected dates do not contain any working days.");
            }


            // -----------------------------------------------------
            // 6. CHECK MINIMUM EMPLOYMENT PERIOD
            // -----------------------------------------------------

            if (leaveType.MinimumEmploymentMonths > 0)
            {
                var eligibleFrom =
                    staff.DateEmployed
                        .AddMonths(
                            leaveType.MinimumEmploymentMonths);


                if (DateTime.Today <
                    eligibleFrom.Date)
                {
                    result.Errors.Add(
                        $"You must be employed for at least " +
                        $"{leaveType.MinimumEmploymentMonths} month(s) " +
                        $"before requesting " +
                        $"{leaveType.LeaveTypeName}.");
                }
            }


            // -----------------------------------------------------
            // 7. CHECK MINIMUM NOTICE
            // -----------------------------------------------------

            if (leaveType.MinimumNoticeDays > 0)
            {
                var minimumStartDate =
                    DateTime.Today.AddDays(
                        leaveType.MinimumNoticeDays);


                if (startDate.Date <
                    minimumStartDate.Date)
                {
                    result.Errors.Add(
                        $"{leaveType.LeaveTypeName} requires at least " +
                        $"{leaveType.MinimumNoticeDays} day(s) notice.");
                }
            }


            // -----------------------------------------------------
            // 8. CHECK MAXIMUM CONSECUTIVE DAYS
            // -----------------------------------------------------

            if (leaveType.MaximumConsecutiveDays.HasValue &&
                workingDays >
                leaveType.MaximumConsecutiveDays.Value)
            {
                result.Errors.Add(
                    $"You may request a maximum of " +
                    $"{leaveType.MaximumConsecutiveDays.Value} working day(s) " +
                    $"at a time for " +
                    $"{leaveType.LeaveTypeName}.");
            }


            // -----------------------------------------------------
            // 9. CHECK OVERLAPPING LEAVE
            // -----------------------------------------------------

            bool hasOverlap =
                await _context.LeaveRequests
                    .AnyAsync(l =>
                        l.StaffId == staffId &&
                        (
                            l.Status ==
                                LeaveRequestStatus.Pending ||
                            l.Status ==
                                LeaveRequestStatus.Approved
                        ) &&
                        l.StartDate.Date <=
                            endDate.Date &&
                        l.EndDate.Date >=
                            startDate.Date);


            if (hasOverlap)
            {
                result.Errors.Add(
                    "You already have a pending or approved leave request " +
                    "that overlaps with these dates.");
            }


            // -----------------------------------------------------
            // 10. CHECK SUPPORTING DOCUMENT
            // -----------------------------------------------------

            if (leaveType.RequiresDocument &&
                !hasSupportingDocument)
            {
                result.Errors.Add(
                    $"{leaveType.LeaveTypeName} requires a supporting document.");
            }


            // -----------------------------------------------------
            // 11. CHECK LEAVE BALANCE
            // -----------------------------------------------------

            if (leaveType.RequiresBalance)
            {
                // Make sure the employee has a balance for the
                // cycle containing the requested start date.
                var balance =
                    await EnsureLeaveBalanceAsync(
                        staff,
                        leaveType,
                        startDate);


                if (balance == null)
                {
                    result.Errors.Add(
                        $"No active {leaveType.LeaveTypeName} balance " +
                        $"was found for this staff member.");
                }
                else
                {
                    // Pending leave temporarily reserves days.
                    int pendingDays =
                        await _context.LeaveRequests
                            .Where(l =>
                                l.StaffId == staffId &&
                                l.LeaveTypeId ==
                                    leaveTypeId &&
                                l.Status ==
                                    LeaveRequestStatus.Pending)
                            .SumAsync(l =>
                                (int?)l.WorkingDaysRequested)
                            ?? 0;


                    int availableDays =
                        balance.TotalAllocatedDays
                        - balance.UsedDays
                        - pendingDays;


                    result.AvailableDays =
                        availableDays;


                    result.RemainingDaysAfterRequest =
                        availableDays
                        - workingDays;


                    if (workingDays >
                        availableDays)
                    {
                        result.Errors.Add(
                            $"You only have {availableDays} day(s) available " +
                            $"for {leaveType.LeaveTypeName}, but you requested " +
                            $"{workingDays} day(s).");
                    }
                }
            }


            result.IsEligible =
                !result.Errors.Any();


            return result;
        }
    }


    // =============================================================
    // VALIDATION RESULT
    // =============================================================

    public class LeaveValidationResult //not a model just object to tell controller more than just the employee qualifies
    {
        public bool IsEligible { get; set; }

        public int WorkingDaysRequested { get; set; }

        public int? AvailableDays { get; set; }

        public int? RemainingDaysAfterRequest { get; set; }

        public List<string> Errors { get; set; } =
            new();
    }
}