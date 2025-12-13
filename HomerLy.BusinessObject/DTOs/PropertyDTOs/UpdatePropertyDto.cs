using System.ComponentModel.DataAnnotations;

namespace Homerly.BusinessObject.DTOs.PropertyDTOs
{
    public class UpdatePropertyDto
    {
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string? Title { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string? Address { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Monthly price must be greater than or equal to 0")]
        public decimal? MonthlyPrice { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Area must be greater than 0")]
        public int? AreaSqm { get; set; }

        public string? ImageUrl { get; set; }
    }
}
