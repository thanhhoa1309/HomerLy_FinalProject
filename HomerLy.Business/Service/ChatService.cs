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

        public async Task<Guid?> GetAdminIdAsync()
        {
            var admin = await _unitOfWork.Account
                .GetQueryable()
                .FirstOrDefaultAsync(a => a.Role == RoleType.Admin && !a.IsDeleted);

            return admin?.Id;
        }

        public async Task<ChatMessageResponseDto> SendMessageAsync(Guid senderId, CreateChatMessageDto createDto)
        {
            // Get sender information
            var sender = await _unitOfWork.Account.GetByIdAsync(senderId);
            if (sender == null || sender.IsDeleted)
            {
                throw new Exception("Sender not found");
            }

            // Determine receiver
            Guid receiverId;

            if (sender.Role == RoleType.Admin)
            {
                // Admin must specify receiver
                if (createDto.ReceiverId == null || createDto.ReceiverId == Guid.Empty)
                {
                    throw new Exception("Admin must specify receiver");
                }
                receiverId = createDto.ReceiverId.Value;
            }
            else
            {
                // User/Owner always send to Admin
                var adminId = await GetAdminIdAsync();
                if (adminId == null)
                {
                    throw new Exception("Admin account not found");
                }
                receiverId = adminId.Value;
            }

            // Get receiver information
            var receiver = await _unitOfWork.Account.GetByIdAsync(receiverId);
            if (receiver == null || receiver.IsDeleted)
            {
                throw new Exception("Receiver not found");
            }

            // Create chat message
            var chatMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                SenderId = senderId,
                ReceiverId = receiverId,
                Message = createDto.Message,
                SentAt = DateTime.UtcNow,
                IsRead = false,
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
                SenderId = chatMessage.SenderId,
                SenderName = sender.FullName,
                SenderRole = sender.Role.ToString(),
                ReceiverId = chatMessage.ReceiverId,
                ReceiverName = receiver.FullName,
                ReceiverRole = receiver.Role.ToString(),
                Message = chatMessage.Message,
                SentAt = chatMessage.SentAt,
                IsRead = chatMessage.IsRead,
                CreatedAt = chatMessage.CreatedAt
            };
        }

        public async Task<List<ChatMessageResponseDto>> GetChatHistoryAsync(Guid userId1, Guid userId2, int pageNumber = 1, int pageSize = 50)
        {
            var messages = await _unitOfWork.ChatMessage
                .GetQueryable()
                .Where(m => !m.IsDeleted &&
                           ((m.SenderId == userId1 && m.ReceiverId == userId2) ||
                            (m.SenderId == userId2 && m.ReceiverId == userId1)))
                .OrderByDescending(m => m.SentAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get both users
            var userIds = new List<Guid> { userId1, userId2 };
            var users = await _unitOfWork.Account
                .GetQueryable()
                .Where(a => userIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a);

            return messages.Select(m => new ChatMessageResponseDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = users.ContainsKey(m.SenderId) ? users[m.SenderId].FullName : "Unknown",
                SenderRole = users.ContainsKey(m.SenderId) ? users[m.SenderId].Role.ToString() : "Unknown",
                ReceiverId = m.ReceiverId,
                ReceiverName = users.ContainsKey(m.ReceiverId) ? users[m.ReceiverId].FullName : "Unknown",
                ReceiverRole = users.ContainsKey(m.ReceiverId) ? users[m.ReceiverId].Role.ToString() : "Unknown",
                Message = m.Message,
                SentAt = m.SentAt,
                IsRead = m.IsRead,
                CreatedAt = m.CreatedAt
            }).OrderBy(m => m.SentAt).ToList();
        }

        public async Task<List<ChatMessageResponseDto>> GetRecentMessagesAsync(Guid userId1, Guid userId2, int count = 20)
        {
            var messages = await _unitOfWork.ChatMessage
                .GetQueryable()
                .Where(m => !m.IsDeleted &&
                           ((m.SenderId == userId1 && m.ReceiverId == userId2) ||
                            (m.SenderId == userId2 && m.ReceiverId == userId1)))
                .OrderByDescending(m => m.SentAt)
                .Take(count)
                .ToListAsync();

            // Get both users
            var userIds = new List<Guid> { userId1, userId2 };
            var users = await _unitOfWork.Account
                .GetQueryable()
                .Where(a => userIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a);

            return messages.Select(m => new ChatMessageResponseDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = users.ContainsKey(m.SenderId) ? users[m.SenderId].FullName : "Unknown",
                SenderRole = users.ContainsKey(m.SenderId) ? users[m.SenderId].Role.ToString() : "Unknown",
                ReceiverId = m.ReceiverId,
                ReceiverName = users.ContainsKey(m.ReceiverId) ? users[m.ReceiverId].FullName : "Unknown",
                ReceiverRole = users.ContainsKey(m.ReceiverId) ? users[m.ReceiverId].Role.ToString() : "Unknown",
                Message = m.Message,
                SentAt = m.SentAt,
                IsRead = m.IsRead,
                CreatedAt = m.CreatedAt
            }).OrderBy(m => m.SentAt).ToList();
        }

        public async Task<List<ChatConversationDto>> GetAdminConversationsAsync(Guid adminId)
        {
            // Get all unique users who have chatted with admin
            var userIds = await _unitOfWork.ChatMessage
                .GetQueryable()
                .Where(m => !m.IsDeleted && (m.SenderId == adminId || m.ReceiverId == adminId))
                .SelectMany(m => new[] { m.SenderId, m.ReceiverId })
                .Where(id => id != adminId)
                .Distinct()
                .ToListAsync();

            var conversations = new List<ChatConversationDto>();

            foreach (var userId in userIds)
            {
                var user = await _unitOfWork.Account.GetByIdAsync(userId);
                if (user == null || user.IsDeleted) continue;

                // Get last message
                var lastMessage = await _unitOfWork.ChatMessage
                    .GetQueryable()
                    .Where(m => !m.IsDeleted &&
                               ((m.SenderId == adminId && m.ReceiverId == userId) ||
                                (m.SenderId == userId && m.ReceiverId == adminId)))
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                // Count unread messages
                var unreadCount = await _unitOfWork.ChatMessage
                    .GetQueryable()
                    .CountAsync(m => !m.IsDeleted &&
                                    m.SenderId == userId &&
                                    m.ReceiverId == adminId &&
                                    !m.IsRead);

                conversations.Add(new ChatConversationDto
                {
                    UserId = userId,
                    UserName = user.FullName,
                    UserRole = user.Role.ToString(),
                    LastMessage = lastMessage?.Message,
                    LastMessageTime = lastMessage?.SentAt,
                    UnreadCount = unreadCount
                });
            }

            return conversations.OrderByDescending(c => c.LastMessageTime).ToList();
        }

        public async Task MarkMessagesAsReadAsync(Guid userId, Guid otherUserId)
        {
            var unreadMessages = await _unitOfWork.ChatMessage
                .GetQueryable()
                .Where(m => !m.IsDeleted &&
                           m.SenderId == otherUserId &&
                           m.ReceiverId == userId &&
                           !m.IsRead)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.UpdatedAt = DateTime.UtcNow;
                message.UpdatedBy = userId;
            }

            if (unreadMessages.Any())
            {
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
