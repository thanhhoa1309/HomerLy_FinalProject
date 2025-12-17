using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.TenancyDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Admin.Tenancies
{
    [Authorize(Roles = "Admin")]
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
        public int DaysRemaining { get; set; }
        public int TotalDays { get; set; }
        public decimal ProgressPercentage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var adminId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                Tenancy = await _tenancyService.GetTenancyByIdAsync(id, adminId);

                if (Tenancy == null)
                {
                    ErrorMessage = "Tenancy not found.";
                    return Page();
                }

                // Calculate days remaining and progress
                TotalDays = (Tenancy.EndDate - Tenancy.StartDate).Days;
                var daysElapsed = (DateTime.Now - Tenancy.StartDate).Days;
                DaysRemaining = Math.Max(0, (Tenancy.EndDate - DateTime.Now).Days);
                ProgressPercentage = TotalDays > 0 ? Math.Min(100, (decimal)daysElapsed / TotalDays * 100) : 0;

                SuccessMessage = TempData["SuccessMessage"] as string;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading tenancy details: {ex.Message}");
                ErrorMessage = "An error occurred while loading tenancy details.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostApproveAsync(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var adminId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                var result = await _tenancyService.UpdateTenancyStatusAsync(
                    id,
                    adminId,
                    Homerly.BusinessObject.Enums.TenancyStatus.active);

                if (result != null)
                {
                    TempData["SuccessMessage"] = "Tenancy approved successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to approve tenancy.";
                }

                return RedirectToPage(new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error approving tenancy: {ex.Message}");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage(new { id });
            }
        }

        public async Task<IActionResult> OnPostRejectAsync(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var adminId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                var result = await _tenancyService.UpdateTenancyStatusAsync(
                    id,
                    adminId,
                    Homerly.BusinessObject.Enums.TenancyStatus.cancelled);

                if (result != null)
                {
                    TempData["SuccessMessage"] = "Tenancy rejected successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to reject tenancy.";
                }

                return RedirectToPage(new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error rejecting tenancy: {ex.Message}");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage(new { id });
            }
        }
    }
}