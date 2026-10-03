namespace SkyTech.Models
{
    public class LeasePaymentRequest
    {
        public int LeaseAgreementId { get; set; }
        public int? LeasePaymentId { get; set; }
        public decimal Amount { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerName { get; set; }
    }
}
