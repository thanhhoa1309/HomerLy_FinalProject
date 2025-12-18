using HomerLy.Business.Interfaces;
using HomerLy.BusinessObject.DTOs.ChatMessageDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Chat
{
    [Authorize(Roles = "User,Owner")]
    public class AdminChatModel : PageModel
    {
        private readonly IChatService _chatService;
        private readonly ILogger<AdminChatModel> _logger;

        public AdminChatModel(
            IChatService chatService,
            ILogger<AdminChatModel> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        public Guid? AdminId { get; set; }
        public List<ChatMessageResponseDto> Messages { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public string? UserRole { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                UserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // Get Admin ID
                AdminId = await _chatService.GetAdminIdAsync();
                if (AdminId == null)
                {
                    ErrorMessage = "Admin account not found in the system.";
                    return Page();
                }

                // Load recent messages with admin
                Messages = await _chatService.GetRecentMessagesAsync(userId, AdminId.Value, 50);

                // Mark messages as read
                await _chatService.MarkMessagesAsReadAsync(userId, AdminId.Value);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading admin chat: {ex.Message}");
                ErrorMessage = "An error occurred while loading the chat.";
                return Page();
            }
        }
    }
}
