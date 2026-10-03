using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkyTech.Models.Repair_Services;

namespace SkyTech.Models.Repair_Item_Services
{
    public enum QuotationStatus { Pending, Approved, Rejected, Expired }

    public class Quotation
    {
        [Key]
        public int QuotationId { get; set; }

        [ForeignKey("RepairRequestId")]
        public int RepairRequestId { get; set; }
        public virtual Repair_Request RepairRequest { get; set; }

        [ForeignKey("TechnicianId")]
        public int? TechnicianId { get; set; }
        public virtual Technician Technician { get; set; }

        public DateTime CreatedDate { get; set; }

        public QuotationStatus Status { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal TotalCost { get; set; }

        public string TechnicianNotes { get; set; }

        public ICollection<QuotationItem> QuotationItems { get; set; }
    }
}
