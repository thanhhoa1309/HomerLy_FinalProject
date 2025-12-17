using HomerLy.Business.Interfaces;
using Homerly.Business.Interfaces;
using HomerLy.BusinessObject.DTOs.ChatMessageDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Chat
{
    [Authorize]
    public class TenancyChatModel : PageModel
    {
        private readonly IChatService _chatService;
        private readonly ITenancyService _tenancyService;
        private readonly ILogger<TenancyChatModel> _logger;

        public TenancyChatModel(
            IChatService chatService,
            ITenancyService tenancyService,
            ILogger<TenancyChatModel> logger)
        {
            _chatService = chatService;
            _tenancyService = tenancyService;
            _logger = logger;
        }

        public Guid TenancyId { get; set; }
        public List<ChatMessageResponseDto> Messages { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public bool HasAccess { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid tenancyId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                TenancyId = tenancyId;

                // Check if user has access to this chat
                HasAccess = await _chatService.CanUserAccessChatAsync(userId, tenancyId);

                if (!HasAccess)
                {
                    ErrorMessage = "You don't have access to this chat.";
                    return Page();
                }

                // Get tenancy details to display
                var tenancy = await _tenancyService.GetTenancyByIdAsync(tenancyId, userId);
                if (tenancy == null)
                {
                    ErrorMessage = "Tenancy not found.";
                    return Page();
                }

                // Load recent messages
                Messages = await _chatService.GetRecentMessagesAsync(tenancyId, 50);

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
