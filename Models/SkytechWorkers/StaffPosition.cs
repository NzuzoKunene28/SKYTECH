using DocumentFormat.OpenXml.Drawing;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models.SkytechWorkers
{
    public class StaffPosition
    {
        [Key]
        public int PositionId { get; set; }

        public string PositionName {  get; set; }

        public double HourlyRate { get; set; } //added

        [ForeignKey(nameof(Departments))]
        public int DepartmentId { get; set; }
        public virtual Department Departments { get; set; }
        [ValidateNever]

        public virtual ICollection<Staff> Staffs { get; set; }
    }
}
