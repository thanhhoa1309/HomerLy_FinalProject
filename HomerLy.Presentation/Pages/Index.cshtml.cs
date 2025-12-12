using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            // Check if user is authenticated
            if (User.Identity?.IsAuthenticated == true)
            {
                // Get user role
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                _logger.LogInformation($"Authenticated user with role: {role}");

                // Redirect based on role
                return role switch
                {
                    "Admin" => RedirectToPage("/Admin/Dashboard"),
                    "Owner" => RedirectToPage("/Owner/Dashboard"),
                    "User" => RedirectToPage("/User/Dashboard"),
                    _ => Page()
                };
            }

            // Show home page for unauthenticated users
            return Page();
        }
    }
}
