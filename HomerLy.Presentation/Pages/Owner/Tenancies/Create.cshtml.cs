using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.TenancyDTOs;
using Homerly.BusinessObject.DTOs.PropertyDTOs;
using Homerly.BusinessObject.DTOs.UserDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Owner.Tenancies
{
    [Authorize(Roles = "Owner")]
    public class CreateModel : PageModel
    {
        private readonly ITenancyService _tenancyService;
        private readonly IPropertyService _propertyService;
        private readonly IAccountService _accountService;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            ITenancyService tenancyService,
            IPropertyService propertyService,
            IAccountService accountService,
            ILogger<CreateModel> logger)
        {
            _tenancyService = tenancyService;
            _propertyService = propertyService;
            _accountService = accountService;
            _logger = logger;
        }

        [BindProperty]
        public CreateTenancyDto Tenancy { get; set; } = new();

        public List<PropertyResponseDto> AvailableProperties { get; set; } = new();
        public List<AccountResponseDto> AvailableTenants { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                // Get owner's available properties
                var allProperties = await _propertyService.GetPropertiesAsync(
                    pageNumber: 1,
                    pageSize: int.MaxValue,
                    ownerId: ownerId);

                AvailableProperties = allProperties
                    .Where(p => p.Status == Homerly.BusinessObject.Enums.PropertyStatus.available)
                    .ToList();

                // Get all users (tenants)
                var allAccounts = await _accountService.GetAccountsAsync(
                    pageNumber: 1,
                    pageSize: int.MaxValue);

                AvailableTenants = allAccounts
                    .Where(a => a.Role == Homerly.BusinessObject.Enums.RoleType.User)
                    .ToList();

                // Set default values
                Tenancy.StartDate = DateTime.Today;
                Tenancy.EndDate = DateTime.Today.AddMonths(12);
                Tenancy.ElectricUnitPrice = 3500; // Default electric price
                Tenancy.WaterUnitPrice = 15000;   // Default water price
                Tenancy.ElectricOldIndex = 0;
                Tenancy.WaterOldIndex = 0;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading create tenancy page: {ex.Message}");
                ErrorMessage = "An error occurred while loading the page.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                if (!ModelState.IsValid)
                {
                    // Reload dropdowns
                    var allProperties = await _propertyService.GetPropertiesAsync(
                        pageNumber: 1,
                        pageSize: int.MaxValue,
                        ownerId: ownerId);

                    AvailableProperties = allProperties
                        .Where(p => p.Status == Homerly.BusinessObject.Enums.PropertyStatus.available)
                        .ToList();

                    var allAccounts = await _accountService.GetAccountsAsync(
                        pageNumber: 1,
                        pageSize: int.MaxValue);

                    AvailableTenants = allAccounts
                        .Where(a => a.Role == Homerly.BusinessObject.Enums.RoleType.User)
                        .ToList();

                    ErrorMessage = "Please correct the errors and try again.";
                    return Page();
                }

                // Validate dates
                if (Tenancy.EndDate <= Tenancy.StartDate)
                {
                    ErrorMessage = "End date must be after start date.";
                    await OnGetAsync();
                    return Page();
                }

                var result = await _tenancyService.CreateTenancyAsync(ownerId, Tenancy);

                if (result == null)
                {
                    ErrorMessage = "Failed to create tenancy. Please try again.";
                    await OnGetAsync();
                    return Page();
                }

                _logger.LogInformation($"Tenancy created successfully: {result.Id}");

                TempData["SuccessMessage"] = $"Tenancy created successfully! Waiting for tenant confirmation.";
                return RedirectToPage("/Owner/Tenancies/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating tenancy: {ex.Message}");

                // Show specific error messages
                if (ex.Message.Contains("already") || ex.Message.Contains("occupied") ||
                    ex.Message.Contains("not found") || ex.Message.Contains("must be"))
                {
                    ErrorMessage = ex.Message;
                }
                else
                {
                    ErrorMessage = $"An error occurred while creating the tenancy: {ex.Message}";
                }

                await OnGetAsync();
                return Page();
            }
        }
    }
}
