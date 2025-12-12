using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HomerLy.Presentation.Pages.Auth
{
    public class LogoutModel : PageModel
    {
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(ILogger<LogoutModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            // Clear all session data
            var userEmail = HttpContext.Session.GetString("UserEmail");
            
            HttpContext.Session.Clear();
            
            _logger.LogInformation($"User {userEmail} logged out successfully");

            // Redirect to home page
            return RedirectToPage("/Home/LandingPage");
        }

        public IActionResult OnPost()
        {
            return OnGet();
        }
    }
}
