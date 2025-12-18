using HomerLy.Business.Interfaces;
using HomerLy.BusinessObject.DTOs.ChatMessageDTOs;
using Homerly.BusinessObject.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace HomerLy.Presentation.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            var userRole = GetUserRole();

            // Add user to their personal group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            _logger.LogInformation($"User {userId} ({userRole}) connected to chat hub");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");

            _logger.LogInformation($"User {userId} disconnected from chat hub");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinAdminChat()
        {
            try
            {
                var userId = GetUserId();
                var userRole = GetUserRole();

                // Get admin ID (or current user ID if they are admin)
                Guid otherUserId;
                if (userRole == "Admin")
                {
                    // Admin joins - they will be notified of messages from all users
                    _logger.LogInformation($"Admin {userId} joined admin chat hub");
                    return; // Admin is already in their group
                }
                else
                {
                    // User/Owner joins - load chat with admin
                    var adminId = await _chatService.GetAdminIdAsync();
                    if (adminId == null)
                    {
                        await Clients.Caller.SendAsync("Error", "Admin not found");
                        return;
                    }
                    otherUserId = adminId.Value;
                }

                _logger.LogInformation($"User {userId} ({userRole}) joined chat with admin");

                // Send recent messages to the user
                var recentMessages = await _chatService.GetRecentMessagesAsync(userId, otherUserId);
                await Clients.Caller.SendAsync("LoadChatHistory", recentMessages);

                // Mark messages as read
                await _chatService.MarkMessagesAsReadAsync(userId, otherUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining admin chat");
                await Clients.Caller.SendAsync("Error", "Failed to join chat");
            }
        }

        public async Task LoadConversation(string otherUserIdString)
        {
            try
            {
                var userId = GetUserId();
                var otherUserId = Guid.Parse(otherUserIdString);

                _logger.LogInformation($"Admin {userId} loading conversation with user {otherUserId}");

                // Load chat history
                var recentMessages = await _chatService.GetRecentMessagesAsync(userId, otherUserId);
                await Clients.Caller.SendAsync("LoadChatHistory", recentMessages);

                // Mark messages as read
                await _chatService.MarkMessagesAsReadAsync(userId, otherUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading conversation");
                await Clients.Caller.SendAsync("Error", "Failed to load conversation");
            }
        }

        public async Task SendMessage(string? receiverIdString, string message)
        {
            try
            {
                var userId = GetUserId();
                var userRole = GetUserRole();

                Guid? receiverId = null;
                if (!string.IsNullOrEmpty(receiverIdString))
                {
                    receiverId = Guid.Parse(receiverIdString);
                }

                var createDto = new CreateChatMessageDto
                {
                    ReceiverId = receiverId,
                    Message = message
                };

                // Save message and get response
                var chatMessage = await _chatService.SendMessageAsync(userId, createDto);

                // Send to sender (confirmation)
                await Clients.Caller.SendAsync("ReceiveMessage", chatMessage);

                // Send to receiver
                await Clients.Group($"user_{chatMessage.ReceiverId}")
                    .SendAsync("ReceiveMessage", chatMessage);

                _logger.LogInformation($"User {userId} sent message to {chatMessage.ReceiverId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        public async Task LoadMoreMessages(string otherUserIdString, int pageNumber)
        {
            try
            {
                var userId = GetUserId();
                var otherUserId = Guid.Parse(otherUserIdString);

                var messages = await _chatService.GetChatHistoryAsync(userId, otherUserId, pageNumber);
                await Clients.Caller.SendAsync("LoadMoreMessagesResult", messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading more messages");
                await Clients.Caller.SendAsync("Error", "Failed to load messages");
            }
        }

        public async Task TypingIndicator(string otherUserIdString, bool isTyping)
        {
            try
            {
                var userId = GetUserId();
                var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                var otherUserId = Guid.Parse(otherUserIdString);

                // Notify the other user
                await Clients.Group($"user_{otherUserId}")
                    .SendAsync("UserTyping", new { userId, userName, isTyping });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending typing indicator");
            }
        }

        private Guid GetUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new HubException("User not authenticated");
            }
            return userId;
        }

        private string GetUserRole()
        {
            var roleClaim = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            return roleClaim ?? "Unknown";
        }
    }
}
