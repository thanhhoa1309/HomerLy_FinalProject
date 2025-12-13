using Homerly.BusinessObject.Enums;
using HomerLy.DataAccess.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Homerly.DataAccess.Entities
{
    public class Property : BaseEntity
    {
        [Required]
        public Guid OwnerId { get; set; }
        [Required]
        public string Title { get; set; }
        public string Description { get; set; }
        [Required]
        public string Address { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal MonthlyPrice { get; set; }
        public int AreaSqm { get; set; }
        public PropertyStatus Status { get; set; }

        public string ImageUrl { get; set; }
    }
}
