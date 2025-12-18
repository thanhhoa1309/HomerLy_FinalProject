using HomerLy.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using static HomerLy.Business.Interfaces.IChatService;

namespace HomerLy.Presentation.Pages.Admin.Chats
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IChatService _chatService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IChatService chatService, ILogger<IndexModel> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        public List<ChatConversationDto> Conversations { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var adminId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                // Get all conversations
                Conversations = await _chatService.GetAdminConversationsAsync(adminId);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading conversations: {ex.Message}");
                ErrorMessage = "An error occurred while loading conversations.";
                return Page();
            }
        }
    }
}
