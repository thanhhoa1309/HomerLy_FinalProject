using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.UtilityReadingDTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.UtilityReadings
{

    public class UtilityReadingsModel : PageModel
    {
        private readonly IUtilityReadingService _utilityReadingService;
        private readonly ILogger<UtilityReadingsModel> _logger;

        public UtilityReadingsModel(
            IUtilityReadingService utilityReadingService,
            ILogger<UtilityReadingsModel> logger)
        {
            _utilityReadingService = utilityReadingService;
            _logger = logger;
        }

        [BindProperty]
        public CreateUtilityReadingDto CreateDto { get; set; } = new();

        public List<UtilityReadingResponseDto> Readings { get; set; } = new();
        public UtilityReadingResponseDto? LatestReading { get; set; }


        public async Task<IActionResult> OnGetAsync(Guid propertyId, int pageNumber = 1)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);


                var result = await _utilityReadingService.GetUtilityReadingsByPropertyIdAsync(
                    propertyId: propertyId,
                    userId: userId,
                    pageNumber: pageNumber,
                    pageSize: 10,
                    isCharged: null,
                    fromDate: null,
                    toDate: null
                );

                Readings = result.ToList();

                LatestReading = await _utilityReadingService.GetLatestUtilityReadingAsync(propertyId, userId);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading utility readings: {ex.Message}");
                TempData["ErrorMessage"] = GetErrorMessage(ex);
                return Page();
            }
        }


        public async Task<IActionResult> OnPostCreateAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Page();
                }

                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var result = await _utilityReadingService.CreateUtilityReadingAsync(userId, CreateDto);

                TempData["SuccessMessage"] = "Utility reading created successfully!";
                return RedirectToPage("/UtilityReadings/Index", new { propertyId = CreateDto.PropertyId });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating utility reading: {ex.Message}");
                TempData["ErrorMessage"] = GetErrorMessage(ex);
                return Page();
            }
        }


        public async Task<IActionResult> OnPostUpdateAsync(Guid readingId, int electricNewIndex, int waterNewIndex)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var updateDto = new UpdateUtilityReadingDto
                {
                    ElectricNewIndex = electricNewIndex,
                    WaterNewIndex = waterNewIndex
                };

                var result = await _utilityReadingService.UpdateUtilityReadingAsync(readingId, userId, updateDto);

                TempData["SuccessMessage"] = "Utility reading updated successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating utility reading: {ex.Message}");
                TempData["ErrorMessage"] = GetErrorMessage(ex);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostMarkChargedAsync(Guid readingId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                await _utilityReadingService.MarkAsChargedAsync(readingId, userId);

                TempData["SuccessMessage"] = "Utility reading marked as charged!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error marking as charged: {ex.Message}");
                TempData["ErrorMessage"] = GetErrorMessage(ex);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid readingId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                await _utilityReadingService.DeleteUtilityReadingAsync(readingId, userId);

                TempData["SuccessMessage"] = "Utility reading deleted successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting utility reading: {ex.Message}");
                TempData["ErrorMessage"] = GetErrorMessage(ex);
                return Page();
            }
        }

        private string GetErrorMessage(Exception ex)
        {
            if (ex.Data.Contains("StatusCode"))
            {
                return ex.Message;
            }
            return "An error occurred. Please try again.";
        }
    }
}

