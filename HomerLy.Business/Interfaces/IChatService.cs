using HomerLy.BusinessObject.DTOs.ChatMessageDTOs;

namespace HomerLy.Business.Interfaces
{
    public interface IChatService
    {
        Task<ChatMessageResponseDto> SendMessageAsync(Guid senderId, CreateChatMessageDto createDto);
        Task<List<ChatMessageResponseDto>> GetChatHistoryAsync(Guid tenancyId, int pageNumber = 1, int pageSize = 50);
        Task<bool> CanUserAccessChatAsync(Guid userId, Guid tenancyId);
        Task<List<ChatMessageResponseDto>> GetRecentMessagesAsync(Guid tenancyId, int count = 20);
        Task MarkMessagesAsReadAsync(Guid userId, Guid tenancyId);
    }
}
