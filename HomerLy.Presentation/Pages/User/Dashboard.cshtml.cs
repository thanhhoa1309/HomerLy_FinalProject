using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.UserDTOs;
using Homerly.BusinessObject.DTOs.TenancyDTOs;
using Homerly.BusinessObject.Enums;
using HomerLy.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.User
{
    [Authorize(Roles = "User")]
    public class DashboardModel : PageModel
    {
        private readonly IAccountService _accountService;
        private readonly ITenancyService _tenancyService;
        private readonly ILogger<DashboardModel> _logger;

        public DashboardModel(
            IAccountService accountService,
            ITenancyService tenancyService,
            ILogger<DashboardModel> logger)
        {
            _accountService = accountService;
            _tenancyService = tenancyService;
            _logger = logger;
        }

        public AccountResponseDto? CurrentUser { get; set; }
        public string? ErrorMessage { get; set; }
        public List<TenancyResponseDto>? ActiveTenancies { get; set; }

        [BindProperty]
        public UpdateAccountRequestDto UpdateRequest { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                CurrentUser = await _accountService.GetCurrentAccountProfileAsync(userId);

                if (CurrentUser == null)
                {
                    ErrorMessage = "Unable to load user profile.";
                }

                // Get active tenancies for chat button
                try
                {
                    var tenancies = await _tenancyService.GetTenanciesByTenantIdAsync(
                        tenantId: userId,
                        userId: userId,
                        pageNumber: 1,
                        pageSize: 10,
                        status: TenancyStatus.active);
                    ActiveTenancies = tenancies.ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Could not load active tenancies: {ex.Message}");
                    ActiveTenancies = new List<TenancyResponseDto>();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading user dashboard: {ex.Message}");
                ErrorMessage = "An error occurred while loading your dashboard.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdateProfileAsync()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                if (!ModelState.IsValid)
                {
                    CurrentUser = await _accountService.GetCurrentAccountProfileAsync(userId);
                    return Page();
                }

                // Update using the same userId for both parameters (user updating their own account)
                var updatedAccount = await _accountService.UpdateAccountAsync(userId, userId, UpdateRequest);

                if (updatedAccount != null)
                {
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToPage();
                }

                ErrorMessage = "Failed to update profile.";
                CurrentUser = await _accountService.GetCurrentAccountProfileAsync(userId);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating profile: {ex.Message}");
                ErrorMessage = ex.Message;

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
                {
                    CurrentUser = await _accountService.GetCurrentAccountProfileAsync(userId);
                }

                return Page();
            }
        }
    }
}
