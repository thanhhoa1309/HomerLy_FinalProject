using HomerLy.DataAccess.Entities;
using System.ComponentModel.DataAnnotations;

namespace Homerly.DataAccess.Entities
{
    public class ChatMessage : BaseEntity
    {
        [Required]
        public Guid TenancyId { get; set; }
        [Required]
        public Guid SenderId { get; set; }
        [Required]
        public string Message { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;


    }
}
