namespace HomerLy.BusinessObject.DTOs.PaymentDTOs
{
    public class StripeCheckoutSessionDto
    {
        public string SessionId { get; set; }
        public string CheckoutUrl { get; set; }
        public Guid PaymentId { get; set; }
    }
}
