using HomerLy.BusinessObject.Enums;
using System.ComponentModel.DataAnnotations;

namespace HomerLy.BusinessObject.DTOs.InvoiceDTOs
{
    public class UpdateInvoiceStatusDto
    {
        [Required]
        public InvoiceStatus Status { get; set; }

        public DateTime? PaymentDate { get; set; }
    }
}
