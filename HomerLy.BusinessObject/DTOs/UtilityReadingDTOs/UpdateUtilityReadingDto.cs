using System.ComponentModel.DataAnnotations;

namespace Homerly.BusinessObject.DTOs.UtilityReadingDTOs
{
    public class UpdateUtilityReadingDto
    {
        [Required(ErrorMessage = "Electric new index is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Electric new index must be greater than or equal to 0")]
        public int ElectricNewIndex { get; set; }

        [Required(ErrorMessage = "Water new index is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Water new index must be greater than or equal to 0")]
        public int WaterNewIndex { get; set; }

        public DateTime? ReadingDate { get; set; }
    }
}
