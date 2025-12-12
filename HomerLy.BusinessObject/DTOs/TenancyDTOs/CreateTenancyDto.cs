using Homerly.BusinessObject.Enums;
using System.ComponentModel.DataAnnotations;

namespace Homerly.BusinessObject.DTOs.TenancyDTOs
{
    public class CreateTenancyDto
    {
        [Required(ErrorMessage = "Property ID is required")]
        public Guid PropertyId { get; set; }

        [Required(ErrorMessage = "Tenant ID is required")]
        public Guid TenantId { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateTime EndDate { get; set; }

        public string? ContractUrl { get; set; }

        [Required(ErrorMessage = "Electric unit price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Electric unit price must be greater than or equal to 0")]
        public decimal ElectricUnitPrice { get; set; }

        [Required(ErrorMessage = "Water unit price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Water unit price must be greater than or equal to 0")]
        public decimal WaterUnitPrice { get; set; }

        [Required(ErrorMessage = "Electric old index is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Electric old index must be greater than or equal to 0")]
        public int ElectricOldIndex { get; set; }

        [Required(ErrorMessage = "Water old index is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Water old index must be greater than or equal to 0")]
        public int WaterOldIndex { get; set; }
    }
}
