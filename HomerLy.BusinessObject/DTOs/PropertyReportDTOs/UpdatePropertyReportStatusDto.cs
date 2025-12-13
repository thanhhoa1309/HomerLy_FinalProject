using Homerly.BusinessObject.Enums;
using System.ComponentModel.DataAnnotations;

namespace HomerLy.BusinessObject.DTOs.PropertyReportDTOs
{
    public class UpdatePropertyReportStatusDto
    {
        [Required(ErrorMessage = "Priority status is required")]
        public PriorityStatus Priority { get; set; }
    }
}
