using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models.Repair_Item_Services
{
    public class QuotationItem
    {
        [Key]
        public int QuotationItemId { get; set; }

        [ForeignKey("QuotationId")]
        public int QuotationId { get; set; }
        public virtual Quotation Quotation { get; set; }

        [Required]
        public string ItemDescription { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal UnitCost { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal LineTotal
        {
            get { return UnitCost * Quantity; }
        }
    }
}
