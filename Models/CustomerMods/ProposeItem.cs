using SkyTech.Models;
using SkyTech.Models.Documents;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models
{
    public enum SelectCondition
    {
        New,         //0
        Used_like_New, //1
        Used,  //2
        Broken //3
    }
    public class ProposeItem
    {
        [Key]
        public int PropItemId { get; set; }

        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; }

        public int? AssignedDriverId { get; set; }
        [ForeignKey("AssignedDriverId")]
        public virtual DeliveryDriver AssignedDriver { get; set; }

        [Required]
        public string ProdName { get; set; }

        [Required]
        public SelectCategory ProdCategory { get; set; }

        [Required]
        public SelectCondition ProdCondition { get; set; }

        [Required]
        public string ProdDescription { get; set; }

        public virtual ICollection<ReceiptPic> ReceiptPics { get; set; } = new List<ReceiptPic>();
        public virtual ICollection<ProposedProdPic> ProposedProdPics { get; set; } = new List<ProposedProdPic>();

        //Proposed product pictures

        // You must add serial number in pictures that are uploaded

        public Status ProdStatus { get; set; }
        public double AmountProposed { get; set; }
        public string StoreAddress { get; set; }  
        public string CustomerAddress { get; set; }
   

        public DateTime SavedOn { get; set; }


        public DateTime? CollectionDate { get; set; }   // ← add

        public bool IsCollected { get; set; }
        public virtual ProposalCheckout Checkout { get; set; }
    }
}
