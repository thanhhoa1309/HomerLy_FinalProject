using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.PropertyDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Owner.Properties
{
    [Authorize(Roles = "Owner")]
    public class EditModel : PageModel
    {
        private readonly IPropertyService _propertyService;
        private readonly ILogger<EditModel> _logger;

        public EditModel(IPropertyService propertyService, ILogger<EditModel> logger)
        {
            _propertyService = propertyService;
            _logger = logger;
        }

        [BindProperty]
        public UpdatePropertyDto PropertyUpdate { get; set; } = new();

        public PropertyResponseDto? Property { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                Property = await _propertyService.GetPropertyByIdAsync(id);

                if (Property == null)
                {
                    TempData["ErrorMessage"] = "Property not found.";
                    return RedirectToPage("/Owner/Properties/Index");
                }

                // Verify ownership
                if (Property.OwnerId != ownerId)
                {
                    TempData["ErrorMessage"] = "You can only edit your own properties.";
                    return RedirectToPage("/Owner/Properties/Index");
                }

                // Pre-populate form
                PropertyUpdate = new UpdatePropertyDto
                {
                    Title = Property.Title,
                    Description = Property.Description,
                    Address = Property.Address,
                    MonthlyPrice = Property.MonthlyPrice,
                    AreaSqm = Property.AreaSqm,
                    ImageUrl = Property.ImageUrl
                };

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading property {id} for edit: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading the property.";
                return RedirectToPage("/Owner/Properties/Index");
            }
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
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
                    // Reload property for display
                    Property = await _propertyService.GetPropertyByIdAsync(id);
                    ErrorMessage = "Please correct the errors below.";
                    return Page();
                }

                var result = await _propertyService.UpdatePropertyAsync(id, ownerId, PropertyUpdate);

                if (result == null)
                {
                    Property = await _propertyService.GetPropertyByIdAsync(id);
                    ErrorMessage = "Failed to update property. Please try again.";
                    return Page();
                }

                _logger.LogInformation($"Property {id} updated successfully by owner {ownerId}");
                TempData["SuccessMessage"] = $"Property '{result.Title}' has been updated successfully!";

                return RedirectToPage("/Owner/Properties/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating property {id}: {ex.Message}");

                Property = await _propertyService.GetPropertyByIdAsync(id);

                if (ex.Message.Contains("ownership") || ex.Message.Contains("own properties"))
                {
                    ErrorMessage = "You can only edit your own properties.";
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
