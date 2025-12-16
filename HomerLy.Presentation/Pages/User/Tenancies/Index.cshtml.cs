using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.TenancyDTOs;
using Homerly.BusinessObject.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.User.Tenancies
{
    [Authorize(Roles = "User")]
    public class IndexModel : PageModel
    {
        private readonly ITenancyService _tenancyService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ITenancyService tenancyService, ILogger<IndexModel> logger)
        {
            _tenancyService = tenancyService;
            _logger = logger;
        }

        public List<TenancyResponseDto> ActiveTenancies { get; set; } = new List<TenancyResponseDto>();
        public List<TenancyResponseDto> HistoryTenancies { get; set; } = new List<TenancyResponseDto>();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                // Get Active Tenancies (Active + Pending)
                var activeResult = await _tenancyService.GetTenanciesAsync(
                    userId: userId,
                    tenantId: userId,
                    pageNumber: 1,
                    pageSize: 50, // Get enough for simple list
                    status: TenancyStatus.active
                );

                var pendingResult = await _tenancyService.GetTenanciesAsync(
                    userId: userId,
                    tenantId: userId,
                    pageNumber: 1,
                    pageSize: 50,
                    status: TenancyStatus.pending_confirmation
                );

                ActiveTenancies = activeResult.Concat(pendingResult).OrderByDescending(t => t.StartDate).ToList();

                // Get History (Expired + Cancelled)
                var expiredResult = await _tenancyService.GetTenanciesAsync(
                    userId: userId,
                    tenantId: userId,
                    pageNumber: 1,
                    pageSize: 50,
                    status: TenancyStatus.expired
                );

                var cancelledResult = await _tenancyService.GetTenanciesAsync(
                    userId: userId,
                    tenantId: userId,
                    pageNumber: 1,
                    pageSize: 50,
                    status: TenancyStatus.cancelled
                );

                HistoryTenancies = expiredResult.Concat(cancelledResult).OrderByDescending(t => t.EndDate).ToList();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user tenancies");
                return Page();
            }
        }
    }
}
