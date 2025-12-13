using System.ComponentModel.DataAnnotations;

namespace Homerly.BusinessObject.DTOs.PropertyDTOs
{
    public class CreatePropertyDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Monthly price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Monthly price must be greater than or equal to 0")]
        public decimal MonthlyPrice { get; set; }

        [Required(ErrorMessage = "Area is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Area must be greater than 0")]
        public int AreaSqm { get; set; }

        public string ImageUrl { get; set; } = string.Empty;
    }
}
