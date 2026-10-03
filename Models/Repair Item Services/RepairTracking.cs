using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models.Repair_Item_Services
{
    public class RepairTracking
    {
        [Key]
        public int TrackingId { get; set; }

        [ForeignKey("RepairRequestId")]
        public int RepairRequestId { get; set; }
        public virtual Repair_Request RepairRequest { get; set; }

        public RepairStatus Status { get; set; }

        public DateTime UpdatedDate { get; set; }

        [MaxLength(500)]
        public string StatusMessage { get; set; }
    }
}
