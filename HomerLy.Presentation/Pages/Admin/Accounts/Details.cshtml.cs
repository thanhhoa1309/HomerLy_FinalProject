using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.UserDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Admin.Accounts
{
    [Authorize(Roles = "Admin")]
    public class DetailsModel : PageModel
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(IAccountService accountService, ILogger<DetailsModel> logger)
        {
            _accountService = accountService;
            _logger = logger;
        }

        public AccountResponseDto? Account { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var adminId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                Account = await _accountService.GetAccountByIdAsync(id);

                if (Account == null)
                {
                    ErrorMessage = "Account not found.";
                    return Page();
                }

                SuccessMessage = TempData["SuccessMessage"] as string;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading account details: {ex.Message}");
                ErrorMessage = "An error occurred while loading account details.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var adminId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                var success = await _accountService.DeleteAccountAsync(id);

                if (success)
                {
                    TempData["SuccessMessage"] = "Account deleted successfully!";
                    return RedirectToPage("/Admin/Accounts/Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete account.";
                    return RedirectToPage(new { id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting account: {ex.Message}");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage(new { id });
            }
        }
    }
}