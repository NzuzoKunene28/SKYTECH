using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models
{
    public enum OrderStatus { Pending, Assigned, OutForDelivery, Delivered }
    public enum DeliveryType { HomeDelivery, InStorePickup }

    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [ForeignKey("CustomerId")]
        public int? CustomerId { get; set; }
        public virtual Customer Customers { get; set; }

        public DateTime OrderedOn { get; set; }
        public DateTime? PaymentExpiry { get; set; }
        public OrderStatus Status { get; set; }

        [ForeignKey("Driver")]
        public int? DriverId { get; set; }
        public virtual DeliveryDriver Driver { get; set; }

        [Required]
        public string DeliveryAddress { get; set; }
        public double TotalAmount { get; set; }
        public string PaymentReference { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? DeliveredOn { get; set; }
        public bool IsArchived { get; set; } = false;

        public virtual ICollection<OrderItem> OrderItems { get; set; }

        //Need to have shop owner Id to know which store order belongs to
        public int OwnerId { get; set; }
        [ForeignKey("OwnerId")]
        public virtual ShopOwner ShopOwner { get; set; }

        //Delivery pin needed to complete order
        public string DeliveryPin { get; set; }

        //The date the order will be scheduled for
        //The date the driver will be assigned for an order
        public DateTime ScheduledDeliveryDate { get; set; }
     

        public DeliveryType OrderType { get; set; } // HomeDelivery or InStorePickup
        public int? PickupOrderId { get; set; } // Link to PickupOrder if applicable

    }
}
