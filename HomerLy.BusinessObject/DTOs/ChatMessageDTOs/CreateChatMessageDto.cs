using System.ComponentModel.DataAnnotations;

namespace HomerLy.BusinessObject.DTOs.ChatMessageDTOs
{
    public class CreateChatMessageDto
    {
        // ReceiverId will be determined automatically:
        // - User/Owner -> send to Admin
        // - Admin -> send to specific user (ReceiverId required)
        public Guid? ReceiverId { get; set; }

        [Required(ErrorMessage = "Message is required")]
        [StringLength(2000, ErrorMessage = "Message cannot exceed 2000 characters")]
        public string Message { get; set; }
    }
}
