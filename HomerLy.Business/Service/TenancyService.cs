using Homerly.Business.Interfaces;
using Homerly.Business.Utils;
using Homerly.BusinessObject.DTOs.TenancyDTOs;
using Homerly.BusinessObject.Enums;
using Homerly.DataAccess.Entities;
using HomerLy.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Homerly.Business.Services
{
    public class TenancyService : ITenancyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TenancyService> _logger;

        public TenancyService(IUnitOfWork unitOfWork, ILogger<TenancyService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<TenancyResponseDto?> CreateTenancyAsync(Guid ownerId, CreateTenancyDto createDto)
        {
            try
            {
                _logger.LogInformation($"Creating tenancy for property {createDto.PropertyId}");

                if (createDto == null)
                {
                    throw ErrorHelper.BadRequest("Tenancy data is required.");
                }

                // Verify owner exists and has Owner role
                var owner = await _unitOfWork.Account.GetByIdAsync(ownerId);
                if (owner == null || owner.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Owner with ID {ownerId} not found.");
                }

                if (owner.Role != RoleType.Owner)
                {
                    throw ErrorHelper.Forbidden("Only owners can create tenancies.");
                }

                // Verify property exists and belongs to owner
                var property = await _unitOfWork.Property.GetByIdAsync(createDto.PropertyId);
                if (property == null || property.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Property with ID {createDto.PropertyId} not found.");
                }

                if (property.OwnerId != ownerId)
                {
                    throw ErrorHelper.Forbidden("You can only create tenancies for your own properties.");
                }

                // Check if property is available
                if (property.Status == PropertyStatus.occupied)
                {
                    throw ErrorHelper.Conflict("Property is already occupied.");
                }

                // Verify tenant exists and has User role (ng??i thu�)
                var tenant = await _unitOfWork.Account.GetByIdAsync(createDto.TenantId);
                if (tenant == null || tenant.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Tenant with ID {createDto.TenantId} not found.");
                }

                // Tenant ph?i c� role User
                if (tenant.Role != RoleType.User)
                {
                    throw ErrorHelper.BadRequest("Tenant must be a User (not Owner or Admin).");
                }

                // Check if there's already an active tenancy for this property
                var existingActiveTenancy = await _unitOfWork.Tenancy.FirstOrDefaultAsync(
                    t => t.PropertyId == createDto.PropertyId &&
                         t.Status == TenancyStatus.active &&
                         !t.IsDeleted
                );

                if (existingActiveTenancy != null)
                {
                    throw ErrorHelper.Conflict("Property already has an active tenancy.");
                }

                // Validate dates
                if (createDto.EndDate <= createDto.StartDate)
                {
                    throw ErrorHelper.BadRequest("End date must be after start date.");
                }

                // Convert dates to UTC for PostgreSQL
                var startDateUtc = DateTime.SpecifyKind(createDto.StartDate, DateTimeKind.Utc);
                var endDateUtc = DateTime.SpecifyKind(createDto.EndDate, DateTimeKind.Utc);

                // Create tenancy with initial utility indexes
                var tenancy = new Tenancy
                {
                    PropertyId = createDto.PropertyId,
                    TenantId = createDto.TenantId,
                    OwnerId = ownerId,
                    StartDate = startDateUtc,
                    EndDate = endDateUtc,
                    ContractUrl = createDto.ContractUrl ?? string.Empty,
                    Status = TenancyStatus.pending_confirmation,
                    IsTenantConfirmed = false,
                    ElectricUnitPrice = createDto.ElectricUnitPrice,
                    WaterUnitPrice = createDto.WaterUnitPrice,
                    ElectricOldIndex = createDto.ElectricOldIndex,  // Ch? s? ?i?n ban ??u
                    WaterOldIndex = createDto.WaterOldIndex          // Ch? s? n??c ban ??u
                };

                await _unitOfWork.Tenancy.AddAsync(tenancy);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Tenancy {tenancy.Id} created successfully with initial Electric: {tenancy.ElectricOldIndex}, Water: {tenancy.WaterOldIndex}");

                return await MapToResponseDto(tenancy);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating tenancy: {ex.Message}");
                throw;
            }
        }

        public async Task<TenancyResponseDto?> UpdateTenancyAsync(Guid tenancyId, Guid userId, UpdateTenancyDto updateDto)
        {
            try
            {
                _logger.LogInformation($"Updating tenancy {tenancyId}");

                if (updateDto == null)
                {
                    throw ErrorHelper.BadRequest("Update data is required.");
                }

                var tenancy = await _unitOfWork.Tenancy.GetByIdAsync(tenancyId);
                if (tenancy == null || tenancy.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Tenancy with ID {tenancyId} not found.");
                }

                // Only owner can update tenancy details
                if (tenancy.OwnerId != userId)
                {
                    throw ErrorHelper.Forbidden("Only the property owner can update tenancy details.");
                }

                // Cannot update if already active (except certain fields)
                if (tenancy.Status == TenancyStatus.active || tenancy.Status == TenancyStatus.expired)
                {
                    throw ErrorHelper.Conflict("Cannot update active or expired tenancy details.");
                }

                // Update fields
                if (updateDto.StartDate.HasValue)
                {
                    tenancy.StartDate = updateDto.StartDate.Value;
                }

                if (updateDto.EndDate.HasValue)
                {
                    tenancy.EndDate = updateDto.EndDate.Value;
                }

                if (updateDto.EndDate.HasValue && updateDto.StartDate.HasValue)
                {
                    if (updateDto.EndDate.Value <= updateDto.StartDate.Value)
                    {
                        throw ErrorHelper.BadRequest("End date must be after start date.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(updateDto.ContractUrl))
                {
                    tenancy.ContractUrl = updateDto.ContractUrl;
                }

                if (updateDto.ElectricUnitPrice.HasValue)
                {
                    tenancy.ElectricUnitPrice = updateDto.ElectricUnitPrice.Value;
                }

                if (updateDto.WaterUnitPrice.HasValue)
                {
                    tenancy.WaterUnitPrice = updateDto.WaterUnitPrice.Value;
                }

                await _unitOfWork.Tenancy.Update(tenancy);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Tenancy {tenancyId} updated successfully");

                return await MapToResponseDto(tenancy);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating tenancy {tenancyId}: {ex.Message}");
                throw;
            }
        }

        public async Task<TenancyResponseDto?> UpdateTenancyStatusAsync(Guid tenancyId, Guid userId, TenancyStatus newStatus)
        {
            try
            {
                _logger.LogInformation($"Updating tenancy {tenancyId} status to {newStatus}");

                var tenancy = await _unitOfWork.Tenancy.GetByIdAsync(tenancyId);
                if (tenancy == null || tenancy.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Tenancy with ID {tenancyId} not found.");
                }

                // Only owner can change status
                if (tenancy.OwnerId != userId)
                {
                    throw ErrorHelper.Forbidden("Only the property owner can change tenancy status.");
                }

                // Validate status transition
                if (!IsValidStatusTransition(tenancy.Status, newStatus))
                {
                    throw ErrorHelper.BadRequest($"Cannot change status from {tenancy.Status} to {newStatus}.");
                }

                // If changing to active, check if tenant confirmed
                if (newStatus == TenancyStatus.active && !tenancy.IsTenantConfirmed)
                {
                    throw ErrorHelper.BadRequest("Tenant must confirm the tenancy before it can be activated.");
                }

                // Update property status when tenancy becomes active
                if (newStatus == TenancyStatus.active)
                {
                    var property = await _unitOfWork.Property.GetByIdAsync(tenancy.PropertyId);
                    if (property != null)
                    {
                        property.Status = PropertyStatus.occupied;
                        await _unitOfWork.Property.Update(property);
                    }
                }

                // Update property status when tenancy ends
                if (newStatus == TenancyStatus.expired || newStatus == TenancyStatus.cancelled)
                {
                    var property = await _unitOfWork.Property.GetByIdAsync(tenancy.PropertyId);
                    if (property != null)
                    {
                        property.Status = PropertyStatus.available;
                        await _unitOfWork.Property.Update(property);
                    }
                }

                tenancy.Status = newStatus;

                await _unitOfWork.Tenancy.Update(tenancy);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Tenancy {tenancyId} status updated to {newStatus}");

                return await MapToResponseDto(tenancy);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating tenancy status {tenancyId}: {ex.Message}");
                throw;
            }
        }

        public async Task<TenancyResponseDto?> TenantConfirmTenancyAsync(Guid tenancyId, Guid tenantId)
        {
            try
            {
                _logger.LogInformation($"Tenant {tenantId} confirming tenancy {tenancyId}");

                var tenancy = await _unitOfWork.Tenancy.GetByIdAsync(tenancyId);
                if (tenancy == null || tenancy.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Tenancy with ID {tenancyId} not found.");
                }

                // Only the tenant can confirm
                if (tenancy.TenantId != tenantId)
                {
                    throw ErrorHelper.Forbidden("You can only confirm your own tenancy.");
                }

                // Can only confirm if status is pending_confirmation
                if (tenancy.Status != TenancyStatus.pending_confirmation)
                {
                    throw ErrorHelper.BadRequest("Tenancy is not in pending confirmation status.");
                }

                if (tenancy.IsTenantConfirmed)
                {
                    throw ErrorHelper.Conflict("Tenancy is already confirmed.");
                }

                // Set confirmation flag and activate tenancy
                tenancy.IsTenantConfirmed = true;
                tenancy.Status = TenancyStatus.active;

                // Update property status to occupied
                var property = await _unitOfWork.Property.GetByIdAsync(tenancy.PropertyId);
                if (property != null)
                {
                    property.Status = PropertyStatus.occupied;
                    await _unitOfWork.Property.Update(property);
                }

                await _unitOfWork.Tenancy.Update(tenancy);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Tenancy {tenancyId} confirmed by tenant and activated");

                return await MapToResponseDto(tenancy);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error confirming tenancy {tenancyId}: {ex.Message}");
                throw;
            }
        }

        public async Task<TenancyResponseDto?> GetTenancyByIdAsync(Guid tenancyId, Guid userId)
        {
            try
            {
                _logger.LogInformation($"Getting tenancy {tenancyId}");

                var tenancy = await _unitOfWork.Tenancy.GetByIdAsync(tenancyId);
                if (tenancy == null || tenancy.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Tenancy with ID {tenancyId} not found.");
                }

                // Only owner, tenant, or admin can view
                if (tenancy.OwnerId != userId && tenancy.TenantId != userId)
                {
                    var user = await _unitOfWork.Account.GetByIdAsync(userId);
                    if (user == null || user.Role != RoleType.Admin)
                    {
                        throw ErrorHelper.Forbidden("You don't have permission to view this tenancy.");
                    }
                }

                return await MapToResponseDto(tenancy);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting tenancy {tenancyId}: {ex.Message}");
                throw;
            }
        }

        public async Task<Pagination<TenancyResponseDto>> GetTenanciesAsync(
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
            DateTime? startDateTo = null)
        {
            try
            {
                _logger.LogInformation("Getting tenancies with filters");

                var query = _unitOfWork.Tenancy.GetQueryable();

                // Apply isDeleted filter
                if (isDeleted.HasValue)
                {
                    query = query.Where(t => t.IsDeleted == isDeleted.Value);
                }
                else
                {
                    query = query.Where(t => !t.IsDeleted);
                }

                // Apply filters
                if (propertyId.HasValue)
                {
                    query = query.Where(t => t.PropertyId == propertyId.Value);
                }

                if (tenantId.HasValue)
                {
                    query = query.Where(t => t.TenantId == tenantId.Value);
                }

                if (ownerId.HasValue)
                {
                    query = query.Where(t => t.OwnerId == ownerId.Value);
                }

                if (status.HasValue)
                {
                    query = query.Where(t => t.Status == status.Value);
                }

                if (isTenantConfirmed.HasValue)
                {
                    query = query.Where(t => t.IsTenantConfirmed == isTenantConfirmed.Value);
                }

                if (startDateFrom.HasValue)
                {
                    query = query.Where(t => t.StartDate >= startDateFrom.Value);
                }

                if (startDateTo.HasValue)
                {
                    query = query.Where(t => t.StartDate <= startDateTo.Value);
                }

                // Authorization check
                var user = await _unitOfWork.Account.GetByIdAsync(userId);
                if (user != null && user.Role != RoleType.Admin)
                {
                    // Non-admin can only see their own tenancies
                    query = query.Where(t => t.OwnerId == userId || t.TenantId == userId);
                }

                query = query.OrderByDescending(t => t.CreatedAt);

                var totalCount = await query.CountAsync();
                var tenancies = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var tenancyDtos = new List<TenancyResponseDto>();
                foreach (var tenancy in tenancies)
                {
                    var dto = await MapToResponseDto(tenancy);
                    if (dto != null)
                    {
                        tenancyDtos.Add(dto);
                    }
                }

                return new Pagination<TenancyResponseDto>(tenancyDtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting tenancies: {ex.Message}");
                throw;
            }
        }

        public async Task<TenancyResponseDto?> GetActiveTenancyByPropertyIdAsync(Guid propertyId, Guid userId)
        {
            try
            {
                _logger.LogInformation($"Getting active tenancy for property {propertyId}");

                var property = await _unitOfWork.Property.GetByIdAsync(propertyId);
                if (property == null || property.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Property with ID {propertyId} not found.");
                }

                var activeTenancy = await _unitOfWork.Tenancy.FirstOrDefaultAsync(
                    t => t.PropertyId == propertyId &&
                         t.Status == TenancyStatus.active &&
                         !t.IsDeleted
                );

                if (activeTenancy == null)
                {
                    return null;
                }

                // Check permission
                if (activeTenancy.OwnerId != userId && activeTenancy.TenantId != userId)
                {
                    var user = await _unitOfWork.Account.GetByIdAsync(userId);
                    if (user == null || user.Role != RoleType.Admin)
                    {
                        throw ErrorHelper.Forbidden("You don't have permission to view this tenancy.");
                    }
                }

                return await MapToResponseDto(activeTenancy);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting active tenancy for property {propertyId}: {ex.Message}");
                throw;
            }
        }

        public async Task<Pagination<TenancyResponseDto>> GetTenanciesByTenantIdAsync(
            Guid tenantId,
            Guid userId,
            int pageNumber = 1,
            int pageSize = 10,
            TenancyStatus? status = null)
        {
            return await GetTenanciesAsync(
                userId: userId,
                pageNumber: pageNumber,
                pageSize: pageSize,
                tenantId: tenantId,
                status: status
            );
        }

        public async Task<Pagination<TenancyResponseDto>> GetTenanciesByOwnerIdAsync(
            Guid ownerId,
            Guid userId,
            int pageNumber = 1,
            int pageSize = 10,
            TenancyStatus? status = null)
        {
            return await GetTenanciesAsync(
                userId: userId,
                pageNumber: pageNumber,
                pageSize: pageSize,
                ownerId: ownerId,
                status: status
            );
        }

        public async Task<bool> CancelTenancyAsync(Guid tenancyId, Guid userId)
        {
            try
            {
                _logger.LogInformation($"Cancelling tenancy {tenancyId}");

                var tenancy = await _unitOfWork.Tenancy.GetByIdAsync(tenancyId);
                if (tenancy == null || tenancy.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Tenancy with ID {tenancyId} not found.");
                }

                // Owner or tenant can cancel
                if (tenancy.OwnerId != userId && tenancy.TenantId != userId)
                {
                    throw ErrorHelper.Forbidden("Only owner or tenant can cancel the tenancy.");
                }

                // Cannot cancel if already expired or cancelled
                if (tenancy.Status == TenancyStatus.expired || tenancy.Status == TenancyStatus.cancelled)
                {
                    throw ErrorHelper.Conflict($"Cannot cancel tenancy with status {tenancy.Status}.");
                }

                tenancy.Status = TenancyStatus.cancelled;

                // Update property status to available
                var property = await _unitOfWork.Property.GetByIdAsync(tenancy.PropertyId);
                if (property != null)
                {
                    property.Status = PropertyStatus.available;
                    await _unitOfWork.Property.Update(property);
                }

                await _unitOfWork.Tenancy.Update(tenancy);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Tenancy {tenancyId} cancelled successfully");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cancelling tenancy {tenancyId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteTenancyAsync(Guid tenancyId, Guid ownerId)
        {
            try
            {
                _logger.LogInformation($"Deleting tenancy {tenancyId}");

                var tenancy = await _unitOfWork.Tenancy.GetByIdAsync(tenancyId);
                if (tenancy == null || tenancy.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Tenancy with ID {tenancyId} not found.");
                }

                // Only owner can delete
                if (tenancy.OwnerId != ownerId)
                {
                    throw ErrorHelper.Forbidden("Only the property owner can delete tenancies.");
                }

                // Cannot delete active tenancy
                if (tenancy.Status == TenancyStatus.active)
                {
                    throw ErrorHelper.Conflict("Cannot delete active tenancy. Cancel it first.");
                }

                await _unitOfWork.Tenancy.SoftRemove(tenancy);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Tenancy {tenancyId} deleted successfully");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting tenancy {tenancyId}: {ex.Message}");
                throw;
            }
        }

        public async Task<int> UpdateExpiredTenanciesAsync()
        {
            try
            {
                _logger.LogInformation("Updating expired tenancies");

                var now = DateTime.UtcNow;
                var expiredTenancies = await _unitOfWork.Tenancy.GetQueryable()
                    .Where(t => t.Status == TenancyStatus.active &&
                                t.EndDate < now &&
                                !t.IsDeleted)
                    .ToListAsync();

                if (!expiredTenancies.Any())
                {
                    return 0;
                }

                foreach (var tenancy in expiredTenancies)
                {
                    tenancy.Status = TenancyStatus.expired;

                    // Update property status to available
                    var property = await _unitOfWork.Property.GetByIdAsync(tenancy.PropertyId);
                    if (property != null)
                    {
                        property.Status = PropertyStatus.available;
                        await _unitOfWork.Property.Update(property);
                    }
                }

                await _unitOfWork.Tenancy.UpdateRange(expiredTenancies);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"{expiredTenancies.Count} tenancies marked as expired");

                return expiredTenancies.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating expired tenancies: {ex.Message}");
                throw;
            }
        }

        #region Private Helper Methods

        private async Task<TenancyResponseDto?> MapToResponseDto(Tenancy tenancy)
        {
            var property = await _unitOfWork.Property.GetByIdAsync(tenancy.PropertyId);
            var tenant = await _unitOfWork.Account.GetByIdAsync(tenancy.TenantId);
            var owner = await _unitOfWork.Account.GetByIdAsync(tenancy.OwnerId);

            // Get latest utility reading for this tenancy
            var latestReading = await _unitOfWork.UtilityReading.GetQueryable()
                .Where(ur => ur.TenancyId == tenancy.Id && !ur.IsDeleted)
                .OrderByDescending(ur => ur.ReadingDate)
                .FirstOrDefaultAsync();

            return new TenancyResponseDto
            {
                Id = tenancy.Id,
                PropertyId = tenancy.PropertyId,
                PropertyTitle = property?.Title ?? string.Empty,
                PropertyAddress = property?.Address ?? string.Empty,
                TenantId = tenancy.TenantId,
                TenantName = tenant?.FullName ?? string.Empty,
                TenantEmail = tenant?.Email ?? string.Empty,
                OwnerId = tenancy.OwnerId,
                OwnerName = owner?.FullName ?? string.Empty,
                StartDate = tenancy.StartDate,
                EndDate = tenancy.EndDate,
                ContractUrl = tenancy.ContractUrl,
                Status = tenancy.Status,
                IsTenantConfirmed = tenancy.IsTenantConfirmed,
                ElectricUnitPrice = tenancy.ElectricUnitPrice,
                WaterUnitPrice = tenancy.WaterUnitPrice,
                ElectricOldIndex = tenancy.ElectricOldIndex,        // Ch? s? ban ??u
                WaterOldIndex = tenancy.WaterOldIndex,              // Ch? s? ban ??u
                LatestElectricIndex = latestReading?.ElectricNewIndex,
                LatestWaterIndex = latestReading?.WaterNewIndex,
                LatestReadingDate = latestReading?.ReadingDate,
                CreatedAt = tenancy.CreatedAt,
                UpdatedAt = tenancy.UpdatedAt,
                IsDeleted = tenancy.IsDeleted
            };
        }

        private bool IsValidStatusTransition(TenancyStatus currentStatus, TenancyStatus newStatus)
        {
            // Define valid status transitions
            return (currentStatus, newStatus) switch
            {
                (TenancyStatus.pending_confirmation, TenancyStatus.active) => true,
                (TenancyStatus.pending_confirmation, TenancyStatus.cancelled) => true,
                (TenancyStatus.active, TenancyStatus.expired) => true,
                (TenancyStatus.active, TenancyStatus.cancelled) => true,
                _ => false
            };
        }

        #endregion
    }
}
