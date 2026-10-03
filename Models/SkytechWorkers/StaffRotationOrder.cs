using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SkyTech.Models.SkytechWorkers
{
    public class StaffRotationOrder
    {
        [Key]
        public int StaffRotationOrderId { get; set; }

        [ForeignKey(nameof(Branch))]
        public int BranchId { get; set; }
        [ValidateNever]
        public virtual Branch Branch { get; set; }

        [ForeignKey(nameof(Position))]
        public int PositionId { get; set; }
        [ValidateNever]
        public virtual StaffPosition Position { get; set; }

        [ForeignKey(nameof(Staff))]
        public int StaffId { get; set; }
        [ValidateNever]
        public virtual Staff Staff { get; set; }

        public int QueuePosition { get; set; }
    }
}