using Homerly.Business.Interfaces;
using Homerly.Business.Utils;
using Homerly.BusinessObject.DTOs.TenancyDTOs;
using Homerly.BusinessObject.DTOs.UtilityReadingDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.User.Utilities
{
    [Authorize(Policy = "UserPolicy")]
    public class IndexModel : PageModel
    {
        private readonly IUtilityReadingService _utilityReadingService;
        private readonly ITenancyService _tenancyService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            IUtilityReadingService utilityReadingService,
            ITenancyService tenancyService,
            ILogger<IndexModel> logger)
        {
            _utilityReadingService = utilityReadingService;
            _tenancyService = tenancyService;
            _logger = logger;
        }

        public Pagination<UtilityReadingResponseDto>? UtilityReadings { get; set; }
        public List<TenancyResponseDto>? MyTenancies { get; set; }
        public Guid? TenancyIdFilter { get; set; }
        public DateTime? FromDateFilter { get; set; }
        public DateTime? ToDateFilter { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        // Statistics
        public int TotalReadings { get; set; }
        public int TotalElectricUsage { get; set; }
        public int TotalWaterUsage { get; set; }
        public UtilityReadingResponseDto? LatestReading { get; set; }

        public async Task<IActionResult> OnGetAsync(
            int pageNumber = 1,
            Guid? tenancyId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                TenancyIdFilter = tenancyId;
                FromDateFilter = fromDate;
                ToDateFilter = toDate;

                // Get tenant's tenancies (active and pending)
                var tenancies = await _tenancyService.GetTenanciesByTenantIdAsync(
                    tenantId: userId,
                    userId: userId,
                    pageNumber: 1,
                    pageSize: int.MaxValue);
                MyTenancies = tenancies.ToList();

                // Get utility readings
                if (tenancyId.HasValue)
                {
                    // Get readings for specific tenancy
                    UtilityReadings = await _utilityReadingService.GetUtilityReadingsByTenancyIdAsync(
                        tenancyId: tenancyId.Value,
                        userId: userId,
                        pageNumber: pageNumber,
                        pageSize: 10,
                        isCharged: null,
                        fromDate: fromDate,
                        toDate: toDate);

                    // Get latest reading for this tenancy
                    var tenancy = MyTenancies?.FirstOrDefault(t => t.Id == tenancyId.Value);
                    if (tenancy != null)
                    {
                        try
                        {
                            LatestReading = await _utilityReadingService.GetLatestUtilityReadingAsync(
                                tenancy.PropertyId, userId);
                        }
                        catch
                        {
                            LatestReading = null;
                        }
                    }
                }
                else
                {
                    // Get all readings from all tenancies
                    var allReadings = new List<UtilityReadingResponseDto>();
                    foreach (var tenancy in MyTenancies ?? new List<TenancyResponseDto>())
                    {
                        try
                        {
                            var readings = await _utilityReadingService.GetUtilityReadingsByTenancyIdAsync(
                                tenancyId: tenancy.Id,
                                userId: userId,
                                pageNumber: 1,
                                pageSize: int.MaxValue,
                                isCharged: null,
                                fromDate: fromDate,
                                toDate: toDate);
                            allReadings.AddRange(readings);
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    // Apply pagination manually
                    var totalCount = allReadings.Count;
                    var paginatedReadings = allReadings
                        .OrderByDescending(r => r.ReadingDate)
                        .Skip((pageNumber - 1) * 10)
                        .Take(10)
                        .ToList();

                    UtilityReadings = new Pagination<UtilityReadingResponseDto>(
                        paginatedReadings, totalCount, pageNumber, 10);
                }

                // Calculate statistics
                if (UtilityReadings != null && UtilityReadings.Any())
                {
                    TotalReadings = UtilityReadings.TotalCount;

                    // Calculate total usage from all readings
                    var allReadingsForStats = UtilityReadings.ToList();
                    TotalElectricUsage = allReadingsForStats.Sum(r => r.ElectricUsage);
                    TotalWaterUsage = allReadingsForStats.Sum(r => r.WaterUsage);
                }

                SuccessMessage = TempData["SuccessMessage"] as string;
                ErrorMessage = TempData["ErrorMessage"] as string;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading utility readings: {ex.Message}");
                ErrorMessage = "An error occurred while loading utility readings.";
                return Page();
            }
        }
    }
}