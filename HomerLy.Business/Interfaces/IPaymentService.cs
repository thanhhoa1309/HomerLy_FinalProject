using HomerLy.BusinessObject.DTOs.PaymentDTOs;
using Homerly.Business.Utils;

namespace HomerLy.Business.Interfaces
{
    public interface IPaymentService
    {
        /// <summary>
        /// T?o Stripe Checkout Session ?? thanh toán invoice
        /// </summary>
        Task<StripeCheckoutSessionDto> CreateCheckoutSessionAsync(Guid userId, CreatePaymentDto createDto);

        /// <summary>
        /// X? lý webhook t? Stripe khi payment thành công
        /// </summary>
        Task<bool> HandleStripeWebhookAsync(string json, string stripeSignature);

        /// <summary>
        /// L?y thông tin payment theo ID
        /// </summary>
        Task<PaymentResponseDto?> GetPaymentByIdAsync(Guid paymentId, Guid userId);

        /// <summary>
        /// L?y danh sách payment c?a user
        /// </summary>
        Task<Pagination<PaymentResponseDto>> GetPaymentsByUserAsync(
            Guid userId,
            int pageNumber = 1,
            int pageSize = 10,
            bool? isPaid = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);

        /// <summary>
        /// L?y danh sách payment c?a property (cho owner)
        /// </summary>
        Task<Pagination<PaymentResponseDto>> GetPaymentsByPropertyAsync(
            Guid propertyId,
            Guid ownerId,
            int pageNumber = 1,
            int pageSize = 10,
            bool? isPaid = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);

        /// <summary>
        /// L?y payment theo invoice ID
        /// </summary>
        Task<PaymentResponseDto?> GetPaymentByInvoiceIdAsync(Guid invoiceId, Guid userId);

        /// <summary>
        /// Ki?m tra invoice ?ã ???c thanh toán ch?a
        /// </summary>
        Task<bool> IsInvoicePaidAsync(Guid invoiceId);
    }
}
