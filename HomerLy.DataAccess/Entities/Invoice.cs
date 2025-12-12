using HomerLy.BusinessObject.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomerLy.DataAccess.Entities
{
    public class Invoice : BaseEntity
    {

        [Required]
        public Guid PropertyId { get; set; }
        [Required]
        public Guid TenancyId { get; set; }
        [Required]
        public Guid TenantId { get; set; }
        [Required]
        public Guid OwnerId { get; set; }

        public Guid UtilityReadingId { get; set; }

        public DateTime BillingPeriodStart { get; set; }
        public DateTime BillingPeriodEnd { get; set; }
        public DateTime DueDate { get; set; }
        public InvoiceStatus Status { get; set; }
        public DateTime? PaymentDate { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal MonthlyRentPrice { get; set; }


        public int ElectricOldIndex { get; set; }
        public int ElectricNewIndex { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal ElectricUnitPrice { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal ElectricCost { get; set; }


        public int WaterOldIndex { get; set; }
        public int WaterNewIndex { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal WaterUnitPrice { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal WaterCost { get; set; }


        [Column(TypeName = "decimal(18, 2)")]
        public decimal OtherFees { get; set; } = 0;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }



    }
}
