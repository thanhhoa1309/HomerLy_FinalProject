using HomerLy.Business.Interfaces;
using HomerLy.BusinessObject.DTOs.ChatMessageDTOs;
using Homerly.BusinessObject.Enums;
using HomerLy.DataAccess.Interfaces;
using Homerly.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomerLy.Business.Service
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChatService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ChatMessageResponseDto> SendMessageAsync(Guid senderId, CreateChatMessageDto createDto)
        {
            // Verify user has access to this tenancy chat
            var hasAccess = await CanUserAccessChatAsync(senderId, createDto.TenancyId);
            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("You don't have access to this chat");
            }

            // Get sender information
            var sender = await _unitOfWork.Account.GetByIdAsync(senderId);

            if (sender == null || sender.IsDeleted)
            {
                throw new Exception("Sender not found");
            }

            // Create chat message
            var chatMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                TenancyId = createDto.TenancyId,
                SenderId = senderId,
                Message = createDto.Message,
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = senderId,
                IsDeleted = false
            };

            await _unitOfWork.ChatMessage.AddAsync(chatMessage);
            await _unitOfWork.SaveChangesAsync();

            // Map to response DTO
            return new ChatMessageResponseDto
            {
                Id = chatMessage.Id,
                TenancyId = chatMessage.TenancyId,
                SenderId = chatMessage.SenderId,
                SenderName = sender.FullName,
                SenderRole = sender.Role.ToString(),
                Message = chatMessage.Message,
                SentAt = chatMessage.SentAt,
                CreatedAt = chatMessage.CreatedAt
            };
        }

        public async Task<List<ChatMessageResponseDto>> GetChatHistoryAsync(Guid tenancyId, int pageNumber = 1, int pageSize = 50)
        {
            var messages = await _unitOfWork.ChatMessage
                .GetQueryable()
                .Where(m => m.TenancyId == tenancyId && !m.IsDeleted)
                .OrderByDescending(m => m.SentAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get all sender IDs
            var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();
            var senders = await _unitOfWork.Account
                .GetQueryable()
                .Where(a => senderIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a);

            return messages.Select(m => new ChatMessageResponseDto
            {
                Id = m.Id,
                TenancyId = m.TenancyId,
                SenderId = m.SenderId,
                SenderName = senders.ContainsKey(m.SenderId) ? senders[m.SenderId].FullName : "Unknown",
                SenderRole = senders.ContainsKey(m.SenderId) ? senders[m.SenderId].Role.ToString() : "Unknown",
                Message = m.Message,
                SentAt = m.SentAt,
                CreatedAt = m.CreatedAt
            }).OrderBy(m => m.SentAt).ToList();
        }

        public async Task<List<ChatMessageResponseDto>> GetRecentMessagesAsync(Guid tenancyId, int count = 20)
        {
            var messages = await _unitOfWork.ChatMessage
                .GetQueryable()
                .Where(m => m.TenancyId == tenancyId && !m.IsDeleted)
                .OrderByDescending(m => m.SentAt)
                .Take(count)
                .ToListAsync();

            // Get all sender IDs
            var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();
            var senders = await _unitOfWork.Account
                .GetQueryable()
                .Where(a => senderIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a);

            return messages.Select(m => new ChatMessageResponseDto
            {
                Id = m.Id,
                TenancyId = m.TenancyId,
                SenderId = m.SenderId,
                SenderName = senders.ContainsKey(m.SenderId) ? senders[m.SenderId].FullName : "Unknown",
                SenderRole = senders.ContainsKey(m.SenderId) ? senders[m.SenderId].Role.ToString() : "Unknown",
                Message = m.Message,
                SentAt = m.SentAt,
                CreatedAt = m.CreatedAt
            }).OrderBy(m => m.SentAt).ToList();
        }

        public async Task<bool> CanUserAccessChatAsync(Guid userId, Guid tenancyId)
        {
            var user = await _unitOfWork.Account.GetByIdAsync(userId);

            if (user == null || user.IsDeleted)
            {
                return false;
            }

            // Admin can access all chats
            if (user.Role == RoleType.Admin)
            {
                return true;
            }

            // Get tenancy details
            var tenancy = await _unitOfWork.Tenancy
                .GetQueryable()
                .Include(t => t.Property)
                .FirstOrDefaultAsync(t => t.Id == tenancyId && !t.IsDeleted);

            if (tenancy == null)
            {
                return false;
            }

            // Owner can access if they own the property
            if (user.Role == RoleType.Owner && tenancy.Property.OwnerId == userId)
            {
                return true;
            }

            // Tenant can access if they are part of the tenancy
            if (user.Role == RoleType.User && tenancy.TenantId == userId)
            {
                return true;
            }

            return false;
        }

        public async Task MarkMessagesAsReadAsync(Guid userId, Guid tenancyId)
        {
            // This can be extended to track read status if needed
            // For now, it's a placeholder for future implementation
            await Task.CompletedTask;
        }
    }
}
