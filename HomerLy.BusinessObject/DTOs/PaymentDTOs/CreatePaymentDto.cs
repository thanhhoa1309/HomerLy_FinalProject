using System.ComponentModel.DataAnnotations;

namespace HomerLy.BusinessObject.DTOs.PaymentDTOs
{
    public class CreatePaymentDto
    {
        [Required]
        public Guid InvoiceId { get; set; }

        public string? CancelUrl { get; set; }
        public string? SuccessUrl { get; set; }
    }
}
