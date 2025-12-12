using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.UserDTOs;
using Homerly.BusinessObject.DTOs.PropertyDTOs;
using Homerly.BusinessObject.DTOs.TenancyDTOs;
using Homerly.BusinessObject.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Owner
{
    [Authorize(Roles = "Owner")]
    public class DashboardModel : PageModel
    {
        private readonly IAccountService _accountService;
        private readonly IPropertyService _propertyService;
        private readonly ITenancyService _tenancyService;
        private readonly ILogger<DashboardModel> _logger;

        public DashboardModel(
            IAccountService accountService,
            IPropertyService propertyService,
            ITenancyService tenancyService,
            ILogger<DashboardModel> logger)
        {
            _accountService = accountService;
            _propertyService = propertyService;
            _tenancyService = tenancyService;
            _logger = logger;
        }

        public AccountResponseDto? CurrentUser { get; set; }
        public string? ErrorMessage { get; set; }

        // Statistics
        public int TotalProperties { get; set; }
        public int OccupiedProperties { get; set; }
        public int ActiveTenancies { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int PendingTenancies { get; set; }
        public int UnchargedUtilities { get; set; }
        public int OpenReports { get; set; }

        // Recent Data
        public List<PropertyResponseDto> RecentProperties { get; set; } = new();
        public List<TenancyResponseDto> RecentTenancies { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                CurrentUser = await _accountService.GetCurrentAccountProfileAsync(userId);

                if (CurrentUser == null)
                {
                    ErrorMessage = "Unable to load owner profile.";
                    return Page();
                }

                // Get properties statistics
                var allProperties = await _propertyService.GetPropertiesAsync(
                    pageNumber: 1,
                    pageSize: int.MaxValue,
                    ownerId: userId);
                TotalProperties = allProperties.TotalCount;

                var occupiedProps = await _propertyService.GetPropertiesAsync(
                    pageNumber: 1,
                    pageSize: int.MaxValue,
                    ownerId: userId,
                    status: PropertyStatus.occupied);
                OccupiedProperties = occupiedProps.TotalCount;

                // Get tenancies statistics
                var activeTenancies = await _tenancyService.GetTenanciesByOwnerIdAsync(
                    ownerId: userId,
                    userId: userId,
                    pageNumber: 1,
                    pageSize: int.MaxValue,
                    status: TenancyStatus.active);
                ActiveTenancies = activeTenancies.TotalCount;

                var pendingTenancies = await _tenancyService.GetTenanciesByOwnerIdAsync(
                    ownerId: userId,
                    userId: userId,
                    pageNumber: 1,
                    pageSize: int.MaxValue,
                    status: TenancyStatus.pending_confirmation);
                PendingTenancies = pendingTenancies.TotalCount;

                // Calculate monthly revenue from active tenancies
                MonthlyRevenue = 0;
                foreach (var tenancy in activeTenancies)
                {
                    var property = await _propertyService.GetPropertyByIdAsync(tenancy.PropertyId);
                    if (property != null)
                    {
                        MonthlyRevenue += property.MonthlyRent;
                    }
                }

                // Get recent properties
                var recentProps = await _propertyService.GetPropertiesAsync(
                    pageNumber: 1,
                    pageSize: 6,
                    ownerId: userId);
                RecentProperties = recentProps.ToList();

                // Get recent tenancies
                var recentTens = await _tenancyService.GetTenanciesByOwnerIdAsync(
                    ownerId: userId,
                    userId: userId,
                    pageNumber: 1,
                    pageSize: 5);
                RecentTenancies = recentTens.ToList();

                // Placeholders for features not yet implemented
                UnchargedUtilities = 0;
                OpenReports = 0;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading owner dashboard: {ex.Message}");
                ErrorMessage = "An error occurred while loading your dashboard.";
                return Page();
            }
        }
    }
}
