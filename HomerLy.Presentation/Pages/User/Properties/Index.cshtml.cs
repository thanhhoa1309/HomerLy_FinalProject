using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.PropertyDTOs;
using Homerly.BusinessObject.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.User.Properties
{
    [Authorize(Roles = "User")]
    public class IndexModel : PageModel
    {
        private readonly IPropertyService _propertyService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IPropertyService propertyService, ILogger<IndexModel> logger)
        {
            _propertyService = propertyService;
            _logger = logger;
        }

        public List<PropertyResponseDto> AvailableProperties { get; set; } = new List<PropertyResponseDto>();
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PriceRange { get; set; }

        public async Task<IActionResult> OnGetAsync(int pageNumber = 1)
        {
            try
            {
                CurrentPage = pageNumber;

                decimal? minPrice = null;
                decimal? maxPrice = null;

                if (!string.IsNullOrEmpty(PriceRange))
                {
                    var parts = PriceRange.Split('-');
                    if (parts.Length == 2)
                    {
                        if (decimal.TryParse(parts[0], out var min)) minPrice = min;
                        if (decimal.TryParse(parts[1], out var max)) maxPrice = max;
                    }
                    else if (PriceRange.EndsWith("+"))
                    {
                        if (decimal.TryParse(PriceRange.Replace("+", ""), out var min)) minPrice = min;
                    }
                }

                // Fetch only available properties for users
                var result = await _propertyService.GetPropertiesAsync(
                    pageNumber: pageNumber,
                    pageSize: 9,
                    status: PropertyStatus.available,
                    searchTerm: SearchTerm,
                    minRent: minPrice,
                    maxRent: maxPrice
                );

                AvailableProperties = result;
                TotalPages = result.TotalPages;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user properties");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostFavoriteAsync(Guid propertyId)
        {
            // TODO: Implement toggle favorite logic
            return RedirectToPage();
        }
    }
}
