using HomerLy.DataAccess.Entities;
using System.ComponentModel.DataAnnotations;

namespace Homerly.DataAccess.Entities
{
    public class UtilityReading : BaseEntity
    {
        [Required]
        public Guid PropertyId { get; set; }
        [Required]
        public Guid TenancyId { get; set; }
        public DateTime ReadingDate { get; set; }
        public int ElectricOldIndex { get; set; }
        public int ElectricNewIndex { get; set; }
        public int WaterOldIndex { get; set; }
        public int WaterNewIndex { get; set; }
        public bool IsCharged { get; set; } = false;
        public Guid CreatedById { get; set; }

    }
}
