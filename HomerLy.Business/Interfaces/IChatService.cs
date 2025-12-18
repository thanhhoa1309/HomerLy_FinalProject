using HomerLy.BusinessObject.DTOs.ChatMessageDTOs;

namespace HomerLy.Business.Interfaces
{
    public interface IChatService
    {
        // Send message from sender to receiver (auto-determine receiver if not specified)
        Task<ChatMessageResponseDto> SendMessageAsync(Guid senderId, CreateChatMessageDto createDto);

        // Get chat history between two users
        Task<List<ChatMessageResponseDto>> GetChatHistoryAsync(Guid userId1, Guid userId2, int pageNumber = 1, int pageSize = 50);

        // Get all chat conversations for admin (list of users who chatted with admin)
        Task<List<ChatConversationDto>> GetAdminConversationsAsync(Guid adminId);

        // Get recent messages between two users
        Task<List<ChatMessageResponseDto>> GetRecentMessagesAsync(Guid userId1, Guid userId2, int count = 20);

        // Mark messages as read
        Task MarkMessagesAsReadAsync(Guid userId, Guid otherUserId);

        // Get admin account ID
        Task<Guid?> GetAdminIdAsync();
    }

    public class ChatConversationDto
    {
        public Guid UserId { get; set; }
        public required string UserName { get; set; }
        public required string UserRole { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
    }
}
