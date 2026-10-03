using SkyTech.Models;
using System.Collections.Generic;

namespace SkyTech.ViewModels
{
    public class CheckoutViewModel
    {
        public string DeliveryAddress { get; set; }
        public string OrderType { get; set; } // "delivery" or "pickup"
        public int? SelectedPickupBranchId { get; set; }
        public List<ShopOwner> AvailableBranches { get; set; }
        public double CartSubtotal { get; set; }
        public double DeliveryFee { get; set; } = 50;
        public double PickupFee { get; set; } = 25; // You can change this amount
        public double TotalAmount { get; set; }
    }
}
