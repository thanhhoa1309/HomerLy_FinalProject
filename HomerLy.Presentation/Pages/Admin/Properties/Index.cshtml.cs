using Homerly.Business.Interfaces;
using Homerly.Business.Utils;
using Homerly.BusinessObject.DTOs.PropertyDTOs;
using Homerly.BusinessObject.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Admin.Properties
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IPropertyService _propertyService;
        private readonly IAccountService _accountService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            IPropertyService propertyService,
            IAccountService accountService,
            ILogger<IndexModel> logger)
        {
            _propertyService = propertyService;
            _accountService = accountService;
            _logger = logger;
        }

        public List<PropertyWithOwnerDto> Properties { get; set; } = new();
        public Pagination<PropertyResponseDto>? PropertiesPagination { get; set; }
        public string? StatusFilter { get; set; }
        public string? SearchQuery { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public int TotalProperties { get; set; }
        public int AvailableCount { get; set; }
        public int OccupiedCount { get; set; }

        public async Task<IActionResult> OnGetAsync(
            int pageNumber = 1,
            string? status = null,
            string? search = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var adminId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                StatusFilter = status;
                SearchQuery = search;

                PropertyStatus? propertyStatus = null;
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<PropertyStatus>(status, out var parsedStatus))
                {
                    propertyStatus = parsedStatus;
                }

                // Get filtered properties
                PropertiesPagination = await _propertyService.GetPropertiesAsync(
                    pageNumber: pageNumber,
                    pageSize: 10,
                    searchTerm: search,
                    status: propertyStatus);

                // Enrich properties with owner information
                Properties = new List<PropertyWithOwnerDto>();
                foreach (var property in PropertiesPagination)
                {
                    var owner = await _accountService.GetAccountByIdAsync(property.OwnerId);
                    Properties.Add(new PropertyWithOwnerDto
                    {
                        Property = property,
                        OwnerName = owner?.FullName ?? "Unknown",
                        OwnerEmail = owner?.Email ?? "N/A"
                    });
                }

                // Get statistics
                var allProperties = await _propertyService.GetPropertiesAsync(
                    pageNumber: 1,
                    pageSize: int.MaxValue);
                TotalProperties = allProperties.TotalCount;

                var availableProps = await _propertyService.GetPropertiesAsync(
                    pageNumber: 1,
                    pageSize: int.MaxValue,
                    status: PropertyStatus.available);
                AvailableCount = availableProps.TotalCount;

                var occupiedProps = await _propertyService.GetPropertiesAsync(
                    pageNumber: 1,
                    pageSize: int.MaxValue,
                    status: PropertyStatus.occupied);
                OccupiedCount = occupiedProps.TotalCount;

                SuccessMessage = TempData["SuccessMessage"] as string;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading properties: {ex.Message}");
                ErrorMessage = "An error occurred while loading properties.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid propertyId, Guid ownerId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var adminId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                var success = await _propertyService.DeletePropertyAsync(propertyId, ownerId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Property deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete property.";
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting property: {ex.Message}");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage();
            }
        }

        public class PropertyWithOwnerDto
        {
            public PropertyResponseDto Property { get; set; } = null!;
            public string OwnerName { get; set; } = string.Empty;
            public string OwnerEmail { get; set; } = string.Empty;
        }
    }
}