using Homerly.BusinessObject.Enums;

namespace HomerLy.BusinessObject.DTOs.PropertyReportDTOs
{
    public class PropertyReportResponseDto
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        public Guid TenancyId { get; set; }
        public Guid RequestedById { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public PriorityStatus Priority { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }

        // Navigation properties for display
        public string PropertyTitle { get; set; } = string.Empty;
        public string PropertyAddress { get; set; } = string.Empty;
        public string RequestedByName { get; set; } = string.Empty;
        public string RequestedByEmail { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
    }
}
