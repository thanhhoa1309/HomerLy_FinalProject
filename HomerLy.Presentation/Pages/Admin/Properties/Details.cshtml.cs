using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.PropertyDTOs;
using Homerly.BusinessObject.DTOs.UserDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Admin.Properties
{
    [Authorize(Roles = "Admin")]
    public class DetailsModel : PageModel
    {
        private readonly IPropertyService _propertyService;
        private readonly IAccountService _accountService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(
            IPropertyService propertyService,
            IAccountService accountService,
            ILogger<DetailsModel> logger)
        {
            _propertyService = propertyService;
            _accountService = accountService;
            _logger = logger;
        }

        public PropertyResponseDto? Property { get; set; }
        public AccountResponseDto? Owner { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var adminId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                Property = await _propertyService.GetPropertyByIdAsync(id);

                if (Property == null)
                {
                    ErrorMessage = "Property not found.";
                    return Page();
                }

                // Get owner information
                Owner = await _accountService.GetAccountByIdAsync(Property.OwnerId);

                SuccessMessage = TempData["SuccessMessage"] as string;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading property details: {ex.Message}");
                ErrorMessage = "An error occurred while loading property details.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id, Guid ownerId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var adminId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                var success = await _propertyService.DeletePropertyAsync(id, ownerId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Property deleted successfully!";
                    return RedirectToPage("/Admin/Properties/Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete property.";
                    return RedirectToPage(new { id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting property: {ex.Message}");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage(new { id });
            }
        }
    }
}