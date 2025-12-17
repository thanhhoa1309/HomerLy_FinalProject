using System.ComponentModel.DataAnnotations;

namespace HomerLy.BusinessObject.DTOs.ChatMessageDTOs
{
    public class CreateChatMessageDto
    {
        [Required(ErrorMessage = "TenancyId is required")]
        public Guid TenancyId { get; set; }

        [Required(ErrorMessage = "Message is required")]
        [StringLength(2000, ErrorMessage = "Message cannot exceed 2000 characters")]
        public string Message { get; set; }
    }
}
