using Homerly.BusinessObject.Enums;

namespace Homerly.BusinessObject.DTOs.TenancyDTOs
{
    public class TenancyResponseDto
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        public string PropertyTitle { get; set; } = string.Empty;
        public string PropertyAddress { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string TenantEmail { get; set; } = string.Empty;
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ContractUrl { get; set; } = string.Empty;
        public TenancyStatus Status { get; set; }
        public bool IsTenantConfirmed { get; set; }
        public decimal ElectricUnitPrice { get; set; }
        public decimal WaterUnitPrice { get; set; }
        
        public int ElectricOldIndex { get; set; }
        public int WaterOldIndex { get; set; }
        
        public int? LatestElectricIndex { get; set; }
        public int? LatestWaterIndex { get; set; }
        public DateTime? LatestReadingDate { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
