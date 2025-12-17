using HomerLy.Business.Interfaces;
using HomerLy.BusinessObject.DTOs.ChatMessageDTOs;
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
            _logger.LogInformation($"User {userId} connected to chat hub");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            _logger.LogInformation($"User {userId} disconnected from chat hub");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinTenancyChat(string tenancyId)
        {
            try
            {
                var userId = GetUserId();
                var tenancyGuid = Guid.Parse(tenancyId);

                // Verify user has access to this tenancy chat
                var hasAccess = await _chatService.CanUserAccessChatAsync(userId, tenancyGuid);
                if (!hasAccess)
                {
                    await Clients.Caller.SendAsync("Error", "You don't have access to this chat");
                    return;
                }

                // Add user to SignalR group for this tenancy
                await Groups.AddToGroupAsync(Context.ConnectionId, $"tenancy_{tenancyId}");
                
                _logger.LogInformation($"User {userId} joined tenancy chat {tenancyId}");
                
                // Send recent messages to the user
                var recentMessages = await _chatService.GetRecentMessagesAsync(tenancyGuid);
                await Clients.Caller.SendAsync("LoadChatHistory", recentMessages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining tenancy chat");
                await Clients.Caller.SendAsync("Error", "Failed to join chat");
            }
        }

        public async Task LeaveTenancyChat(string tenancyId)
        {
            try
            {
                var userId = GetUserId();
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenancy_{tenancyId}");
                _logger.LogInformation($"User {userId} left tenancy chat {tenancyId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving tenancy chat");
            }
        }

        public async Task SendMessage(string tenancyId, string message)
        {
            try
            {
                var userId = GetUserId();
                var tenancyGuid = Guid.Parse(tenancyId);

                var createDto = new CreateChatMessageDto
                {
                    TenancyId = tenancyGuid,
                    Message = message
                };

                // Save message and get response
                var chatMessage = await _chatService.SendMessageAsync(userId, createDto);

                // Broadcast message to all users in the tenancy chat group
                await Clients.Group($"tenancy_{tenancyId}")
                    .SendAsync("ReceiveMessage", chatMessage);

                _logger.LogInformation($"User {userId} sent message in tenancy {tenancyId}");
            }
            catch (UnauthorizedAccessException ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                await Clients.Caller.SendAsync("Error", "Failed to send message");
            }
        }

        public async Task LoadMoreMessages(string tenancyId, int pageNumber)
        {
            try
            {
                var userId = GetUserId();
                var tenancyGuid = Guid.Parse(tenancyId);

                // Verify user has access
                var hasAccess = await _chatService.CanUserAccessChatAsync(userId, tenancyGuid);
                if (!hasAccess)
                {
                    await Clients.Caller.SendAsync("Error", "You don't have access to this chat");
                    return;
                }

                var messages = await _chatService.GetChatHistoryAsync(tenancyGuid, pageNumber);
                await Clients.Caller.SendAsync("LoadMoreMessagesResult", messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading more messages");
                await Clients.Caller.SendAsync("Error", "Failed to load messages");
            }
        }

        public async Task TypingIndicator(string tenancyId, bool isTyping)
        {
            try
            {
                var userId = GetUserId();
                var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                // Notify other users in the group
                await Clients.OthersInGroup($"tenancy_{tenancyId}")
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
    }
}
