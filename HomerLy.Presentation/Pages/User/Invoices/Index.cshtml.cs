using HomerLy.Business.Interfaces;
using HomerLy.BusinessObject.DTOs.InvoiceDTOs;
using HomerLy.BusinessObject.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.User.Invoices
{
    [Authorize(Roles = "User")]
    public class IndexModel : PageModel
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IInvoiceService invoiceService, ILogger<IndexModel> logger)
        {
            _invoiceService = invoiceService;
            _logger = logger;
        }

        public List<InvoiceResponseDto> Invoices { get; set; } = new();
        public string? StatusFilter { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        // Statistics
        public int PendingCount { get; set; }
        public int PaidCount { get; set; }
        public int OverdueCount { get; set; }
        public decimal TotalPending { get; set; }

        public async Task<IActionResult> OnGetAsync(string? status = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                StatusFilter = status;

                // Get all invoices for tenant
                Invoices = await _invoiceService.GetInvoicesByTenantAsync(userId);

                // Apply status filter
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<InvoiceStatus>(status, out var invoiceStatus))
                {
                    Invoices = Invoices.Where(i => i.Status == invoiceStatus).ToList();
                }

                // Calculate statistics
                PendingCount = Invoices.Count(i => i.Status == InvoiceStatus.pending);
                PaidCount = Invoices.Count(i => i.Status == InvoiceStatus.paid);
                OverdueCount = Invoices.Count(i => i.Status == InvoiceStatus.overdue);
                TotalPending = Invoices.Where(i => i.Status == InvoiceStatus.pending).Sum(i => i.TotalAmount);

                SuccessMessage = TempData["SuccessMessage"] as string;
                ErrorMessage = TempData["ErrorMessage"] as string;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading invoices: {ex.Message}");
                ErrorMessage = "An error occurred while loading invoices.";
                return Page();
            }
        }
    }
}