using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models
{
    public class GuestCartItem
    {
        public int ProdId { get; set; }
        public string ProdName { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }
    }
}
