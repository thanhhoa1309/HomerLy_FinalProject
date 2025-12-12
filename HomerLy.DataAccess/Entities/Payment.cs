using HomerLy.DataAccess.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Homerly.DataAccess.Entities
{
    public class Payment : BaseEntity
    {
        [Required]
        public Guid PropertyId { get; set; }
        [Required]
        public Guid TenancyId { get; set; }
        [Required]
        public Guid PayerId { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }
        public string PaymentFor { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; }
        public bool IsPaid { get; set; } = false;

    }
}
