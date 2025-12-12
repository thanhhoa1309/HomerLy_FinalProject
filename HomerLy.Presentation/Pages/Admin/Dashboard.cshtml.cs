using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.UserDTOs;
using Homerly.BusinessObject.Enums;
using Homerly.Business.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<DashboardModel> _logger;

        public DashboardModel(IAccountService accountService, ILogger<DashboardModel> logger)
        {
            _accountService = accountService;
            _logger = logger;
        }

        public AccountResponseDto? CurrentUser { get; set; }
        public Pagination<AccountResponseDto>? RecentAccounts { get; set; }
        public int TotalUsers { get; set; }
        public int TotalOwners { get; set; }
        public int TotalAdmins { get; set; }
        public string? ErrorMessage { get; set; }

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
                    ErrorMessage = "Unable to load admin profile.";
                    return Page();
                }

                // Get recent accounts using unified method
                RecentAccounts = await _accountService.GetAccountsAsync(pageNumber: 1, pageSize: 5);

                // Get statistics using unified method with role filter
                var allUsers = await _accountService.GetAccountsAsync(
                    pageNumber: 1, 
                    pageSize: int.MaxValue, 
                    role: RoleType.User);
                    
                var allOwners = await _accountService.GetAccountsAsync(
                    pageNumber: 1, 
                    pageSize: int.MaxValue, 
                    role: RoleType.Owner);
                    
                var allAdmins = await _accountService.GetAccountsAsync(
                    pageNumber: 1, 
                    pageSize: int.MaxValue, 
                    role: RoleType.Admin);

                TotalUsers = allUsers.TotalCount;
                TotalOwners = allOwners.TotalCount;
                TotalAdmins = allAdmins.TotalCount;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading admin dashboard: {ex.Message}");
                ErrorMessage = "An error occurred while loading the dashboard.";
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
                    await LoadDashboardDataAsync(userId);
                    return Page();
                }

                // Admin updating their own profile
                var updatedAccount = await _accountService.UpdateAccountAsync(userId, userId, UpdateRequest);

                if (updatedAccount != null)
                {
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToPage();
                }

                ErrorMessage = "Failed to update profile.";
                await LoadDashboardDataAsync(userId);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating profile: {ex.Message}");
                ErrorMessage = ex.Message;
                
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
                {
                    await LoadDashboardDataAsync(userId);
                }
                
                return Page();
            }
        }

        private async Task LoadDashboardDataAsync(Guid userId)
        {
            CurrentUser = await _accountService.GetCurrentAccountProfileAsync(userId);
            RecentAccounts = await _accountService.GetAccountsAsync(pageNumber: 1, pageSize: 5);

            var allUsers = await _accountService.GetAccountsAsync(
                pageNumber: 1, 
                pageSize: int.MaxValue, 
                role: RoleType.User);
                
            var allOwners = await _accountService.GetAccountsAsync(
                pageNumber: 1, 
                pageSize: int.MaxValue, 
                role: RoleType.Owner);
                
            var allAdmins = await _accountService.GetAccountsAsync(
                pageNumber: 1, 
                pageSize: int.MaxValue, 
                role: RoleType.Admin);

            TotalUsers = allUsers.TotalCount;
            TotalOwners = allOwners.TotalCount;
            TotalAdmins = allAdmins.TotalCount;
        }
    }
}
