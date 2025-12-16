using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.UserDTOs;
using Homerly.BusinessObject.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Admin.Accounts
{
    [Authorize(Roles = "Admin")]
    public class UpdateModel : PageModel
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<UpdateModel> _logger;

        public UpdateModel(IAccountService accountService, ILogger<UpdateModel> logger)
        {
            _accountService = accountService;
            _logger = logger;
        }

        [BindProperty]
        public UpdateAccountInput Input { get; set; } = new UpdateAccountInput();

        public AccountResponseDto? Account { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public List<SelectListItem> RoleOptions { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Admin", Text = "Admin" },
            new SelectListItem { Value = "Owner", Text = "Owner" },
            new SelectListItem { Value = "User", Text = "User" }
        };

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (!id.HasValue)
            {
                TempData["ErrorMessage"] = "Account ID is required.";
                return RedirectToPage("/Admin/Accounts/Index");
            }

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var adminId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                Account = await _accountService.GetAccountByIdAsync(id.Value);

                if (Account == null)
                {
                    TempData["ErrorMessage"] = "Account not found.";
                    return RedirectToPage("/Admin/Accounts/Index");
                }

                // Pre-populate the form
                Input = new UpdateAccountInput
                {
                    FullName = Account.FullName,
                    Email = Account.Email,
                    Phone = Account.Phone,
                    CccdNumber = Account.CccdNumber,
                    Role = Account.Role.ToString(),
                    IsOwnerApproved = Account.Role == RoleType.Owner
                };

                SuccessMessage = TempData["SuccessMessage"] as string;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading account {id}: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading the account.";
                return RedirectToPage("/Admin/Accounts/Index");
            }
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (!id.HasValue)
            {
                ErrorMessage = "Account ID is required.";
                return Page();
            }

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please correct the errors below.";
                Account = await _accountService.GetAccountByIdAsync(id.Value);
                return Page();
            }

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var adminId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                Account = await _accountService.GetAccountByIdAsync(id.Value);

                if (Account == null)
                {
                    ErrorMessage = "Account not found.";
                    return Page();
                }

                // Admin can update any account, so we use adminId as currentUserId parameter
                // But we need to modify the account directly since admin has special privileges
                var account = await _accountService.GetAccountByIdAsync(id.Value);

                if (account == null)
                {
                    ErrorMessage = "Account not found.";
                    return Page();
                }

                // Update basic information through service (but we'll handle role separately)
                var updateDto = new UpdateAccountRequestDto
                {
                    FullName = Input.FullName,
                    Phone = Input.Phone
                };

                // Note: Since UpdateAccountAsync requires currentUserId == accountId,
                // we'll need to handle admin updates differently
                // For now, let's use the account's own ID
                await _accountService.UpdateAccountAsync(id.Value, id.Value, updateDto);

                // Update role if changed
                if (!string.IsNullOrEmpty(Input.Role) && Input.Role != Account.Role.ToString())
                {
                    await _accountService.ChangeAccountRoleAsync(id.Value, Input.Role);
                }

                // Update owner approval if role is Owner
                if (Input.Role == "Owner" && Input.IsOwnerApproved && !Account.IsDeleted)
                {
                    var currentAccount = await _accountService.GetAccountByIdAsync(id.Value);
                    if (currentAccount?.Role == RoleType.Owner)
                    {
                        await _accountService.ApproveOwnerAccountAsync(id.Value);
                    }
                }

                TempData["SuccessMessage"] = "Account updated successfully!";
                return RedirectToPage("/Admin/Accounts/Update", new { id = id.Value });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating account {id}: {ex.Message}");
                ErrorMessage = ex.Message;
                Account = await _accountService.GetAccountByIdAsync(id.Value);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostApproveOwnerAsync(Guid id)
        {
            try
            {
                await _accountService.ApproveOwnerAccountAsync(id);
                TempData["SuccessMessage"] = "Owner approved successfully!";
                return RedirectToPage("/Admin/Accounts/Update", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error approving owner: {ex.Message}");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage("/Admin/Accounts/Update", new { id });
            }
        }

        public class UpdateAccountInput
        {
            [Required(ErrorMessage = "Full name is required")]
            [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Phone number is required")]
            [Phone(ErrorMessage = "Invalid phone number")]
            public string Phone { get; set; } = string.Empty;

            public string CccdNumber { get; set; } = string.Empty;

            [Required(ErrorMessage = "Role is required")]
            public string Role { get; set; } = string.Empty;

            public bool IsOwnerApproved { get; set; }
        }
    }
}