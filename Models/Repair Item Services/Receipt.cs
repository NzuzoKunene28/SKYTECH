using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkyTech.Models.Repair_Services;

namespace SkyTech.Models.Repair_Item_Services
{
    public class Receipt
    {
        [Key]
        public int ReceiptId { get; set; }

        [ForeignKey("RepairRequestId")]
        public int RepairRequestId { get; set; }
        public virtual Repair_Request RepairRequest { get; set; }

        [ForeignKey("QuotationId")]
        public int? QuotationId { get; set; }
        public virtual Quotation Quotation { get; set; }

        [ForeignKey("TechnicianId")]
        public int? TechnicianId { get; set; }
        public virtual Technician Technician { get; set; }

        public DateTime IssuedDate { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal FinalAmount { get; set; }

        public string ReceiptNumber { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; }
    }
}
