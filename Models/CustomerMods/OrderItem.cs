using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models
{
    public enum ReturnProduct
    {
        No_Request,
        Return_Pending,
        Return_Approved,
        Collection_Assigned,   // driver assigned to collect
        Item_Collected,        // item picked up, being inspected
        Exchange_Dispatched,   // exchange item sent back to customer
        Completed              // done

    }

    public enum GetRefund
    {
        No_Requests,
        Refund,
        Exchange_item
    }

    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }
        public int ProdId { get; set; }
        [ForeignKey("ProdId")]
        public virtual AddProduct Products { get; set; }
        public string ProdName { get; set; }
        public int Quantity { get; set; }
        public double PriceAtTimeOfPurchase { get; set; }
        //handling returns
        public ReturnProduct ReturnPStatus { get; set; }   //whether it is approved or rejected
        public string RetunReason { get; set; }            
        public int QuantityReturned { get; set; }         
        public GetRefund RefundOrExchange { get; set; }    //Choose exchange

        public bool IsAssingedForCollection { get; set; }  // wherether driver is assigned for colllection
        public bool IsAssingedForSecDelivery { get; set; } // wherether driver is assined for second delivery

        //drivers who will collect and re deliver
        // Collection driver (picks up from customer)
        public int? DriverId { get; set; }
        [ForeignKey("DriverId")]
        public virtual DeliveryDriver DeliveryDriver { get; set; }  // driver for collection

        public DateTime? CollectionDate { get; set; }
        public DateTime? NewDeliveryDate { get; set; }

   

    }
}