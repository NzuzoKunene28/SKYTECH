using System.ComponentModel.DataAnnotations;

namespace SkyTech.Models.SkytechWorkers
{
    public class LeaveType
    {
        [Key]
        public int LeaveTypeId { get; set; }

        [Required]
        [MaxLength(100)]
        public string LeaveTypeName { get; set; } = string.Empty;

        public bool RequiresDocument { get; set; }

        public int MinimumNoticeDays { get; set; }

        //can be null
        public int? MaximumConsecutiveDays { get; set; }

        public bool IsPaid { get; set; }

        //to check if for this leave type you must check available leave balance
        public bool RequiresBalance { get; set; }

        //the period in which a leave balance belongs you 
        //example- 1jan 2026-1 jan 2028 can be one leave cycle for an employee
        //if we allocate 30 days for this cycle it means the employee has 30 days for sick leave for 3 years
        //it does not mean every year they get 30 days sick leave balance
        public int? CycleMonths { get; set; }

        public int MinimumEmploymentMonths { get; set; }

        //SkyTech worker can enable or diable this leave type
        public bool IsActive { get; set; } = true;
    }
}