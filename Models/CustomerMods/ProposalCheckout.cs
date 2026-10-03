using SkyTech.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models
{

    public enum Delivery
    {
        Shop_Will_Collect,    //0
        Customer_Will_Bring  //1

    }
    public class ProposalCheckout
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CheckoutId { get; set; }

        public int PropItemId { get; set; }
        [ForeignKey("PropItemId")]
        public virtual ProposeItem ProposeItem { get; set; }

        public int CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; }

        public Delivery SelectDelivery { get; set; }

        [Required, Phone]
        public string PhoneNumber { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string BankName { get; set; }       // display name e.g. "Standard Bank of South Africa"

        [Required]
        public string BankCode { get; set; }        // ← ADD THIS — PayStack code e.g. "057"

        [Required]
        public string AccountHolderName { get; set; }

        [Required]
        public string AccountNumber { get; set; }

        [Required]
        public string BranchCode { get; set; }      // customer's branch code — keep for records

        public AccountType AccountType { get; set; }

        public DateTime SubmittedOn { get; set; } = DateTime.Now;
    }

    public enum AccountType
    {
        Cheque,
        Savings,
        Transmission
    }
}
