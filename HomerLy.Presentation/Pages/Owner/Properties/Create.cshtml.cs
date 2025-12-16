using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.PropertyDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Owner.Properties
{
    [Authorize(Roles = "Owner")]
    public class CreateModel : PageModel
    {
        private readonly IPropertyService _propertyService;
        private readonly IAccountService _accountService;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            IPropertyService propertyService,
            IAccountService accountService,
            ILogger<CreateModel> logger)
        {
            _propertyService = propertyService;
            _accountService = accountService;
            _logger = logger;
        }

        [BindProperty]
        public CreatePropertyDto Property { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading create property page: {ex.Message}");
                ErrorMessage = "An error occurred while loading the page.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                if (!ModelState.IsValid)
                {
                    ErrorMessage = "Please fill in all required fields correctly.";
                    return Page();
                }

                var result = await _propertyService.CreatePropertyAsync(ownerId, Property);

                if (result == null)
                {
                    ErrorMessage = "Failed to create property. Please try again.";
                    return Page();
                }

                _logger.LogInformation($"Property {result.Id} created successfully by owner {ownerId}");
                TempData["SuccessMessage"] = $"Property '{result.Title}' has been created successfully!";

                return RedirectToPage("/Owner/Properties/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating property: {ex.Message}");

                if (ex.Message.Contains("approved"))
                {
                    ErrorMessage = "Your owner account must be approved before creating properties.";
                }
                else if (ex.Message.Contains("Owner"))
                {
                    ErrorMessage = "Only approved owners can create properties.";
                }
                else
                {
                    ErrorMessage = $"An error occurred: {ex.Message}";
                }

                return Page();
            }
        }
    }
}
