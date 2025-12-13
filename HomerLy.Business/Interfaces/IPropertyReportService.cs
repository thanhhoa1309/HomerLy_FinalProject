using HomerLy.BusinessObject.DTOs.PropertyReportDTOs;

namespace HomerLy.Business.Interfaces
{
    public interface IPropertyReportService
    {
        /// <summary>
        /// Tenant t?o report cho property
        /// </summary>
        Task<PropertyReportResponseDto> CreateReportAsync(Guid tenantId, CreatePropertyReportDto dto);

        /// <summary>
        /// L?y report theo ID
        /// </summary>
        Task<PropertyReportResponseDto?> GetReportByIdAsync(Guid reportId);

        /// <summary>
        /// L?y danh sách report c?a owner
        /// </summary>
        Task<List<PropertyReportResponseDto>> GetReportsByOwnerAsync(Guid ownerId);

        /// <summary>
        /// L?y danh sách report c?a tenant
        /// </summary>
        Task<List<PropertyReportResponseDto>> GetReportsByTenantAsync(Guid tenantId);

        /// <summary>
        /// L?y danh sách report c?a property
        /// </summary>
        Task<List<PropertyReportResponseDto>> GetReportsByPropertyAsync(Guid propertyId);

        /// <summary>
        /// L?y danh sách report c?a tenancy
        /// </summary>
        Task<List<PropertyReportResponseDto>> GetReportsByTenancyAsync(Guid tenancyId);

        /// <summary>
        /// Tenant c?p nh?t report c?a mình
        /// </summary>
        Task<PropertyReportResponseDto> UpdateReportAsync(Guid reportId, Guid tenantId, UpdatePropertyReportDto dto);

        /// <summary>
        /// Owner c?p nh?t tr?ng thái report (Priority)
        /// </summary>
        Task<PropertyReportResponseDto> UpdateReportStatusAsync(Guid reportId, Guid ownerId, UpdatePropertyReportStatusDto dto);

        /// <summary>
        /// Xóa report (soft delete)
        /// </summary>
        Task<bool> DeleteReportAsync(Guid reportId, Guid userId);
    }
}
