namespace HomerLy.BusinessObject.DTOs.ChatMessageDTOs
{
    public class ChatMessageResponseDto
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public string SenderName { get; set; }
        public string SenderRole { get; set; }
        public Guid ReceiverId { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverRole { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
