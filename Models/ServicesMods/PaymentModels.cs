namespace SkyTech.Models
{
    public class PaymentRequest
    {
        public string CustomerEmail { get; set; }
        public string CustomerName { get; set; }
        public decimal Amount { get; set; }
        public string ProductName { get; set; }
        public string OrderIds { get; set; } // e.g. "12,13,14" — all orders from checkout
        public int OrderId => int.TryParse(OrderIds?.Split(',')[0], out int id) ? id : 0; // first orderId for the payment reference

    }

    public class PayStackInitResponse
    {
        public bool status { get; set; }
        public string message { get; set; }
        public PayStackInitData data { get; set; }
    }

    public class PayStackInitData
    {
        public string authorization_url { get; set; }
        public string access_code { get; set; }
        public string reference { get; set; }
    }

    public class PayStackVerifyResponse
    {
        public bool status { get; set; }
        public string message { get; set; }
        public PayStackVerifyData data { get; set; }
    }

    public class PayStackVerifyData
    {
        public string status { get; set; }
        public string reference { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public PayStackCustomer customer { get; set; }
    }

    public class PayStackCustomer
    {
        public string email { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
    }
}
