using Homerly.BusinessObject.Enums;
using HomerLy.DataAccess.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Homerly.DataAccess.Entities
{
    public class Tenancy : BaseEntity
    {
        [Required]
        public Guid PropertyId { get; set; }
        [Required]
        public Guid TenantId { get; set; }
        [Required]
        public Guid OwnerId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ContractUrl { get; set; }
        public TenancyStatus Status { get; set; }
        public bool IsTenantConfirmed { get; set; } = false;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ElectricUnitPrice { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal WaterUnitPrice { get; set; }

        // Chỉ số điện/nước ban đầu (khi tenant bắt đầu thuê)
        public int ElectricOldIndex { get; set; }
        public int WaterOldIndex { get; set; }

        public virtual Property Property { get; set; }

        public virtual Account Tenant { get; set; }

        public virtual Account Owner { get; set; }
    }
}
