using HomerLy.Business.Interfaces;
using Homerly.Business.Interfaces;
using HomerLy.BusinessObject.DTOs.ChatMessageDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Chat
{
    [Authorize(Roles = "Admin")]
    public class AdminChatWithUserModel : PageModel
    {
        private readonly IChatService _chatService;
        private readonly IAccountService _accountService;
        private readonly ILogger<AdminChatWithUserModel> _logger;

        public AdminChatWithUserModel(
            IChatService chatService,
            IAccountService accountService,
            ILogger<AdminChatWithUserModel> logger)
        {
            _chatService = chatService;
            _accountService = accountService;
            _logger = logger;
        }

        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserRole { get; set; }
        public List<ChatMessageResponseDto> Messages { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid userId)
        {
            try
            {
                var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(adminIdClaim) || !Guid.TryParse(adminIdClaim, out var adminId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                UserId = userId;

                // Get user details
                var user = await _accountService.GetAccountByIdAsync(userId);
                if (user == null)
                {
                    ErrorMessage = "User not found.";
                    return Page();
                }

                UserName = user.FullName;
                UserRole = user.Role.ToString();

                // Load recent messages with user
                Messages = await _chatService.GetRecentMessagesAsync(adminId, userId, 50);

                // Mark messages as read
                await _chatService.MarkMessagesAsReadAsync(adminId, userId);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading chat: {ex.Message}");
                ErrorMessage = "An error occurred while loading the chat.";
                return Page();
            }
        }
    }
}
