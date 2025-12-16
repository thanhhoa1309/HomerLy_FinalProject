using System.Security.Claims;
using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.UserDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HomerLy.Presentation.Pages.User
{
    [Authorize(Roles = "User")]
    public class ProfileModel : PageModel
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<ProfileModel> _logger;

        public ProfileModel(IAccountService accountService, ILogger<ProfileModel> logger)
        {
            _accountService = accountService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public class InputModel
        {
            public string? FullName { get; set; }
            public string? Email { get; set; }
            public string? PhoneNumber { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var userProfile = await _accountService.GetCurrentAccountProfileAsync(userId);
            if (userProfile == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            Input.FullName = userProfile.FullName;
            Input.Email = userProfile.Email;
            Input.PhoneNumber = userProfile.Phone;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            try
            {
                var updateDto = new UpdateAccountRequestDto
                {
                    FullName = Input.FullName ?? string.Empty,
                    Phone = Input.PhoneNumber ?? string.Empty
                };

                await _accountService.UpdateAccountAsync(userId, userId, updateDto);

                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                ModelState.AddModelError(string.Empty, "An error occurred while updating the profile.");
                return Page();
            }
        }
    }
}
