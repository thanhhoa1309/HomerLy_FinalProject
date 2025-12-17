using HomerLy.Business.Interfaces;
using HomerLy.BusinessObject.DTOs.PaymentDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HomerLy.Presentation.Pages.Payment
{
    [Authorize(Roles = "User")]
    public class PayInvoiceModel : PageModel
    {
        private readonly IPaymentService _paymentService;
        private readonly IInvoiceService _invoiceService;
        private readonly ILogger<PayInvoiceModel> _logger;

        public PayInvoiceModel(
            IPaymentService paymentService,
            IInvoiceService invoiceService,
            ILogger<PayInvoiceModel> logger)
        {
            _paymentService = paymentService;
            _invoiceService = invoiceService;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public Guid InvoiceId { get; set; }

        public HomerLy.BusinessObject.DTOs.InvoiceDTOs.InvoiceResponseDto? Invoice { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                // Get invoice
                Invoice = await _invoiceService.GetInvoiceByIdAsync(InvoiceId);

                if (Invoice == null)
                {
                    ErrorMessage = "Invoice not found";
                    return Page();
                }

                // Check if invoice belongs to current user
                if (Invoice.TenantId != userId)
                {
                    ErrorMessage = "You don't have access to this invoice";
                    return Page();
                }

                // Check if already paid
                if (Invoice.Status == HomerLy.BusinessObject.Enums.InvoiceStatus.paid)
                {
                    ErrorMessage = "This invoice has already been paid";
                    return Page();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading invoice: {ex.Message}");
                ErrorMessage = "An error occurred while loading the invoice";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                
                var createDto = new CreatePaymentDto
                {
                    InvoiceId = InvoiceId,
                    SuccessUrl = $"{baseUrl}/Payment/Success?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{baseUrl}/Payment/Cancel?invoiceId={InvoiceId}"
                };

                var checkoutSession = await _paymentService.CreateCheckoutSessionAsync(userId, createDto);

                // Redirect to Stripe Checkout using the URL from the session
                return Redirect(checkoutSession.CheckoutUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating checkout session: {ex.Message}");
                ErrorMessage = ex.Message;
                
                // Reload invoice
                Invoice = await _invoiceService.GetInvoiceByIdAsync(InvoiceId);
                
                return Page();
            }
        }
    }
}
