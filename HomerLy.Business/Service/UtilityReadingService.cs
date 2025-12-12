using Homerly.Business.Interfaces;
using Homerly.Business.Utils;
using Homerly.BusinessObject.DTOs.UtilityReadingDTOs;
using Homerly.DataAccess.Entities;
using HomerLy.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Homerly.Business.Services
{
    public class UtilityReadingService : IUtilityReadingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UtilityReadingService> _logger;

        public UtilityReadingService(IUnitOfWork unitOfWork, ILogger<UtilityReadingService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<UtilityReadingResponseDto?> CreateUtilityReadingAsync(Guid userId, CreateUtilityReadingDto createDto)
        {
            try
            {
                _logger.LogInformation($"Creating utility reading for property {createDto.PropertyId}");

                if (createDto == null)
                {
                    throw ErrorHelper.BadRequest("Utility reading data is required.");
                }

                // Verify property exists
                var property = await _unitOfWork.Property.GetByIdAsync(createDto.PropertyId);
                if (property == null || property.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Property with ID {createDto.PropertyId} not found.");
                }

                // Verify tenancy exists and user has permission
                var tenancy = await _unitOfWork.Tenancy.GetByIdAsync(createDto.TenancyId);
                if (tenancy == null || tenancy.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Tenancy with ID {createDto.TenancyId} not found.");
                }

                // Verify property matches tenancy
                if (tenancy.PropertyId != createDto.PropertyId)
                {
                    throw ErrorHelper.BadRequest("Tenancy does not belong to this property.");
                }

                // Check permission: Only owner or tenant of the tenancy can create reading
                if (tenancy.OwnerId != userId && tenancy.TenantId != userId)
                {
                    throw ErrorHelper.Forbidden("You don't have permission to create utility reading for this property.");
                }

                // Get the latest utility reading for this property and tenancy
                var latestReading = await _unitOfWork.UtilityReading.GetQueryable()
                    .Where(ur => ur.PropertyId == createDto.PropertyId 
                                 && ur.TenancyId == createDto.TenancyId 
                                 && !ur.IsDeleted)
                    .OrderByDescending(ur => ur.ReadingDate)
                    .FirstOrDefaultAsync();

                int electricOldIndex;
                int waterOldIndex;

                if (latestReading != null)
                {
                    // Có reading tr??c ?ó: S? d?ng NewIndex c?a tháng tr??c làm OldIndex tháng này
                    electricOldIndex = latestReading.ElectricNewIndex;
                    waterOldIndex = latestReading.WaterNewIndex;

                    _logger.LogInformation($"Using previous reading - Electric: {electricOldIndex}, Water: {waterOldIndex}");

                    // Validate: New index must be >= old index
                    if (createDto.ElectricNewIndex < electricOldIndex)
                    {
                        throw ErrorHelper.BadRequest($"Electric new index ({createDto.ElectricNewIndex}) must be greater than or equal to the previous reading ({electricOldIndex}).");
                    }

                    if (createDto.WaterNewIndex < waterOldIndex)
                    {
                        throw ErrorHelper.BadRequest($"Water new index ({createDto.WaterNewIndex}) must be greater than or equal to the previous reading ({waterOldIndex}).");
                    }
                }
                else
                {
                    // Không có reading tr??c ?ó: L?y t? Tenancy (ch? s? ban ??u khi b?t ??u thuê)
                    electricOldIndex = tenancy.ElectricOldIndex;
                    waterOldIndex = tenancy.WaterOldIndex;

                    _logger.LogInformation($"First reading - Using Tenancy initial index - Electric: {electricOldIndex}, Water: {waterOldIndex}");

                    // Validate: New index must be >= tenancy's initial index
                    if (createDto.ElectricNewIndex < electricOldIndex)
                    {
                        throw ErrorHelper.BadRequest($"Electric new index ({createDto.ElectricNewIndex}) must be greater than or equal to the initial index from tenancy ({electricOldIndex}).");
                    }

                    if (createDto.WaterNewIndex < waterOldIndex)
                    {
                        throw ErrorHelper.BadRequest($"Water new index ({createDto.WaterNewIndex}) must be greater than or equal to the initial index from tenancy ({waterOldIndex}).");
                    }
                }

                // Create new utility reading
                var utilityReading = new UtilityReading
                {
                    PropertyId = createDto.PropertyId,
                    TenancyId = createDto.TenancyId,
                    ReadingDate = createDto.ReadingDate,
                    ElectricOldIndex = electricOldIndex,
                    ElectricNewIndex = createDto.ElectricNewIndex,
                    WaterOldIndex = waterOldIndex,
                    WaterNewIndex = createDto.WaterNewIndex,
                    IsCharged = false,
                    CreatedById = userId
                };

                await _unitOfWork.UtilityReading.AddAsync(utilityReading);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Utility reading {utilityReading.Id} created successfully");

                return MapToResponseDto(utilityReading);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating utility reading: {ex.Message}");
                throw;
            }
        }

        public async Task<UtilityReadingResponseDto?> UpdateUtilityReadingAsync(Guid readingId, Guid userId, UpdateUtilityReadingDto updateDto)
        {
            try
            {
                _logger.LogInformation($"Updating utility reading {readingId}");

                if (updateDto == null)
                {
                    throw ErrorHelper.BadRequest("Update data is required.");
                }

                var reading = await _unitOfWork.UtilityReading.GetByIdAsync(readingId);
                if (reading == null || reading.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Utility reading with ID {readingId} not found.");
                }

                // Check if already charged
                if (reading.IsCharged)
                {
                    throw ErrorHelper.Conflict("Cannot update utility reading that has already been charged.");
                }

                // Verify permission
                var tenancy = await _unitOfWork.Tenancy.GetByIdAsync(reading.TenancyId);
                if (tenancy == null)
                {
                    throw ErrorHelper.NotFound("Tenancy not found.");
                }

                if (tenancy.OwnerId != userId && tenancy.TenantId != userId)
                {
                    throw ErrorHelper.Forbidden("You don't have permission to update this utility reading.");
                }

                // Validate: New index must be >= old index
                if (updateDto.ElectricNewIndex < reading.ElectricOldIndex)
                {
                    throw ErrorHelper.BadRequest($"Electric new index ({updateDto.ElectricNewIndex}) must be greater than or equal to old index ({reading.ElectricOldIndex}).");
                }

                if (updateDto.WaterNewIndex < reading.WaterOldIndex)
                {
                    throw ErrorHelper.BadRequest($"Water new index ({updateDto.WaterNewIndex}) must be greater than or equal to old index ({reading.WaterOldIndex}).");
                }

                // Update fields
                reading.ElectricNewIndex = updateDto.ElectricNewIndex;
                reading.WaterNewIndex = updateDto.WaterNewIndex;

                if (updateDto.ReadingDate.HasValue)
                {
                    reading.ReadingDate = updateDto.ReadingDate.Value;
                }

                await _unitOfWork.UtilityReading.Update(reading);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Utility reading {readingId} updated successfully");

                return MapToResponseDto(reading);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating utility reading {readingId}: {ex.Message}");
                throw;
            }
        }

        public async Task<UtilityReadingResponseDto?> GetUtilityReadingByIdAsync(Guid readingId, Guid userId)
        {
            try
            {
                _logger.LogInformation($"Getting utility reading {readingId}");

                var reading = await _unitOfWork.UtilityReading.GetByIdAsync(readingId);
                if (reading == null || reading.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Utility reading with ID {readingId} not found.");
                }

                // Verify permission
                var tenancy = await _unitOfWork.Tenancy.GetByIdAsync(reading.TenancyId);
                if (tenancy == null)
                {
                    throw ErrorHelper.NotFound("Tenancy not found.");
                }

                if (tenancy.OwnerId != userId && tenancy.TenantId != userId)
                {
                    throw ErrorHelper.Forbidden("You don't have permission to view this utility reading.");
                }

                return MapToResponseDto(reading);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting utility reading {readingId}: {ex.Message}");
                throw;
            }
        }

        public async Task<Pagination<UtilityReadingResponseDto>> GetUtilityReadingsByPropertyIdAsync(
            Guid propertyId,
            Guid userId,
            int pageNumber = 1,
            int pageSize = 10,
            bool? isCharged = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                _logger.LogInformation($"Getting utility readings for property {propertyId}");

                // Verify property exists
                var property = await _unitOfWork.Property.GetByIdAsync(propertyId);
                if (property == null || property.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Property with ID {propertyId} not found.");
                }

                // Get active tenancy for this property
                var activeTenancy = await _unitOfWork.Tenancy.FirstOrDefaultAsync(
                    t => t.PropertyId == propertyId && !t.IsDeleted
                );

                if (activeTenancy == null)
                {
                    throw ErrorHelper.NotFound("No tenancy found for this property.");
                }

                // Check permission: Only owner or tenant can view
                if (activeTenancy.OwnerId != userId && activeTenancy.TenantId != userId)
                {
                    throw ErrorHelper.Forbidden("You don't have permission to view utility readings for this property.");
                }

                var query = _unitOfWork.UtilityReading.GetQueryable()
                    .Where(ur => ur.PropertyId == propertyId && !ur.IsDeleted);

                // Apply filters
                if (isCharged.HasValue)
                {
                    query = query.Where(ur => ur.IsCharged == isCharged.Value);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(ur => ur.ReadingDate >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(ur => ur.ReadingDate <= toDate.Value);
                }

                query = query.OrderByDescending(ur => ur.ReadingDate);

                var totalCount = await query.CountAsync();
                var readings = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var readingDtos = readings.Select(MapToResponseDto).ToList();

                return new Pagination<UtilityReadingResponseDto>(readingDtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting utility readings for property {propertyId}: {ex.Message}");
                throw;
            }
        }

        public async Task<Pagination<UtilityReadingResponseDto>> GetUtilityReadingsByTenancyIdAsync(
            Guid tenancyId,
            Guid userId,
            int pageNumber = 1,
            int pageSize = 10,
            bool? isCharged = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                _logger.LogInformation($"Getting utility readings for tenancy {tenancyId}");

                // Verify tenancy exists
                var tenancy = await _unitOfWork.Tenancy.GetByIdAsync(tenancyId);
                if (tenancy == null || tenancy.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Tenancy with ID {tenancyId} not found.");
                }

                // Check permission: Only owner or tenant can view
                if (tenancy.OwnerId != userId && tenancy.TenantId != userId)
                {
                    throw ErrorHelper.Forbidden("You don't have permission to view utility readings for this tenancy.");
                }

                var query = _unitOfWork.UtilityReading.GetQueryable()
                    .Where(ur => ur.TenancyId == tenancyId && !ur.IsDeleted);

                // Apply filters
                if (isCharged.HasValue)
                {
                    query = query.Where(ur => ur.IsCharged == isCharged.Value);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(ur => ur.ReadingDate >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(ur => ur.ReadingDate <= toDate.Value);
                }

                query = query.OrderByDescending(ur => ur.ReadingDate);

                var totalCount = await query.CountAsync();
                var readings = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var readingDtos = readings.Select(MapToResponseDto).ToList();

                return new Pagination<UtilityReadingResponseDto>(readingDtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting utility readings for tenancy {tenancyId}: {ex.Message}");
                throw;
            }
        }

        public async Task<UtilityReadingResponseDto?> GetLatestUtilityReadingAsync(Guid propertyId, Guid userId)
        {
            try
            {
                _logger.LogInformation($"Getting latest utility reading for property {propertyId}");

                var property = await _unitOfWork.Property.GetByIdAsync(propertyId);
                if (property == null || property.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Property with ID {propertyId} not found.");
                }

                var activeTenancy = await _unitOfWork.Tenancy.FirstOrDefaultAsync(
                    t => t.PropertyId == propertyId && !t.IsDeleted
                );

                if (activeTenancy == null)
                {
                    throw ErrorHelper.NotFound("No tenancy found for this property.");
                }

                if (activeTenancy.OwnerId != userId && activeTenancy.TenantId != userId)
                {
                    throw ErrorHelper.Forbidden("You don't have permission to view utility readings for this property.");
                }

                var latestReading = await _unitOfWork.UtilityReading.GetQueryable()
                    .Where(ur => ur.PropertyId == propertyId && !ur.IsDeleted)
                    .OrderByDescending(ur => ur.ReadingDate)
                    .FirstOrDefaultAsync();

                if (latestReading == null)
                {
                    return null;
                }

                return MapToResponseDto(latestReading);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting latest utility reading for property {propertyId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteUtilityReadingAsync(Guid readingId, Guid userId)
        {
            try
            {
                _logger.LogInformation($"Deleting utility reading {readingId}");

                var reading = await _unitOfWork.UtilityReading.GetByIdAsync(readingId);
                if (reading == null || reading.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Utility reading with ID {readingId} not found.");
                }

                if (reading.IsCharged)
                {
                    throw ErrorHelper.Conflict("Cannot delete utility reading that has already been charged.");
                }

                var tenancy = await _unitOfWork.Tenancy.GetByIdAsync(reading.TenancyId);
                if (tenancy == null)
                {
                    throw ErrorHelper.NotFound("Tenancy not found.");
                }

                if (tenancy.OwnerId != userId)
                {
                    throw ErrorHelper.Forbidden("Only the property owner can delete utility readings.");
                }

                await _unitOfWork.UtilityReading.SoftRemove(reading);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Utility reading {readingId} deleted successfully");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting utility reading {readingId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> MarkAsChargedAsync(Guid readingId, Guid userId)
        {
            try
            {
                _logger.LogInformation($"Marking utility reading {readingId} as charged");

                var reading = await _unitOfWork.UtilityReading.GetByIdAsync(readingId);
                if (reading == null || reading.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Utility reading with ID {readingId} not found.");
                }

                // Verify permission - only owner can mark as charged
                var tenancy = await _unitOfWork.Tenancy.GetByIdAsync(reading.TenancyId);
                if (tenancy == null)
                {
                    throw ErrorHelper.NotFound("Tenancy not found.");
                }

                if (tenancy.OwnerId != userId)
                {
                    throw ErrorHelper.Forbidden("Only the property owner can mark utility readings as charged.");
                }

                if (reading.IsCharged)
                {
                    throw ErrorHelper.Conflict("Utility reading is already marked as charged.");
                }

                reading.IsCharged = true;

                await _unitOfWork.UtilityReading.Update(reading);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Utility reading {readingId} marked as charged successfully");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error marking utility reading {readingId} as charged: {ex.Message}");
                throw;
            }
        }

        #region Private Helper Methods

        private UtilityReadingResponseDto MapToResponseDto(UtilityReading reading)
        {
            return new UtilityReadingResponseDto
            {
                Id = reading.Id,
                PropertyId = reading.PropertyId,
                TenancyId = reading.TenancyId,
                ReadingDate = reading.ReadingDate,
                ElectricOldIndex = reading.ElectricOldIndex,
                ElectricNewIndex = reading.ElectricNewIndex,
                ElectricUsage = reading.ElectricNewIndex - reading.ElectricOldIndex,
                WaterOldIndex = reading.WaterOldIndex,
                WaterNewIndex = reading.WaterNewIndex,
                WaterUsage = reading.WaterNewIndex - reading.WaterOldIndex,
                IsCharged = reading.IsCharged,
                CreatedById = reading.CreatedById,
                CreatedAt = reading.CreatedAt,
                UpdatedAt = reading.UpdatedAt,
                IsDeleted = reading.IsDeleted
            };
        }

        #endregion
    }
}
