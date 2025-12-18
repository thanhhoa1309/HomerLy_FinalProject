using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.TenancyDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.User.Tenancies
{
    [Authorize(Policy = "UserPolicy")]
    public class DetailsModel : PageModel
    {
        private readonly ITenancyService _tenancyService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(ITenancyService tenancyService, ILogger<DetailsModel> logger)
        {
            _tenancyService = tenancyService;
            _logger = logger;
        }

        public TenancyResponseDto? Tenancy { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var tenancy = await _tenancyService.GetTenancyByIdAsync(id, userId);

                if (tenancy == null)
                {
                    ErrorMessage = "Tenancy not found.";
                    return Page();
                }

                // Verify this tenancy belongs to current user
                if (tenancy.TenantId != userId)
                {
                    ErrorMessage = "You are not authorized to view this tenancy.";
                    return Page();
                }

                Tenancy = tenancy;

                // Get messages from TempData
                SuccessMessage = TempData["SuccessMessage"] as string;
                ErrorMessage = TempData["ErrorMessage"] as string;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading tenancy {id}");
                ErrorMessage = "An error occurred while loading the tenancy.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostConfirmAsync(Guid id)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // Confirm tenancy
                var result = await _tenancyService.TenantConfirmTenancyAsync(id, userId);

                if (result == null)
                {
                    TempData["ErrorMessage"] = "Failed to confirm tenancy.";
                    return RedirectToPage("/User/Tenancies/Details", new { id });
                }

                TempData["SuccessMessage"] = "Tenancy confirmed successfully!";
                return RedirectToPage("/User/Tenancies/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error confirming tenancy {id}: {ex.Message}");
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToPage("/User/Tenancies/Details", new { id });
            }
        }

        public async Task<IActionResult> OnPostRejectAsync(Guid id)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // Cancel/Reject tenancy
                var result = await _tenancyService.CancelTenancyAsync(id, userId);

                if (!result)
                {
                    TempData["ErrorMessage"] = "Failed to reject tenancy.";
                    return RedirectToPage("/User/Tenancies/Details", new { id });
                }

                TempData["SuccessMessage"] = "Tenancy rejected successfully.";
                return RedirectToPage("/User/Tenancies/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting tenancy {id}: {ex.Message}");
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToPage("/User/Tenancies/Details", new { id });
            }
        }
    }
}