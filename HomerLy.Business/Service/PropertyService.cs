using Homerly.Business.Interfaces;
using Homerly.Business.Utils;
using Homerly.BusinessObject.DTOs.PropertyDTOs;
using Homerly.BusinessObject.Enums;
using Homerly.DataAccess.Entities;
using HomerLy.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Homerly.Business.Services
{
    public class PropertyService : IPropertyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PropertyService> _logger;

        public PropertyService(IUnitOfWork unitOfWork, ILogger<PropertyService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<PropertyResponseDto?> CreatePropertyAsync(Guid ownerId, CreatePropertyDto createPropertyDto)
        {
            try
            {
                _logger.LogInformation($"Creating new property for owner {ownerId}");

                if (createPropertyDto == null)
                {
                    throw ErrorHelper.BadRequest("Property data is required.");
                }

                // Verify owner exists and has Owner role
                var owner = await _unitOfWork.Account.GetByIdAsync(ownerId);
                if (owner == null || owner.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Owner with ID {ownerId} not found.");
                }

                if (owner.Role != RoleType.Owner)
                {
                    throw ErrorHelper.Forbidden("Only owners can create properties.");
                }

                if (!owner.IsOwnerApproved)
                {
                    throw ErrorHelper.Forbidden("Owner account must be approved before creating properties.");
                }

                // Create new property
                var property = new Property
                {
                    OwnerId = ownerId,
                    Title = createPropertyDto.Title,
                    Description = createPropertyDto.Description,
                    Address = createPropertyDto.Address,
                    MonthlyRent = createPropertyDto.MonthlyRent,
                    Price = createPropertyDto.Price,
                    AreaSqm = createPropertyDto.AreaSqm,
                    ImageUrl = createPropertyDto.ImageUrl,
                    Status = PropertyStatus.available // New properties are always available
                };

                await _unitOfWork.Property.AddAsync(property);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Property {property.Id} created successfully for owner {ownerId}");

                return MapToResponseDto(property);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating property for owner {ownerId}: {ex.Message}");
                throw;
            }
        }

        public async Task<PropertyResponseDto?> UpdatePropertyAsync(Guid propertyId, Guid ownerId, UpdatePropertyDto updatePropertyDto)
        {
            try
            {
                _logger.LogInformation($"Updating property {propertyId} for owner {ownerId}");

                if (updatePropertyDto == null)
                {
                    throw ErrorHelper.BadRequest("Update data is required.");
                }

                var property = await _unitOfWork.Property.GetByIdAsync(propertyId);

                if (property == null || property.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Property with ID {propertyId} not found.");
                }

                // Verify ownership
                if (property.OwnerId != ownerId)
                {
                    throw ErrorHelper.Forbidden("You can only update your own properties.");
                }

                // Update only provided fields
                if (!string.IsNullOrWhiteSpace(updatePropertyDto.Title))
                    property.Title = updatePropertyDto.Title;

                if (updatePropertyDto.Description != null)
                    property.Description = updatePropertyDto.Description;

                if (!string.IsNullOrWhiteSpace(updatePropertyDto.Address))
                    property.Address = updatePropertyDto.Address;

                if (updatePropertyDto.MonthlyRent.HasValue)
                    property.MonthlyRent = updatePropertyDto.MonthlyRent.Value;

                if (updatePropertyDto.Price.HasValue)
                    property.Price = updatePropertyDto.Price.Value;

                if (updatePropertyDto.AreaSqm.HasValue)
                    property.AreaSqm = updatePropertyDto.AreaSqm.Value;

                if (updatePropertyDto.ImageUrl != null)
                    property.ImageUrl = updatePropertyDto.ImageUrl;

                await _unitOfWork.Property.Update(property);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Property {propertyId} updated successfully");

                return MapToResponseDto(property);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating property {propertyId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdatePropertyStatusAsync(Guid propertyId, Guid ownerId, PropertyStatus newStatus)
        {
            try
            {
                _logger.LogInformation($"Updating property {propertyId} status to {newStatus}");

                var property = await _unitOfWork.Property.GetByIdAsync(propertyId);

                if (property == null || property.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Property with ID {propertyId} not found.");
                }

                // Verify ownership
                if (property.OwnerId != ownerId)
                {
                    throw ErrorHelper.Forbidden("You can only update status of your own properties.");
                }

                property.Status = newStatus;

                await _unitOfWork.Property.Update(property);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Property {propertyId} status updated to {newStatus}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating property status {propertyId}: {ex.Message}");
                throw;
            }
        }

        public async Task<PropertyResponseDto?> GetPropertyByIdAsync(Guid propertyId)
        {
            try
            {
                _logger.LogInformation($"Getting property {propertyId}");

                var property = await _unitOfWork.Property.GetByIdAsync(propertyId);

                if (property == null || property.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Property with ID {propertyId} not found.");
                }

                return MapToResponseDto(property);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting property {propertyId}: {ex.Message}");
                throw;
            }
        }

        public async Task<Pagination<PropertyResponseDto>> GetPropertiesAsync(
            int pageNumber = 1,
            int pageSize = 10,
            string? searchTerm = null,
            Guid? ownerId = null,
            PropertyStatus? status = null,
            decimal? minRent = null,
            decimal? maxRent = null,
            int? minArea = null,
            int? maxArea = null,
            string? address = null,
            bool? isDeleted = null,
            DateTime? createdFrom = null,
            DateTime? createdTo = null)
        {
            try
            {
                _logger.LogInformation("Getting properties with filters");

                var query = _unitOfWork.Property.GetQueryable().AsQueryable();

                // Apply isDeleted filter
                if (isDeleted.HasValue)
                {
                    query = query.Where(p => p.IsDeleted == isDeleted.Value);
                }
                else
                {
                    query = query.Where(p => !p.IsDeleted);
                }

                // Apply owner filter
                if (ownerId.HasValue)
                {
                    query = query.Where(p => p.OwnerId == ownerId.Value);
                }

                // Apply status filter
                if (status.HasValue)
                {
                    query = query.Where(p => p.Status == status.Value);
                }

                // Apply search term filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(p =>
                        p.Title.Contains(searchTerm) ||
                        p.Description.Contains(searchTerm) ||
                        p.Address.Contains(searchTerm));
                }

                // Apply address filter
                if (!string.IsNullOrWhiteSpace(address))
                {
                    query = query.Where(p => p.Address.Contains(address));
                }

                // Apply rent range filters
                if (minRent.HasValue)
                {
                    query = query.Where(p => p.MonthlyRent >= minRent.Value);
                }

                if (maxRent.HasValue)
                {
                    query = query.Where(p => p.MonthlyRent <= maxRent.Value);
                }

                // Apply area range filters
                if (minArea.HasValue)
                {
                    query = query.Where(p => p.AreaSqm >= minArea.Value);
                }

                if (maxArea.HasValue)
                {
                    query = query.Where(p => p.AreaSqm <= maxArea.Value);
                }

                // Apply date range filters
                if (createdFrom.HasValue)
                {
                    query = query.Where(p => p.CreatedAt >= createdFrom.Value);
                }

                if (createdTo.HasValue)
                {
                    query = query.Where(p => p.CreatedAt <= createdTo.Value);
                }

                query = query.OrderByDescending(p => p.CreatedAt);

                var totalCount = await query.CountAsync();
                var properties = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var propertyDtos = properties.Select(MapToResponseDto).ToList();

                return new Pagination<PropertyResponseDto>(propertyDtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting properties: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeletePropertyAsync(Guid propertyId, Guid ownerId)
        {
            try
            {
                _logger.LogInformation($"Deleting property {propertyId}");

                var property = await _unitOfWork.Property.GetByIdAsync(propertyId);

                if (property == null || property.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Property with ID {propertyId} not found.");
                }

                // Verify ownership
                if (property.OwnerId != ownerId)
                {
                    throw ErrorHelper.Forbidden("You can only delete your own properties.");
                }

                // Check if property is currently occupied
                if (property.Status == PropertyStatus.occupied)
                {
                    // Check for active tenancies
                    var activeTenancy = await _unitOfWork.Tenancy.FirstOrDefaultAsync(
                        t => t.PropertyId == propertyId && 
                             t.Status == TenancyStatus.active && 
                             !t.IsDeleted
                    );

                    if (activeTenancy != null)
                    {
                        throw ErrorHelper.Conflict("Cannot delete property with active tenancy.");
                    }
                }

                await _unitOfWork.Property.SoftRemove(property);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Property {propertyId} deleted successfully");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting property {propertyId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> MarkPropertyAsOccupiedAsync(Guid propertyId)
        {
            try
            {
                _logger.LogInformation($"Marking property {propertyId} as occupied");

                var property = await _unitOfWork.Property.GetByIdAsync(propertyId);

                if (property == null || property.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Property with ID {propertyId} not found.");
                }

                if (property.Status == PropertyStatus.occupied)
                {
                    throw ErrorHelper.Conflict("Property is already occupied.");
                }

                property.Status = PropertyStatus.occupied;

                await _unitOfWork.Property.Update(property);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Property {propertyId} marked as occupied");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error marking property {propertyId} as occupied: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> MarkPropertyAsAvailableAsync(Guid propertyId, Guid ownerId)
        {
            try
            {
                _logger.LogInformation($"Marking property {propertyId} as available");

                var property = await _unitOfWork.Property.GetByIdAsync(propertyId);

                if (property == null || property.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Property with ID {propertyId} not found.");
                }

                // Verify ownership
                if (property.OwnerId != ownerId)
                {
                    throw ErrorHelper.Forbidden("You can only update status of your own properties.");
                }

                // Check for active tenancies
                var activeTenancy = await _unitOfWork.Tenancy.FirstOrDefaultAsync(
                    t => t.PropertyId == propertyId && 
                         t.Status == TenancyStatus.active && 
                         !t.IsDeleted
                );

                if (activeTenancy != null)
                {
                    throw ErrorHelper.Conflict("Cannot mark property as available while it has an active tenancy.");
                }

                property.Status = PropertyStatus.available;

                await _unitOfWork.Property.Update(property);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Property {propertyId} marked as available");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error marking property {propertyId} as available: {ex.Message}");
                throw;
            }
        }

        #region Private Helper Methods

        private PropertyResponseDto MapToResponseDto(Property property)
        {
            return new PropertyResponseDto
            {
                Id = property.Id,
                OwnerId = property.OwnerId,
                Title = property.Title,
                Description = property.Description,
                Address = property.Address,
                MonthlyRent = property.MonthlyRent,
                Price = property.Price,
                AreaSqm = property.AreaSqm,
                Status = property.Status,
                ImageUrl = property.ImageUrl,
                CreatedAt = property.CreatedAt,
                UpdatedAt = property.UpdatedAt,
                IsDeleted = property.IsDeleted
            };
        }

        #endregion
    }
}
