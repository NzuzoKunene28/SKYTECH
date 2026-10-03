using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkyTech.Models;

namespace SkyTech.Models.SkytechWorkers
{
    public class StaffLeaveBalance
    {
        [Key]
        public int StaffLeaveBalanceId { get; set; }

        [ForeignKey(nameof(Staff))]
        public int StaffId { get; set; }

        [ValidateNever]
        public virtual Staff Staff { get; set; }

        [ForeignKey(nameof(LeaveType))]
        public int LeaveTypeId { get; set; }

        [ValidateNever]
        public virtual LeaveType LeaveType { get; set; }

        public int TotalAllocatedDays { get; set; }

        public int UsedDays { get; set; }

        //days leave balance belongs to you
        public DateTime CycleStartDate { get; set; }

        public DateTime CycleEndDate { get; set; }

        //not mapped because we dont need a column fir it we can calculate whenever it is needed
        [NotMapped]
        public int RemainingDays => TotalAllocatedDays - UsedDays;
    }
}