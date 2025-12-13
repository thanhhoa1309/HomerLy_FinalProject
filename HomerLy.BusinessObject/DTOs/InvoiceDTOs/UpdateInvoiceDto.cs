using System.ComponentModel.DataAnnotations;

namespace HomerLy.BusinessObject.DTOs.InvoiceDTOs
{
    public class UpdateInvoiceDto
    {
        public DateTime? BillingPeriodStart { get; set; }
        public DateTime? BillingPeriodEnd { get; set; }
        public DateTime? DueDate { get; set; }
        public int? ElectricNewIndex { get; set; }
        public int? WaterNewIndex { get; set; }
        public decimal? ElectricUnitPrice { get; set; }
        public decimal? WaterUnitPrice { get; set; }
        public decimal? OtherFees { get; set; }
    }
}
