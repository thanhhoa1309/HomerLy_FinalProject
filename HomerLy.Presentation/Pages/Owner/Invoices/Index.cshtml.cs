using HomerLy.Business.Interfaces;
using HomerLy.BusinessObject.DTOs.InvoiceDTOs;
using HomerLy.BusinessObject.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Owner.Invoices
{
    [Authorize(Roles = "Owner")]
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
        public int DraftCount { get; set; }
        public int PendingCount { get; set; }
        public int PaidCount { get; set; }
        public int OverdueCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal PendingRevenue { get; set; }

        public async Task<IActionResult> OnGetAsync(string? status = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                StatusFilter = status;

                // Get all invoices created by owner
                Invoices = await _invoiceService.GetInvoicesByOwnerAsync(ownerId);

                // Apply status filter
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<InvoiceStatus>(status, out var invoiceStatus))
                {
                    Invoices = Invoices.Where(i => i.Status == invoiceStatus).ToList();
                }

                // Calculate statistics
                DraftCount = Invoices.Count(i => i.Status == InvoiceStatus.draft);
                PendingCount = Invoices.Count(i => i.Status == InvoiceStatus.pending);
                PaidCount = Invoices.Count(i => i.Status == InvoiceStatus.paid);
                OverdueCount = Invoices.Count(i => i.Status == InvoiceStatus.overdue);
                TotalRevenue = Invoices.Where(i => i.Status == InvoiceStatus.paid).Sum(i => i.TotalAmount);
                PendingRevenue = Invoices.Where(i => i.Status == InvoiceStatus.pending).Sum(i => i.TotalAmount);

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

        public async Task<IActionResult> OnPostSendAsync(Guid invoiceId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                var updateDto = new UpdateInvoiceStatusDto
                {
                    Status = InvoiceStatus.pending
                };

                await _invoiceService.UpdateInvoiceStatusAsync(invoiceId, updateDto);
                TempData["SuccessMessage"] = "Invoice sent to tenant successfully!";

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending invoice: {ex.Message}");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid invoiceId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                var success = await _invoiceService.DeleteInvoiceAsync(invoiceId, ownerId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Draft invoice deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete invoice.";
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting invoice: {ex.Message}");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage();
            }
        }
    }
}