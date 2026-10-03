using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models
{
    public class Cart
    {
        [Key]
        public int CartId { get; set; }
        public int CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public virtual Customer Customers { get; set; }
        public DateTime DateCreated { get; set; }
        public bool IsCheckedOut { get; set; }
        public virtual ICollection<CartItem> CartItems { get; set; }
    }
}