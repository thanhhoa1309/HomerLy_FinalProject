namespace HomerLy.BusinessObject.DTOs.ChatMessageDTOs
{
    public class ChatMessageResponseDto
    {
        public Guid Id { get; set; }
        public Guid TenancyId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderName { get; set; }
        public string SenderRole { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
