using Homerly.Business.Utils;
using Homerly.BusinessObject.DTOs.UtilityReadingDTOs;

namespace Homerly.Business.Interfaces
{
    public interface IUtilityReadingService
    {

        Task<UtilityReadingResponseDto?> CreateUtilityReadingAsync(Guid userId, CreateUtilityReadingDto createDto);


        Task<UtilityReadingResponseDto?> UpdateUtilityReadingAsync(Guid readingId, Guid userId, UpdateUtilityReadingDto updateDto);


        Task<UtilityReadingResponseDto?> GetUtilityReadingByIdAsync(Guid readingId, Guid userId);


        Task<Pagination<UtilityReadingResponseDto>> GetUtilityReadingsByPropertyIdAsync(
            Guid propertyId,
            Guid userId,
            int pageNumber = 1,
            int pageSize = 10,
            bool? isCharged = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);


        Task<Pagination<UtilityReadingResponseDto>> GetUtilityReadingsByTenancyIdAsync(
            Guid tenancyId,
            Guid userId,
            int pageNumber = 1,
            int pageSize = 10,
            bool? isCharged = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);


        Task<UtilityReadingResponseDto?> GetLatestUtilityReadingAsync(Guid propertyId, Guid userId);


        Task<bool> DeleteUtilityReadingAsync(Guid readingId, Guid userId);


        Task<bool> MarkAsChargedAsync(Guid readingId, Guid userId);
    }
}
