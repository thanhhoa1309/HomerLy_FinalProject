using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.AuthDTOs;
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
    public class CreateModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(IAuthService authService, ILogger<CreateModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [BindProperty]
        public CreateAccountInput Input { get; set; } = new CreateAccountInput();

        public string? ErrorMessage { get; set; }

        public List<SelectListItem> RoleOptions { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Admin", Text = "Admin" },
            new SelectListItem { Value = "Owner", Text = "Owner" },
            new SelectListItem { Value = "User", Text = "User" }
        };

        public IActionResult OnGet()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var adminId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading create account page: {ex.Message}");
                ErrorMessage = "An error occurred while loading the page.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please correct the errors below.";
                return Page();
            }

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var adminId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                // Validate password confirmation
                if (Input.Password != Input.ConfirmPassword)
                {
                    ErrorMessage = "Password and confirmation password do not match.";
                    return Page();
                }

                // Parse role
                if (!Enum.TryParse<RoleType>(Input.Role, out var roleType))
                {
                    ErrorMessage = "Invalid role selected.";
                    return Page();
                }

                // Create registration DTO
                var registrationDto = new AccountRegistrationDto
                {
                    FullName = Input.FullName,
                    Email = Input.Email,
                    Phone = Input.Phone,
                    CccdNumber = Input.CccdNumber,
                    Password = Input.Password,
                    Role = roleType
                };

                // Register the account
                var result = await _authService.RegisterUserAsync(registrationDto);

                if (result == null)
                {
                    ErrorMessage = "Failed to create account. Please try again.";
                    return Page();
                }

                _logger.LogInformation($"Account created successfully by admin {adminId}: {result.Email}");
                TempData["SuccessMessage"] = $"Account for '{result.FullName}' has been created successfully!";

                return RedirectToPage("/Admin/Accounts/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating account: {ex.Message}");

                // Handle specific errors
                if (ex.Message.Contains("email") || ex.Message.Contains("Email"))
                {
                    ErrorMessage = "This email is already registered.";
                }
                else if (ex.Message.Contains("CCCD") || ex.Message.Contains("cccd"))
                {
                    ErrorMessage = "This CCCD number is already registered.";
                }
                else
                {
                    ErrorMessage = ex.Message;
                }

                return Page();
            }
        }

        public class CreateAccountInput
        {
            [Required(ErrorMessage = "Full name is required")]
            [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Phone number is required")]
            [Phone(ErrorMessage = "Invalid phone number")]
            [RegularExpression(@"^(\+84|0)[0-9]{9}$", ErrorMessage = "Phone number must be a valid Vietnamese phone number")]
            public string Phone { get; set; } = string.Empty;

            [Required(ErrorMessage = "CCCD number is required")]
            [RegularExpression(@"^(\d{9}|\d{12})$", ErrorMessage = "CCCD number must be exactly 9 or 12 digits")]
            public string CccdNumber { get; set; } = string.Empty;

            [Required(ErrorMessage = "Role is required")]
            public string Role { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please confirm your password")]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Password and confirmation password do not match")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }
    }
}