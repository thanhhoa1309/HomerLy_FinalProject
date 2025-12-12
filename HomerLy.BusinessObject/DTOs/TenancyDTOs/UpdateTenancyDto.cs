using Homerly.BusinessObject.Enums;
using System.ComponentModel.DataAnnotations;

namespace Homerly.BusinessObject.DTOs.TenancyDTOs
{
    public class UpdateTenancyDto
    {
        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string? ContractUrl { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Electric unit price must be greater than or equal to 0")]
        public decimal? ElectricUnitPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Water unit price must be greater than or equal to 0")]
        public decimal? WaterUnitPrice { get; set; }
    }
}
