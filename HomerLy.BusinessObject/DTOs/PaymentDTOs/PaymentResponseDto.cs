namespace HomerLy.BusinessObject.DTOs.PaymentDTOs
{
    public class PaymentResponseDto
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        public Guid TenancyId { get; set; }
        public Guid PayerId { get; set; }
        public Guid? InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentFor { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; }
        public bool IsPaid { get; set; }
        
        // Stripe info
        public string? StripeSessionId { get; set; }
        public string? StripePaymentIntentId { get; set; }
        public string? StripeChargeId { get; set; }
        
        // Navigation properties
        public string? PayerName { get; set; }
        public string? PropertyTitle { get; set; }
        public string? InvoiceNumber { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
