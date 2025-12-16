using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.PropertyDTOs;
using Homerly.BusinessObject.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Owner.Properties
{
    [Authorize(Roles = "Owner")]
    public class DetailsModel : PageModel
    {
        private readonly IPropertyService _propertyService;
        private readonly ITenancyService _tenancyService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(
            IPropertyService propertyService,
            ITenancyService tenancyService,
            ILogger<DetailsModel> logger)
        {
            _propertyService = propertyService;
            _tenancyService = tenancyService;
            _logger = logger;
        }

        public PropertyResponseDto? Property { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public bool IsOwner { get; set; }

        // Statistics
        public int TotalTenancies { get; set; }
        public int ActiveTenancies { get; set; }
        public decimal TotalRevenue { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                Property = await _propertyService.GetPropertyByIdAsync(id);

                if (Property == null)
                {
                    TempData["ErrorMessage"] = "Property not found.";
                    return RedirectToPage("/Owner/Properties/Index");
                }

                IsOwner = Property.OwnerId == userId;

                if (!IsOwner)
                {
                    TempData["ErrorMessage"] = "You can only view details of your own properties.";
                    return RedirectToPage("/Owner/Properties/Index");
                }

                // Get tenancies statistics
                var tenancies = await _tenancyService.GetTenanciesAsync(
                    userId: userId,
                    propertyId: id,
                    pageNumber: 1,
                    pageSize: int.MaxValue);

                TotalTenancies = tenancies.TotalCount;
                ActiveTenancies = tenancies.Count(t => t.Status == TenancyStatus.active);

                // Calculate total revenue from all tenancies
                TotalRevenue = tenancies
                    .Where(t => t.Status == TenancyStatus.active || t.Status == TenancyStatus.expired)
                    .Sum(t => CalculateTenancyRevenue(t.StartDate, t.EndDate, Property.MonthlyPrice));

                SuccessMessage = TempData["SuccessMessage"] as string;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading property details {id}: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading property details.";
                return RedirectToPage("/Owner/Properties/Index");
            }
        }

        public async Task<IActionResult> OnPostChangeStatusAsync(Guid id, PropertyStatus newStatus)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                var success = await _propertyService.UpdatePropertyStatusAsync(id, ownerId, newStatus);

                if (success)
                {
                    TempData["SuccessMessage"] = $"Property status changed to {newStatus} successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update property status.";
                }

                return RedirectToPage(new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error changing property status: {ex.Message}");
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToPage(new { id });
            }
        }

        private decimal CalculateTenancyRevenue(DateTime startDate, DateTime endDate, decimal monthlyPrice)
        {
            var months = ((endDate.Year - startDate.Year) * 12) + endDate.Month - startDate.Month;
            if (months < 1) months = 1;
            return months * monthlyPrice;
        }
    }
}
