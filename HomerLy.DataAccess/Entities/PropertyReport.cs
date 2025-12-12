using Homerly.BusinessObject.Enums;
using HomerLy.DataAccess.Entities;
using System.ComponentModel.DataAnnotations;

namespace Homerly.DataAccess.Entities
{
    public class PropertyReport : BaseEntity
    {
        [Required]
        public Guid PropertyId { get; set; }
        [Required]
        public Guid TenancyId { get; set; }
        [Required]
        public Guid RequestedById { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; }
        public PriorityStatus Priority { get; set; }


    }
}
