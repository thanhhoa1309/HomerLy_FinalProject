using System.ComponentModel.DataAnnotations;

namespace HomerLy.BusinessObject.DTOs.InvoiceDTOs
{
    public class CreateInvoiceDto
    {
        [Required]
        public Guid TenancyId { get; set; }

        [Required]
        public DateTime BillingPeriodStart { get; set; }

        [Required]
        public DateTime BillingPeriodEnd { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        public int ElectricNewIndex { get; set; }

        [Required]
        public int WaterNewIndex { get; set; }

        [Required]
        public decimal ElectricUnitPrice { get; set; }

        [Required]
        public decimal WaterUnitPrice { get; set; }

        public decimal OtherFees { get; set; } = 0;
    }
}
