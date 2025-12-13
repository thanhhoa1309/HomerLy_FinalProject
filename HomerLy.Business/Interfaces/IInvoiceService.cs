using HomerLy.BusinessObject.DTOs.InvoiceDTOs;

namespace HomerLy.Business.Interfaces
{
    public interface IInvoiceService
    {
        /// <summary>
        /// Owner t?o invoice cho tenancy
        /// </summary>
        Task<InvoiceResponseDto> CreateInvoiceAsync(Guid ownerId, CreateInvoiceDto dto);

        /// <summary>
        /// L?y invoice theo ID
        /// </summary>
        Task<InvoiceResponseDto?> GetInvoiceByIdAsync(Guid invoiceId);

        /// <summary>
        /// L?y danh sách invoice c?a owner
        /// </summary>
        Task<List<InvoiceResponseDto>> GetInvoicesByOwnerAsync(Guid ownerId);

        /// <summary>
        /// L?y danh sách invoice c?a tenant
        /// </summary>
        Task<List<InvoiceResponseDto>> GetInvoicesByTenantAsync(Guid tenantId);

        /// <summary>
        /// L?y danh sách invoice c?a tenancy
        /// </summary>
        Task<List<InvoiceResponseDto>> GetInvoicesByTenancyAsync(Guid tenancyId);

        /// <summary>
        /// C?p nh?t invoice (ch? khi status là draft)
        /// </summary>
        Task<InvoiceResponseDto> UpdateInvoiceAsync(Guid invoiceId, Guid ownerId, UpdateInvoiceDto dto);

        /// <summary>
        /// C?p nh?t tr?ng thái invoice
        /// </summary>
        Task<InvoiceResponseDto> UpdateInvoiceStatusAsync(Guid invoiceId, UpdateInvoiceStatusDto dto);

        /// <summary>
        /// Xóa invoice (soft delete, ch? khi status là draft)
        /// </summary>
        Task<bool> DeleteInvoiceAsync(Guid invoiceId, Guid ownerId);

        /// <summary>
        /// C?p nh?t tr?ng thái invoice quá h?n (background job)
        /// </summary>
        Task UpdateOverdueInvoicesAsync();

        /// <summary>
        /// Tenant thanh toán invoice
        /// </summary>
        Task<InvoiceResponseDto> PayInvoiceAsync(Guid invoiceId, Guid tenantId);
    }
}
