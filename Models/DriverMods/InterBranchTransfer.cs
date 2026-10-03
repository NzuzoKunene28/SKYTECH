using SkyTech.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models
{
    public enum ItemTransferStatus { PendingAtSource, AssignedToDriver, InTransit, DeliveredToPickupBranch }

    public class InterBranchTransfer
    {
        [Key]
        public int TransferId { get; set; }

        public int OrderItemId { get; set; }
        [ForeignKey("OrderItemId")]
        public virtual OrderItem OrderItem { get; set; }

        public int SourceBranchId { get; set; } // Branch that has the item
        [ForeignKey("SourceBranchId")]
        public virtual ShopOwner SourceBranch { get; set; }

        public int TargetBranchId { get; set; } // Pickup branch
        [ForeignKey("TargetBranchId")]
        public virtual ShopOwner TargetBranch { get; set; }

        public int? DriverId { get; set; }
        [ForeignKey("DriverId")]
        public virtual DeliveryDriver Driver { get; set; }

        public string TransferCode { get; set; } // Unique code for driver to scan/enter
        public ItemTransferStatus TransferStatus { get; set; }

        public DateTime ScheduledTransferDate { get; set; } // 1 day after order
        public DateTime? DeliveredToTargetOn { get; set; }

        public bool IsDelivered { get; set; }
    }
}
