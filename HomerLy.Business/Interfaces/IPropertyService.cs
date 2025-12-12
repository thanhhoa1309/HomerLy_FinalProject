using Homerly.Business.Utils;
using Homerly.BusinessObject.DTOs.PropertyDTOs;
using Homerly.BusinessObject.Enums;

namespace Homerly.Business.Interfaces
{
    public interface IPropertyService
    {
        Task<PropertyResponseDto?> CreatePropertyAsync(Guid ownerId, CreatePropertyDto createPropertyDto);

        Task<PropertyResponseDto?> UpdatePropertyAsync(Guid propertyId, Guid ownerId, UpdatePropertyDto updatePropertyDto);

        Task<bool> UpdatePropertyStatusAsync(Guid propertyId, Guid ownerId, PropertyStatus newStatus);

        Task<PropertyResponseDto?> GetPropertyByIdAsync(Guid propertyId);

        Task<Pagination<PropertyResponseDto>> GetPropertiesAsync(
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
            DateTime? createdTo = null);

        Task<bool> DeletePropertyAsync(Guid propertyId, Guid ownerId);

        Task<bool> MarkPropertyAsOccupiedAsync(Guid propertyId);

        Task<bool> MarkPropertyAsAvailableAsync(Guid propertyId, Guid ownerId);
    }
}
