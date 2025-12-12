using Homerly.Business.Interfaces;
using Homerly.Business.Utils;
using Homerly.BusinessObject.DTOs.UserDTOs;
using Homerly.BusinessObject.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Admin.Accounts
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IAccountService accountService, ILogger<IndexModel> logger)
        {
            _accountService = accountService;
            _logger = logger;
        }

        public Pagination<AccountResponseDto>? Accounts { get; set; }
        public string? RoleFilter { get; set; }
        public bool? IsOwnerApprovedFilter { get; set; }
        public string? SearchQuery { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        
        public int TotalOwners { get; set; }
        public int TotalUsers { get; set; }
        public int PendingApprovalCount { get; set; }

        public async Task<IActionResult> OnGetAsync(
            int pageNumber = 1,
            string? role = null,
            bool? isOwnerApproved = null,
            string? search = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var adminId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                RoleFilter = role;
                IsOwnerApprovedFilter = isOwnerApproved;
                SearchQuery = search;

                RoleType? roleType = null;
                if (!string.IsNullOrEmpty(role) && Enum.TryParse<RoleType>(role, out var parsedRole))
                {
                    roleType = parsedRole;
                }

                // Get filtered accounts - using correct parameter name 'searchTerm'
                Accounts = await _accountService.GetAccountsAsync(
                    pageNumber: pageNumber,
                    pageSize: 10,
                    searchTerm: search,
                    role: roleType,
                    isOwnerApproved: isOwnerApproved);

                // Get statistics
                var allOwners = await _accountService.GetAccountsAsync(
                    pageNumber: 1,
                    pageSize: int.MaxValue,
                    role: RoleType.Owner);
                TotalOwners = allOwners.TotalCount;

                var allUsers = await _accountService.GetAccountsAsync(
                    pageNumber: 1,
                    pageSize: int.MaxValue,
                    role: RoleType.User);
                TotalUsers = allUsers.TotalCount;

                var pendingOwners = await _accountService.GetAccountsAsync(
                    pageNumber: 1,
                    pageSize: int.MaxValue,
                    role: RoleType.Owner,
                    isOwnerApproved: false);
                PendingApprovalCount = pendingOwners.TotalCount;

                SuccessMessage = TempData["SuccessMessage"] as string;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading accounts: {ex.Message}");
                ErrorMessage = "An error occurred while loading accounts.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostApproveOwnerAsync(Guid accountId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var adminId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                // Using correct method name
                var success = await _accountService.ApproveOwnerAccountAsync(accountId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Owner approved successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to approve owner.";
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error approving owner: {ex.Message}");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid accountId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var adminId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                // Using correct method signature (only accountId)
                var success = await _accountService.DeleteAccountAsync(accountId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Account deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete account.";
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting account: {ex.Message}");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage();
            }
        }
    }
}
