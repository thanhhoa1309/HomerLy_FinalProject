using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.PropertyDTOs;
using Homerly.BusinessObject.DTOs.TenancyDTOs;
using Homerly.BusinessObject.DTOs.UtilityReadingDTOs;
using Homerly.BusinessObject.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Owner.Utilities
{
    [Authorize(Roles = "Owner")]
    public class CreateModel : PageModel
    {
        private readonly IUtilityReadingService _utilityReadingService;
        private readonly IPropertyService _propertyService;
        private readonly ITenancyService _tenancyService;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            IUtilityReadingService utilityReadingService,
            IPropertyService propertyService,
            ITenancyService tenancyService,
            ILogger<CreateModel> logger)
        {
            _utilityReadingService = utilityReadingService;
            _propertyService = propertyService;
            _tenancyService = tenancyService;
            _logger = logger;
        }

        [BindProperty]
        public CreateUtilityReadingDto CreateDto { get; set; } = new();

        public List<PropertyResponseDto>? OwnerProperties { get; set; }
        public List<TenancyResponseDto>? PropertyTenancies { get; set; }
        public UtilityReadingResponseDto? LatestReading { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? propertyId = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                // Get owner's properties
                var allProperties = await _propertyService.GetPropertiesAsync(
                    pageNumber: 1,
                    pageSize: int.MaxValue,
                    ownerId: ownerId);
                OwnerProperties = allProperties.ToList();

                // Pre-select property if provided
                if (propertyId.HasValue)
                {
                    CreateDto.PropertyId = propertyId.Value;
                    await LoadPropertyData(propertyId.Value, ownerId);
                }

                // Set default reading date to today
                CreateDto.ReadingDate = DateTime.Now;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading create utility reading page: {ex.Message}");
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
                    // Reload properties for dropdown
                    var allProperties = await _propertyService.GetPropertiesAsync(
                        pageNumber: 1,
                        pageSize: int.MaxValue,
                        ownerId: ownerId);
                    OwnerProperties = allProperties.ToList();

                    if (CreateDto.PropertyId != Guid.Empty)
                    {
                        await LoadPropertyData(CreateDto.PropertyId, ownerId);
                    }

                    return Page();
                }

                // Create utility reading
                var result = await _utilityReadingService.CreateUtilityReadingAsync(ownerId, CreateDto);

                if (result != null)
                {
                    TempData["SuccessMessage"] = "Utility reading created successfully!";
                    return RedirectToPage("/Owner/Utilities/Index");
                }
                else
                {
                    ErrorMessage = "Failed to create utility reading.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating utility reading: {ex.Message}");
                ErrorMessage = ex.Message;

                // Reload properties
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var ownerId))
                {
                    var allProperties = await _propertyService.GetPropertiesAsync(
                        pageNumber: 1,
                        pageSize: int.MaxValue,
                        ownerId: ownerId);
                    OwnerProperties = allProperties.ToList();

                    if (CreateDto.PropertyId != Guid.Empty)
                    {
                        await LoadPropertyData(CreateDto.PropertyId, ownerId);
                    }
                }

                return Page();
            }
        }

        public async Task<IActionResult> OnGetLoadPropertyDataAsync(Guid propertyId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerId))
                {
                    return new JsonResult(new { success = false, message = "Unauthorized" });
                }

                await LoadPropertyData(propertyId, ownerId);

                return new JsonResult(new
                {
                    success = true,
                    tenancies = PropertyTenancies?.Select(t => new
                    {
                        id = t.Id,
                        text = $"{t.PropertyTitle} - {t.TenantName} ({t.StartDate:MMM dd, yyyy} - {t.EndDate:MMM dd, yyyy})"
                    }),
                    latestReading = LatestReading != null ? new
                    {
                        electricOldIndex = LatestReading.ElectricNewIndex,
                        waterOldIndex = LatestReading.WaterNewIndex,
                        readingDate = LatestReading.ReadingDate.ToString("yyyy-MM-dd")
                    } : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading property data: {ex.Message}");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        private async Task LoadPropertyData(Guid propertyId, Guid ownerId)
        {
            // Get active tenancies for this property using GetTenanciesAsync with propertyId filter
            var tenancies = await _tenancyService.GetTenanciesAsync(
                userId: ownerId,
                pageNumber: 1,
                pageSize: int.MaxValue,
                propertyId: propertyId,
                ownerId: ownerId,
                status: TenancyStatus.active, // Only get active tenancies
                isTenantConfirmed: true); // Only confirmed tenancies
            PropertyTenancies = tenancies.ToList();

            // Get latest reading
            try
            {
                LatestReading = await _utilityReadingService.GetLatestUtilityReadingAsync(propertyId, ownerId);
            }
            catch
            {
                LatestReading = null;
            }
        }
    }
}