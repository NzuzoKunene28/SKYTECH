using SkyTech.Models.Documents;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models
{
    public enum SelectCategory
    {
        Electronics, Cellphones_and_Accessories, Computers_and_Office,
        Appliances, Furniture, Musical_Instruments, Household_Essentials,
        Tools_and_DIY, Sports_and_Leisure, Gaming
    }

    public enum ProdStatus
    {
        In_Stock,    // 0
        Low_stock,   // 1
        Out_Of_Stock // 2
    }

    public enum SelectType
    {
        Pre_Owned, //0
        New, //1
        Refurbished
    }

    public class AddProduct
    {
        [Key]
        public int ProdId { get; set; }
        public int OwnerId { get; set; }
        [ForeignKey("OwnerId")]
        public virtual ShopOwner Owner { get; set; }

        [Required] public string ProdName { get; set; }
        [Required] public SelectCategory ProdCategory { get; set; }
        [Required] public string ProdDescription { get; set; }
        [Required] public double ProdPrice { get; set; }
        [Required] public string ProdSize { get; set; }
        [Required] public int ProdQuantity { get; set; }
        [Required] public SelectType ProdType { get; set; }

        public virtual ICollection<ProductPicture> ProductPictures { get; set; } = new List<ProductPicture>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

        public ProdStatus Status { get; set; }
    }
}

