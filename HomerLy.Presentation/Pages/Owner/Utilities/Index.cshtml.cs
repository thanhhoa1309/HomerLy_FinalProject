using Homerly.Business.Interfaces;
using Homerly.Business.Utils;
using Homerly.BusinessObject.DTOs.InvoiceDTOs;
using Homerly.BusinessObject.DTOs.PropertyDTOs;
using Homerly.BusinessObject.DTOs.TenancyDTOs;
using Homerly.BusinessObject.DTOs.UtilityReadingDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Owner.Utilities
{
    [Authorize(Roles = "Owner")]
    public class IndexModel : PageModel
    {
        private readonly IUtilityReadingService _utilityReadingService;
        private readonly IPropertyService _propertyService;
        private readonly ITenancyService _tenancyService;
        private readonly IInvoiceService _invoiceService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            IUtilityReadingService utilityReadingService,
            IPropertyService propertyService,
            ITenancyService tenancyService,
            IInvoiceService invoiceService,
            ILogger<IndexModel> logger)
        {
            _utilityReadingService = utilityReadingService;
            _propertyService = propertyService;
            _tenancyService = tenancyService;
            _invoiceService = invoiceService;
            _logger = logger;
        }

        public Pagination<UtilityReadingResponseDto>? UtilityReadings { get; set; }
        public List<PropertyResponseDto>? OwnerProperties { get; set; }
        public Guid? PropertyIdFilter { get; set; }
        public bool? IsChargedFilter { get; set; }
        public DateTime? FromDateFilter { get; set; }
        public DateTime? ToDateFilter { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public int TotalReadings { get; set; }
        public int PendingCharge { get; set; }
        public int ChargedCount { get; set; }

        public async Task<IActionResult> OnGetAsync(
            int pageNumber = 1,
            Guid? propertyId = null,
            bool? isCharged = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                PropertyIdFilter = propertyId;
                IsChargedFilter = isCharged;
                FromDateFilter = fromDate;
                ToDateFilter = toDate;

                var allProperties = await _propertyService.GetPropertiesAsync(
                    pageNumber: 1,
                    pageSize: int.MaxValue,
                    ownerId: ownerId);
                OwnerProperties = allProperties.ToList();

                if (propertyId.HasValue)
                {
                    UtilityReadings = await _utilityReadingService.GetUtilityReadingsByPropertyIdAsync(
                        propertyId: propertyId.Value,
                        userId: ownerId,
                        pageNumber: pageNumber,
                        pageSize: 10,
                        isCharged: isCharged,
                        fromDate: fromDate,
                        toDate: toDate);
                }
                else
                {
                    var allReadings = new List<UtilityReadingResponseDto>();
                    foreach (var property in OwnerProperties ?? new List<PropertyResponseDto>())
                    {
                        try
                        {
                            var readings = await _utilityReadingService.GetUtilityReadingsByPropertyIdAsync(
                                propertyId: property.Id,
                                userId: ownerId,
                                pageNumber: 1,
                                pageSize: int.MaxValue,
                                isCharged: isCharged,
                                fromDate: fromDate,
                                toDate: toDate);
                            allReadings.AddRange(readings);
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    var totalCount = allReadings.Count;
                    var paginatedReadings = allReadings
                        .OrderByDescending(r => r.ReadingDate)
                        .Skip((pageNumber - 1) * 10)
                        .Take(10)
                        .ToList();

                    UtilityReadings = new Pagination<UtilityReadingResponseDto>(
                        paginatedReadings, totalCount, pageNumber, 10);
                }

                TotalReadings = UtilityReadings?.TotalCount ?? 0;
                PendingCharge = UtilityReadings?.Count(r => !r.IsCharged) ?? 0;
                ChargedCount = UtilityReadings?.Count(r => r.IsCharged) ?? 0;

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

        public async Task<IActionResult> OnPostCreateInvoiceAsync(Guid readingId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                var reading = await _utilityReadingService.GetUtilityReadingByIdAsync(readingId, ownerId);
                if (reading == null)
                {
                    TempData["ErrorMessage"] = "Utility reading not found.";
                    return RedirectToPage();
                }

                if (reading.IsCharged)
                {
                    TempData["ErrorMessage"] = "This utility reading has already been charged.";
                    return RedirectToPage();
                }

                var tenancy = await _tenancyService.GetTenancyByIdAsync(reading.TenancyId, ownerId);
                if (tenancy == null)
                {
                    TempData["ErrorMessage"] = "Tenancy not found.";
                    return RedirectToPage();
                }

                var createInvoiceDto = new CreateInvoiceDto
                {
                    TenancyId = reading.TenancyId,
                    BillingPeriodStart = reading.ReadingDate.AddMonths(-1),
                    BillingPeriodEnd = reading.ReadingDate,
                    DueDate = reading.ReadingDate.AddDays(7),     
                    ElectricNewIndex = reading.ElectricNewIndex,
                    WaterNewIndex = reading.WaterNewIndex,
                    ElectricUnitPrice = tenancy.ElectricUnitPrice,
                    WaterUnitPrice = tenancy.WaterUnitPrice,
                    OtherFees = 0    
                };

                var invoice = await _invoiceService.CreateInvoiceAsync(ownerId, createInvoiceDto);

                await _utilityReadingService.MarkAsChargedAsync(readingId, ownerId);

                TempData["SuccessMessage"] = $"Invoice created successfully! Invoice ID: {invoice.Id}. Total Amount: ₫{invoice.TotalAmount:N0}";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating invoice from utility reading {readingId}: {ex.Message}");
                TempData["ErrorMessage"] = $"Error creating invoice: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostMarkChargedAsync(Guid readingId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                var success = await _utilityReadingService.MarkAsChargedAsync(readingId, ownerId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Utility reading marked as charged successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to mark utility reading as charged.";
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error marking as charged: {ex.Message}");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid readingId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                var success = await _utilityReadingService.DeleteUtilityReadingAsync(readingId, ownerId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Utility reading deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete utility reading.";
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting utility reading: {ex.Message}");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage();
            }
        }
    }
}