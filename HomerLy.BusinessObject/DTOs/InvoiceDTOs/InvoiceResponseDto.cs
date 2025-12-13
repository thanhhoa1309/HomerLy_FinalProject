using HomerLy.BusinessObject.Enums;

namespace HomerLy.BusinessObject.DTOs.InvoiceDTOs
{
    public class InvoiceResponseDto
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        public Guid TenancyId { get; set; }
        public Guid TenantId { get; set; }
        public Guid OwnerId { get; set; }
        public Guid UtilityReadingId { get; set; }

        public DateTime BillingPeriodStart { get; set; }
        public DateTime BillingPeriodEnd { get; set; }
        public DateTime DueDate { get; set; }
        public InvoiceStatus Status { get; set; }
        public DateTime? PaymentDate { get; set; }

        public decimal MonthlyRentPrice { get; set; }

        public int ElectricOldIndex { get; set; }
        public int ElectricNewIndex { get; set; }
        public decimal ElectricUnitPrice { get; set; }
        public decimal ElectricCost { get; set; }

        public int WaterOldIndex { get; set; }
        public int WaterNewIndex { get; set; }
        public decimal WaterUnitPrice { get; set; }
        public decimal WaterCost { get; set; }

        public decimal OtherFees { get; set; }
        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties for display
        public string PropertyTitle { get; set; }
        public string PropertyAddress { get; set; }
        public string TenantName { get; set; }
        public string OwnerName { get; set; }
    }
}
