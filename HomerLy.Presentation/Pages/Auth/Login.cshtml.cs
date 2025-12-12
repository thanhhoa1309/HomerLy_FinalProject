using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.AuthDTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(IAuthService authService, IConfiguration configuration, ILogger<LoginModel> logger)
        {
            _authService = authService;
            _configuration = configuration;
            _logger = logger;
        }

        [BindProperty]
        public LoginRequestDto LoginRequest { get; set; } = null!;

        public string? ErrorMessage { get; set; }
        public string? ReturnUrl { get; set; }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var result = await _authService.LoginAsync(LoginRequest, _configuration);

                if (result == null)
                {
                    ErrorMessage = "Invalid email or password. Please try again.";
                    return Page();
                }

                // Store token in session
                HttpContext.Session.SetString("AuthToken", result.Token);

                // Decode JWT token to get user information
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(result.Token);
                
                var email = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value 
                           ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                var role = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value 
                          ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value 
                            ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value;

                if (!string.IsNullOrEmpty(email))
                    HttpContext.Session.SetString("UserEmail", email);
                if (!string.IsNullOrEmpty(role))
                    HttpContext.Session.SetString("UserRole", role);
                if (!string.IsNullOrEmpty(userId))
                    HttpContext.Session.SetString("UserId", userId);

                _logger.LogInformation($"User {email} logged in successfully with role {role}");

                // Redirect based on user role
                var redirectPath = role?.ToUpper() switch
                {
                    "ADMIN" => "/Admin/Dashboard",
                    "OWNER" => "/Owner/Dashboard",
                    "USER" => "/User/Dashboard",
                    _ => "/Index"
                };

                // Check if there's a return URL
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToPage(redirectPath);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Login error: {ex.Message}");
                ErrorMessage = "An error occurred during login. Please try again.";
                return Page();
            }
        }
    }
}
