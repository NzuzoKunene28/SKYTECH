using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace SkyTech.Models.SkytechWorkers
{
    public class Department
    {
        [Key]
        public int DepartmentId { get; set; }

        public string DepartmentName { get; set; }
        public bool IsActive { get; set; } = true;

        // Minimum number of employees that must remain
        // available while other employees are on leave
        public int MinimumStaffRequired { get; set; } = 1;

        [ValidateNever]
        public virtual ICollection<StaffPosition> StaffPositions { get; set; }
    }
}
