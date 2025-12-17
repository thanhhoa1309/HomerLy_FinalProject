using Homerly.Business.Interfaces;
using Homerly.Business.Utils;
using Homerly.BusinessObject.DTOs.PropertyDTOs;
using Homerly.BusinessObject.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Owner.Properties
{
    [Authorize(Roles = "Owner")]
    public class IndexModel : PageModel
    {
        private readonly IPropertyService _propertyService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IPropertyService propertyService, ILogger<IndexModel> logger)
        {
            _propertyService = propertyService;
            _logger = logger;
        }

        public Pagination<PropertyResponseDto>? Properties { get; set; }
        public string? StatusFilter { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public int AvailableCount { get; set; }
        public int OccupiedCount { get; set; }

        public async Task<IActionResult> OnGetAsync(
            int pageNumber = 1,
            string? status = null,
            decimal? minPrice = null,
            decimal? maxPrice = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                StatusFilter = status;
                MinPrice = minPrice;
                MaxPrice = maxPrice;

                PropertyStatus? propertyStatus = null;
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<PropertyStatus>(status, out var parsedStatus))
                {
                    propertyStatus = parsedStatus;
                }

                // Get filtered properties
                Properties = await _propertyService.GetPropertiesAsync(
                    pageNumber: pageNumber,
                    pageSize: 9,
                    ownerId: ownerId,
                    status: propertyStatus,
                    minRent: minPrice,
                    maxRent: maxPrice);

                // Get statistics - using correct enum values
                var availableProps = await _propertyService.GetPropertiesAsync(
                    pageNumber: 1,
                    pageSize: int.MaxValue,
                    ownerId: ownerId,
                    status: PropertyStatus.available);
                AvailableCount = availableProps.TotalCount;

                var occupiedProps = await _propertyService.GetPropertiesAsync(
                    pageNumber: 1,
                    pageSize: int.MaxValue,
                    ownerId: ownerId,
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
    }
}
