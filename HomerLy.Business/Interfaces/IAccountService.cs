using Homerly.Business.Utils;
using Homerly.BusinessObject.DTOs.UserDTOs;
using Homerly.BusinessObject.Enums;

namespace Homerly.Business.Interfaces
{
    public interface IAccountService
    {
        Task<AccountResponseDto?> GetAccountByIdAsync(Guid accountId);

        Task<Pagination<AccountResponseDto>> GetAccountsAsync(
            int pageNumber = 1,
            int pageSize = 10,
            string? searchTerm = null,
            RoleType? role = null,
            bool? isDeleted = null,
            bool? isOwnerApproved = null,
            DateTime? createdFrom = null,
            DateTime? createdTo = null);

        Task<AccountResponseDto?> UpdateAccountAsync(Guid accountId, Guid currentUserId, UpdateAccountRequestDto updateDto);

        Task<bool> DeleteAccountAsync(Guid accountId);

        Task<bool> RestoreAccountAsync(Guid accountId);

        Task<AccountResponseDto?> GetCurrentAccountProfileAsync(Guid currentUserId);

        Task<bool> ChangeAccountRoleAsync(Guid accountId, string newRole);

        Task<bool> ApproveOwnerAccountAsync(Guid accountId);
    }
}
