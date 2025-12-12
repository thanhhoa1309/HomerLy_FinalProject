using Homerly.Business.Utils;
using Homerly.BusinessObject.DTOs.TenancyDTOs;
using Homerly.BusinessObject.Enums;

namespace Homerly.Business.Interfaces
{
    public interface ITenancyService
    {
        /// T?o tenancy m?i (Owner t?o cho Tenant)
        Task<TenancyResponseDto?> CreateTenancyAsync(Guid ownerId, CreateTenancyDto createDto);

        /// C?p nh?t thông tin tenancy
        Task<TenancyResponseDto?> UpdateTenancyAsync(Guid tenancyId, Guid userId, UpdateTenancyDto updateDto);

        /// C?p nh?t status c?a tenancy (pending -> active -> expired)
        Task<TenancyResponseDto?> UpdateTenancyStatusAsync(Guid tenancyId, Guid userId, TenancyStatus newStatus);

        /// Tenant xác nh?n tenancy (IsTenantConfirmed = true)
        Task<TenancyResponseDto?> TenantConfirmTenancyAsync(Guid tenancyId, Guid tenantId);

        /// L?y tenancy theo ID
        Task<TenancyResponseDto?> GetTenancyByIdAsync(Guid tenancyId, Guid userId);

        /// L?y danh sách tenancies (có filter)
        Task<Pagination<TenancyResponseDto>> GetTenanciesAsync(
            Guid userId,
            int pageNumber = 1,
            int pageSize = 10,
            Guid? propertyId = null,
            Guid? tenantId = null,
            Guid? ownerId = null,
            TenancyStatus? status = null,
            bool? isTenantConfirmed = null,
            bool? isDeleted = null,
            DateTime? startDateFrom = null,
            DateTime? startDateTo = null);

        /// L?y active tenancy c?a m?t property
        Task<TenancyResponseDto?> GetActiveTenancyByPropertyIdAsync(Guid propertyId, Guid userId);

        /// L?y danh sách tenancies c?a m?t tenant
        Task<Pagination<TenancyResponseDto>> GetTenanciesByTenantIdAsync(
            Guid tenantId,
            Guid userId,
            int pageNumber = 1,
            int pageSize = 10,
            TenancyStatus? status = null);

        /// L?y danh sách tenancies c?a m?t owner
        Task<Pagination<TenancyResponseDto>> GetTenanciesByOwnerIdAsync(
            Guid ownerId,
            Guid userId,
            int pageNumber = 1,
            int pageSize = 10,
            TenancyStatus? status = null);

        /// Cancel tenancy (Owner ho?c Tenant)
        Task<bool> CancelTenancyAsync(Guid tenancyId, Guid userId);

        /// Xóa tenancy (soft delete, ch? Owner)
        Task<bool> DeleteTenancyAsync(Guid tenancyId, Guid ownerId);

        /// T? ??ng c?p nh?t status expired cho các tenancies ?ã h?t h?n
        Task<int> UpdateExpiredTenanciesAsync();
    }
}
