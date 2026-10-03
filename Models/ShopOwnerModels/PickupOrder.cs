using SkyTech.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models
{
    public enum PickupStatus { Pending, ReadyForPickup, Collected }

    public class PickupOrder
    {
        [Key]
        public int PickupOrderId { get; set; }

        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        public int PickupBranchId { get; set; } // Which branch customer will collect from
        [ForeignKey("PickupBranchId")]
        public virtual ShopOwner PickupBranch { get; set; }

        public string PickupReference { get; set; } // Code customer shows at store
        public double PickupFee { get; set; }
        public PickupStatus Status { get; set; }
        public DateTime? CollectedOn { get; set; }
        public DateTime ScheduledPickupDate { get; set; } // 2 days after order
    }
}
