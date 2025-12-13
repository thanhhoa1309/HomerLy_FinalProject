using Homerly.BusinessObject.Enums;
using System.ComponentModel.DataAnnotations;

namespace HomerLy.BusinessObject.DTOs.PropertyReportDTOs
{
    public class CreatePropertyReportDto
    {
        [Required(ErrorMessage = "Property ID is required")]
        public Guid PropertyId { get; set; }

        [Required(ErrorMessage = "Tenancy ID is required")]
        public Guid TenancyId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Priority is required")]
        public PriorityStatus Priority { get; set; }
    }
}
