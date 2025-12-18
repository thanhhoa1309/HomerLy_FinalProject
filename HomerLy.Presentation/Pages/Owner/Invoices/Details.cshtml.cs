using HomerLy.Business.Interfaces;
using HomerLy.BusinessObject.DTOs.InvoiceDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Owner.Invoices
{
    [Authorize(Roles = "Owner")]
    public class DetailsModel : PageModel
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(IInvoiceService invoiceService, ILogger<DetailsModel> logger)
        {
            _invoiceService = invoiceService;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public Guid Id { get; set; }

        public InvoiceResponseDto? Invoice { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                Invoice = await _invoiceService.GetInvoiceByIdAsync(Id);

                if (Invoice == null)
                {
                    ErrorMessage = "Invoice not found";
                    return Page();
                }

                // Check if invoice belongs to current owner
                if (Invoice.OwnerId != ownerId)
                {
                    ErrorMessage = "You don't have access to this invoice";
                    return Page();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading invoice details: {ex.Message}");
                ErrorMessage = "An error occurred while loading the invoice details";
                return Page();
            }
        }
    }
}