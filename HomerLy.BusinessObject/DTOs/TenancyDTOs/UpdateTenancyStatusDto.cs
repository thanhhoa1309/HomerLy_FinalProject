using Homerly.BusinessObject.Enums;
using System.ComponentModel.DataAnnotations;

namespace Homerly.BusinessObject.DTOs.TenancyDTOs
{
    public class UpdateTenancyStatusDto
    {
        [Required(ErrorMessage = "Status is required")]
        public TenancyStatus Status { get; set; }
    }
}
