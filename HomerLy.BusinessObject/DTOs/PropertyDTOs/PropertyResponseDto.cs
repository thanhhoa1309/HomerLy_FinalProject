using Homerly.BusinessObject.Enums;

namespace Homerly.BusinessObject.DTOs.PropertyDTOs
{
    public class PropertyResponseDto
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal MonthlyPrice { get; set; }
        public int AreaSqm { get; set; }
        public PropertyStatus Status { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
