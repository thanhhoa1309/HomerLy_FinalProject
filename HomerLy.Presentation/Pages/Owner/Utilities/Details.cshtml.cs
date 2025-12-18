using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.TenancyDTOs;
using Homerly.BusinessObject.DTOs.UtilityReadingDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Owner.Utilities
{
    [Authorize(Roles = "Owner")]
    public class DetailsModel : PageModel
    {
        private readonly IUtilityReadingService _utilityReadingService;
        private readonly ITenancyService _tenancyService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(
            IUtilityReadingService utilityReadingService,
            ITenancyService tenancyService,
            ILogger<DetailsModel> logger)
        {
            _utilityReadingService = utilityReadingService;
            _tenancyService = tenancyService;
            _logger = logger;
        }

        public UtilityReadingResponseDto? UtilityReading { get; set; }
        public TenancyResponseDto? Tenancy { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        // Previous and Next readings for navigation
        public UtilityReadingResponseDto? PreviousReading { get; set; }
        public UtilityReadingResponseDto? NextReading { get; set; }

        // Calculated costs
        public decimal EstimatedElectricCost { get; set; }
        public decimal EstimatedWaterCost { get; set; }
        public decimal TotalEstimatedCost { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                // Get utility reading
                UtilityReading = await _utilityReadingService.GetUtilityReadingByIdAsync(id, ownerId);

                if (UtilityReading == null)
                {
                    ErrorMessage = "Utility reading not found.";
                    return Page();
                }

                // Get tenancy information
                Tenancy = await _tenancyService.GetTenancyByIdAsync(UtilityReading.TenancyId, ownerId);

                // Calculate estimated costs
                if (Tenancy != null)
                {
                    EstimatedElectricCost = UtilityReading.ElectricUsage * Tenancy.ElectricUnitPrice;
                    EstimatedWaterCost = UtilityReading.WaterUsage * Tenancy.WaterUnitPrice;
                    TotalEstimatedCost = EstimatedElectricCost + EstimatedWaterCost;
                }

                // Get previous and next readings for navigation
                try
                {
                    var allReadings = await _utilityReadingService.GetUtilityReadingsByTenancyIdAsync(
                        tenancyId: UtilityReading.TenancyId,
                        userId: ownerId,
                        pageNumber: 1,
                        pageSize: int.MaxValue);

                    var readingsList = allReadings.OrderBy(r => r.ReadingDate).ToList();
                    var currentIndex = readingsList.FindIndex(r => r.Id == id);

                    if (currentIndex > 0)
                    {
                        PreviousReading = readingsList[currentIndex - 1];
                    }

                    if (currentIndex >= 0 && currentIndex < readingsList.Count - 1)
                    {
                        NextReading = readingsList[currentIndex + 1];
                    }
                }
                catch
                {
                    // Navigation readings are optional
                }

                SuccessMessage = TempData["SuccessMessage"] as string;
                ErrorMessage ??= TempData["ErrorMessage"] as string;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading utility reading details: {ex.Message}");
                ErrorMessage = "An error occurred while loading utility reading details.";
                return Page();
            }
        }
    }
}