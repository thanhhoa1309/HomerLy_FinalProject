namespace Homerly.BusinessObject.DTOs.UtilityReadingDTOs
{
    public class UtilityReadingResponseDto
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        public Guid TenancyId { get; set; }
        public DateTime ReadingDate { get; set; }
        public int ElectricOldIndex { get; set; }
        public int ElectricNewIndex { get; set; }
        public int ElectricUsage { get; set; }
        public int WaterOldIndex { get; set; }
        public int WaterNewIndex { get; set; }
        public int WaterUsage { get; set; }
        public bool IsCharged { get; set; }
        public Guid CreatedById { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
