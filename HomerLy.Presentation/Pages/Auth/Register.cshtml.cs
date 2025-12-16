using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.AuthDTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HomerLy.Presentation.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(IAuthService authService, ILogger<RegisterModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [BindProperty]
        public AccountRegistrationDto RegisterRequest { get; set; } = null!;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please fill in all required fields correctly.";
                return Page();
            }

            try
            {
                var result = await _authService.RegisterUserAsync(RegisterRequest);

                if (result == null)
                {
                    ErrorMessage = "Registration failed. Please try again.";
                    return Page();
                }

                _logger.LogInformation($"New user registered successfully: {RegisterRequest.Email}");
                
                // Set success message in TempData to show on login page
                TempData["SuccessMessage"] = $"Account created successfully! Welcome {RegisterRequest.FullName}. Please login to continue.";

                // Redirect to login page
                return RedirectToPage("/Auth/Login");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Registration error: {ex.Message}");
                
                // Handle specific error messages
                if (ex.Message.Contains("email") || ex.Message.Contains("Email"))
                {
                    ErrorMessage = "This email is already registered. Please use a different email or login.";
                }
                else if (ex.Message.Contains("phone") || ex.Message.Contains("Phone"))
                {
                    ErrorMessage = "This phone number is already registered. Please use a different number.";
                }
                else if (ex.Message.Contains("CCCD") || ex.Message.Contains("cccd"))
                {
                    ErrorMessage = "This CCCD number is already registered. Please verify your CCCD number.";
                }
                else
                {
                    ErrorMessage = "Registration failed. " + ex.Message;
                }
                
                return Page();
            }
        }
    }
}
