using Homerly.Business.Interfaces;
using Homerly.Business.Utils;
using Homerly.BusinessObject.DTOs.PropertyDTOs;
using Homerly.BusinessObject.DTOs.TenancyDTOs;
using Homerly.BusinessObject.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Owner.Tenancies
{
    [Authorize(Roles = "Owner")]
    public class IndexModel : PageModel
    {
        private readonly ITenancyService _tenancyService;
        private readonly IPropertyService _propertyService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            ITenancyService tenancyService,
            IPropertyService propertyService,
            ILogger<IndexModel> logger)
        {
            _tenancyService = tenancyService;
            _propertyService = propertyService;
            _logger = logger;
        }

        public Pagination<TenancyResponseDto>? Tenancies { get; set; }
        public List<PropertyResponseDto>? OwnerProperties { get; set; }
        public string? StatusFilter { get; set; }
        public Guid? PropertyIdFilter { get; set; }
        public bool? IsTenantConfirmedFilter { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public int ActiveCount { get; set; }
        public int PendingCount { get; set; }
        public int ExpiredCount { get; set; }

        public async Task<IActionResult> OnGetAsync(
            int pageNumber = 1,
            string? status = null,
            Guid? propertyId = null,
            bool? isTenantConfirmed = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                StatusFilter = status;
                PropertyIdFilter = propertyId;
                IsTenantConfirmedFilter = isTenantConfirmed;

                TenancyStatus? tenancyStatus = null;
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<TenancyStatus>(status, out var parsedStatus))
                {
                    tenancyStatus = parsedStatus;
                }

                // Get owner's properties for filter dropdown
                var allProperties = await _propertyService.GetPropertiesAsync(
                    pageNumber: 1,
                    pageSize: int.MaxValue,
                    ownerId: ownerId);
                OwnerProperties = allProperties.ToList();

                // Get filtered tenancies
                Tenancies = await _tenancyService.GetTenanciesByOwnerIdAsync(
                    ownerId: ownerId,
                    userId: ownerId,
                    pageNumber: pageNumber,
                    pageSize: 10,
                    status: tenancyStatus);

                // Apply additional filters
                if (propertyId.HasValue || isTenantConfirmed.HasValue)
                {
                    var allTenancies = await _tenancyService.GetTenanciesAsync(
                        userId: ownerId,
                        pageNumber: pageNumber,
                        pageSize: 10,
                        propertyId: propertyId,
                        ownerId: ownerId,
                        status: tenancyStatus,
                        isTenantConfirmed: isTenantConfirmed);
                    Tenancies = allTenancies;
                }

                // Get statistics - using correct enum values
                var active = await _tenancyService.GetTenanciesByOwnerIdAsync(
                    ownerId: ownerId,
                    userId: ownerId,
                    pageNumber: 1,
                    pageSize: int.MaxValue,
                    status: TenancyStatus.active);
                ActiveCount = active.TotalCount;

                var pending = await _tenancyService.GetTenanciesByOwnerIdAsync(
                    ownerId: ownerId,
                    userId: ownerId,
                    pageNumber: 1,
                    pageSize: int.MaxValue,
                    status: TenancyStatus.pending_confirmation);
                PendingCount = pending.TotalCount;

                var expired = await _tenancyService.GetTenanciesByOwnerIdAsync(
                    ownerId: ownerId,
                    userId: ownerId,
                    pageNumber: 1,
                    pageSize: int.MaxValue,
                    status: TenancyStatus.expired);
                ExpiredCount = expired.TotalCount;

                SuccessMessage = TempData["SuccessMessage"] as string;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading tenancies: {ex.Message}");
                ErrorMessage = "An error occurred while loading tenancies.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCancelAsync(Guid tenancyId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                var success = await _tenancyService.CancelTenancyAsync(tenancyId, ownerId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Tenancy cancelled successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to cancel tenancy.";
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cancelling tenancy: {ex.Message}");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage();
            }
        }
    }
}
