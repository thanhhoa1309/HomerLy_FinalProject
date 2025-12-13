using Homerly.BusinessObject.Enums;
using System.ComponentModel.DataAnnotations;

namespace HomerLy.BusinessObject.DTOs.PropertyReportDTOs
{
    public class UpdatePropertyReportDto
    {
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string? Title { get; set; }

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string? Description { get; set; }

        public PriorityStatus? Priority { get; set; }
    }
}
