using HomerLy.Business.Interfaces;
using HomerLy.BusinessObject.DTOs.PaymentDTOs;
using HomerLy.DataAccess.Interfaces;
using Homerly.DataAccess.Entities;
using Microsoft.Extensions.Logging;
using Stripe.Checkout;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Homerly.Business.Utils;

namespace HomerLy.Business.Service
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentService> _logger;
        private readonly IConfiguration _configuration;

        public PaymentService(IUnitOfWork unitOfWork, ILogger<PaymentService> logger, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<StripeCheckoutSessionDto> CreateCheckoutSessionAsync(Guid userId, CreatePaymentDto createDto)
        {
            try
            {
                _logger.LogInformation($"Creating Stripe checkout session for user {userId} and invoice {createDto.InvoiceId}");

                // Get invoice details
                var invoice = await _unitOfWork.Invoice.GetByIdAsync(createDto.InvoiceId);
                if (invoice == null)
                {
                    throw new Exception($"Invoice with ID {createDto.InvoiceId} not found");
                }

                // Verify user is the tenant
                if (invoice.TenantId != userId)
                {
                    throw new UnauthorizedAccessException("You don't have permission to pay this invoice");
                }

                // Create payment record
                var payment = new Payment
                {
                    PropertyId = invoice.PropertyId,
                    TenancyId = invoice.TenancyId,
                    PayerId = userId,
                    InvoiceId = createDto.InvoiceId,
                    Amount = invoice.TotalAmount,
                    PaymentFor = $"Invoice #{invoice.Id}",
                    PaymentDate = DateTime.UtcNow,
                    PaymentMethod = "Stripe",
                    IsPaid = false
                };

                await _unitOfWork.Payment.AddAsync(payment);
                await _unitOfWork.SaveChangesAsync();

                // Create Stripe checkout session
                var domain = _configuration["AppSettings:Domain"] ?? "http://localhost:5000";
                
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = "usd",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = $"Invoice #{invoice.Id}",
                                    Description = $"Payment for invoice from {invoice.BillingPeriodStart:MM/dd/yyyy} to {invoice.BillingPeriodEnd:MM/dd/yyyy}"
                                },
                                UnitAmount = (long)(invoice.TotalAmount * 100), // Convert to cents
                            },
                            Quantity = 1,
                        },
                    },
                    Mode = "payment",
                    SuccessUrl = createDto.SuccessUrl ?? $"{domain}/Payment/Success?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = createDto.CancelUrl ?? $"{domain}/Payment/Cancel?invoiceId={createDto.InvoiceId}",
                    ClientReferenceId = payment.Id.ToString(),
                    Metadata = new Dictionary<string, string>
                    {
                        { "PaymentId", payment.Id.ToString() },
                        { "InvoiceId", createDto.InvoiceId.ToString() }
                    }
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                // Update payment with Stripe session ID
                payment.StripeSessionId = session.Id;
                await _unitOfWork.Payment.Update(payment);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Stripe checkout session created: {session.Id}");

                return new StripeCheckoutSessionDto
                {
                    SessionId = session.Id,
                    CheckoutUrl = session.Url,
                    PaymentId = payment.Id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating checkout session: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> HandleStripeWebhookAsync(string json, string stripeSignature)
        {
            try
            {
                _logger.LogInformation("Processing Stripe webhook");

                var stripeEvent = Stripe.EventUtility.ConstructEvent(
                    json,
                    stripeSignature,
                    _configuration["Stripe:WebhookSecret"]
                );

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    if (session != null)
                    {
                        var paymentIdStr = session.Metadata["PaymentId"];
                        if (Guid.TryParse(paymentIdStr, out var paymentId))
                        {
                            var payment = await _unitOfWork.Payment.GetByIdAsync(paymentId);
                            if (payment != null)
                            {
                                payment.IsPaid = true;
                                payment.StripePaymentIntentId = session.PaymentIntentId;
                                payment.PaymentDate = DateTime.UtcNow;

                                await _unitOfWork.Payment.Update(payment);

                                // Update invoice status
                                if (payment.InvoiceId.HasValue)
                                {
                                    var invoice = await _unitOfWork.Invoice.GetByIdAsync(payment.InvoiceId.Value);
                                    if (invoice != null)
                                    {
                                        invoice.Status = HomerLy.BusinessObject.Enums.InvoiceStatus.paid;
                                        invoice.PaymentDate = DateTime.UtcNow;
                                        await _unitOfWork.Invoice.Update(invoice);
                                    }
                                }

                                await _unitOfWork.SaveChangesAsync();
                                _logger.LogInformation($"Payment {paymentId} marked as paid");
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling webhook: {ex.Message}");
                throw;
            }
        }

        public async Task<PaymentResponseDto?> GetPaymentByIdAsync(Guid paymentId, Guid userId)
        {
            try
            {
                var payment = await _unitOfWork.Payment.GetByIdAsync(paymentId);
                
                if (payment == null || payment.IsDeleted)
                {
                    return null;
                }

                // Verify user has access
                if (payment.PayerId != userId)
                {
                    throw new UnauthorizedAccessException("You don't have permission to view this payment");
                }

                return await MapToResponseDtoAsync(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting payment {paymentId}: {ex.Message}");
                throw;
            }
        }

        public async Task<Pagination<PaymentResponseDto>> GetPaymentsByUserAsync(
            Guid userId,
            int pageNumber = 1,
            int pageSize = 10,
            bool? isPaid = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                var query = _unitOfWork.Payment.GetQueryable()
                    .Where(p => p.PayerId == userId && !p.IsDeleted);

                if (isPaid.HasValue)
                {
                    query = query.Where(p => p.IsPaid == isPaid.Value);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(p => p.PaymentDate >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(p => p.PaymentDate <= toDate.Value);
                }

                query = query.OrderByDescending(p => p.CreatedAt);

                var totalCount = await query.CountAsync();
                var payments = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var paymentDtos = new List<PaymentResponseDto>();
                foreach (var payment in payments)
                {
                    paymentDtos.Add(await MapToResponseDtoAsync(payment));
                }

                return new Pagination<PaymentResponseDto>(paymentDtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting payments for user {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task<Pagination<PaymentResponseDto>> GetPaymentsByPropertyAsync(
            Guid propertyId,
            Guid ownerId,
            int pageNumber = 1,
            int pageSize = 10,
            bool? isPaid = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                // Verify ownership
                var property = await _unitOfWork.Property.GetByIdAsync(propertyId);
                if (property == null || property.OwnerId != ownerId)
                {
                    throw new UnauthorizedAccessException("You don't have permission to view payments for this property");
                }

                var query = _unitOfWork.Payment.GetQueryable()
                    .Where(p => p.PropertyId == propertyId && !p.IsDeleted);

                if (isPaid.HasValue)
                {
                    query = query.Where(p => p.IsPaid == isPaid.Value);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(p => p.PaymentDate >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(p => p.PaymentDate <= toDate.Value);
                }

                query = query.OrderByDescending(p => p.CreatedAt);

                var totalCount = await query.CountAsync();
                var payments = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var paymentDtos = new List<PaymentResponseDto>();
                foreach (var payment in payments)
                {
                    paymentDtos.Add(await MapToResponseDtoAsync(payment));
                }

                return new Pagination<PaymentResponseDto>(paymentDtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting payments for property {propertyId}: {ex.Message}");
                throw;
            }
        }

        public async Task<PaymentResponseDto?> GetPaymentByInvoiceIdAsync(Guid invoiceId, Guid userId)
        {
            try
            {
                var payment = await _unitOfWork.Payment.FirstOrDefaultAsync(
                    p => p.InvoiceId == invoiceId && !p.IsDeleted);

                if (payment == null)
                {
                    return null;
                }

                // Verify user has access
                if (payment.PayerId != userId)
                {
                    throw new UnauthorizedAccessException("You don't have permission to view this payment");
                }

                return await MapToResponseDtoAsync(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting payment for invoice {invoiceId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> IsInvoicePaidAsync(Guid invoiceId)
        {
            try
            {
                var payment = await _unitOfWork.Payment.FirstOrDefaultAsync(
                    p => p.InvoiceId == invoiceId && p.IsPaid && !p.IsDeleted);

                return payment != null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking if invoice {invoiceId} is paid: {ex.Message}");
                throw;
            }
        }

        private async Task<PaymentResponseDto> MapToResponseDtoAsync(Payment payment)
        {
            var payer = await _unitOfWork.Account.GetByIdAsync(payment.PayerId);
            var property = await _unitOfWork.Property.GetByIdAsync(payment.PropertyId);

            return new PaymentResponseDto
            {
                Id = payment.Id,
                PropertyId = payment.PropertyId,
                TenancyId = payment.TenancyId,
                PayerId = payment.PayerId,
                InvoiceId = payment.InvoiceId,
                Amount = payment.Amount,
                PaymentFor = payment.PaymentFor,
                PaymentDate = payment.PaymentDate,
                PaymentMethod = payment.PaymentMethod,
                IsPaid = payment.IsPaid,
                StripeSessionId = payment.StripeSessionId,
                StripePaymentIntentId = payment.StripePaymentIntentId,
                StripeChargeId = payment.StripeChargeId,
                PayerName = payer?.FullName ?? "Unknown",
                PropertyTitle = property?.Title ?? "Unknown",
                InvoiceNumber = payment.InvoiceId?.ToString(),
                CreatedAt = payment.CreatedAt,
                UpdatedAt = payment.UpdatedAt
            };
        }
    }
}
