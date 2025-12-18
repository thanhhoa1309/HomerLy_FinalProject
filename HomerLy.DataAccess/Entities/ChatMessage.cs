using HomerLy.DataAccess.Entities;
using System.ComponentModel.DataAnnotations;

namespace Homerly.DataAccess.Entities
{
    public class ChatMessage : BaseEntity
    {
        [Required]
        public Guid SenderId { get; set; }

        [Required]
        public Guid ReceiverId { get; set; }

        [Required]
        public required string Message { get; set; }

        public DateTime SentAt { get; set; }

        public bool IsRead { get; set; } = false;
    }
}
