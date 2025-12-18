using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.TenancyDTOs;
using Homerly.BusinessObject.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Owner.Tenancies
{
    [Authorize(Roles = "Owner")]
    public class DetailsModel : PageModel
    {
        private readonly ITenancyService _tenancyService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(ITenancyService tenancyService, ILogger<DetailsModel> logger)
        {
            _tenancyService = tenancyService;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public Guid Id { get; set; }

        public TenancyResponseDto? Tenancy { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        // For statistics and progress
        public int DaysRemaining { get; set; }
        public int TotalDays { get; set; }
        public double ProgressPercentage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                Id = id;
                Tenancy = await _tenancyService.GetTenancyByIdAsync(id, ownerId);

                if (Tenancy == null || Tenancy.OwnerId != ownerId)
                {
                    ErrorMessage = "Tenancy not found or you do not have access.";
                    return Page();
                }

                // Calculate statistics
                var today = DateTime.UtcNow.Date;
                TotalDays = (Tenancy.EndDate.Date - Tenancy.StartDate.Date).Days;
                DaysRemaining = (Tenancy.EndDate.Date - today).Days;
                if (DaysRemaining < 0) DaysRemaining = 0;
                if (TotalDays <= 0) TotalDays = 1; // Prevent division by zero
                var daysPassed = (today - Tenancy.StartDate.Date).Days;
                if (daysPassed < 0) daysPassed = 0;
                ProgressPercentage = Math.Min(100.0, Math.Max(0.0, (double)daysPassed / TotalDays * 100));

                SuccessMessage = TempData["SuccessMessage"] as string;
                ErrorMessage ??= TempData["ErrorMessage"] as string;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading tenancy details: {ex.Message}");
                ErrorMessage = "An error occurred while loading tenancy details.";
                return Page();
            }
        }
    }
}