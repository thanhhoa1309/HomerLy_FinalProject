using Homerly.Business.Interfaces;
using Homerly.Business.Utils;
using Homerly.BusinessObject.DTOs.UserDTOs;
using Homerly.BusinessObject.Enums;
using Homerly.DataAccess.Entities;
using HomerLy.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Homerly.Business.Services
{
    public class AccountService : IAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AccountService> _logger;

        public AccountService(IUnitOfWork unitOfWork, ILogger<AccountService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<AccountResponseDto?> GetAccountByIdAsync(Guid accountId)
        {
            try
            {
                _logger.LogInformation($"Getting account with ID: {accountId}");

                var account = await _unitOfWork.Account.GetByIdAsync(accountId);

                if (account == null || account.IsDeleted)
                {
                    _logger.LogWarning($"Account with ID {accountId} not found or deleted");
                    throw ErrorHelper.NotFound($"Account with ID {accountId} not found.");
                }

                return MapToResponseDto(account);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting account {accountId}: {ex.Message}");
                throw;
            }
        }

        public async Task<Pagination<AccountResponseDto>> GetAccountsAsync(
            int pageNumber = 1,
            int pageSize = 10,
            string? searchTerm = null,
            RoleType? role = null,
            bool? isDeleted = null,
            bool? isOwnerApproved = null,
            DateTime? createdFrom = null,
            DateTime? createdTo = null)
        {
            try
            {
                _logger.LogInformation($"Getting accounts - Page: {pageNumber}, PageSize: {pageSize}");

                var query = _unitOfWork.Account.GetQueryable().AsQueryable();

                // Apply isDeleted filter
                if (isDeleted.HasValue)
                {
                    query = query.Where(a => a.IsDeleted == isDeleted.Value);
                }
                else
                {
                    query = query.Where(a => !a.IsDeleted);
                }

                // Apply search term filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(a =>
                        a.FullName.Contains(searchTerm) ||
                        a.Email.Contains(searchTerm) ||
                        a.PhoneNumber.Contains(searchTerm) ||
                        (a.CccdNumber != null && a.CccdNumber.Contains(searchTerm)));
                }

                // Apply role filter
                if (role.HasValue)
                {
                    query = query.Where(a => a.Role == role.Value);
                }

                // Apply owner approval filter
                if (isOwnerApproved.HasValue)
                {
                    query = query.Where(a => a.IsOwnerApproved == isOwnerApproved.Value);
                }

                // Apply date range filters
                if (createdFrom.HasValue)
                {
                    query = query.Where(a => a.CreatedAt >= createdFrom.Value);
                }

                if (createdTo.HasValue)
                {
                    query = query.Where(a => a.CreatedAt <= createdTo.Value);
                }

                query = query.OrderByDescending(a => a.CreatedAt);

                var totalCount = await query.CountAsync();

                var accounts = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var accountDtos = accounts.Select(MapToResponseDto).ToList();

                return new Pagination<AccountResponseDto>(accountDtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting accounts: {ex.Message}");
                throw;
            }
        }

        public async Task<AccountResponseDto?> UpdateAccountAsync(Guid accountId, Guid currentUserId, UpdateAccountRequestDto updateDto)
        {
            try
            {
                _logger.LogInformation($"User {currentUserId} attempting to update account {accountId}");

                if (updateDto == null)
                {
                    throw ErrorHelper.BadRequest("Update data is required.");
                }

                // Permission check: Users can only update their own account
                if (accountId != currentUserId)
                {
                    _logger.LogWarning($"User {currentUserId} attempted to update account {accountId} - Permission denied");
                    throw ErrorHelper.Forbidden("You can only update your own account.");
                }

                var account = await _unitOfWork.Account.GetByIdAsync(accountId);

                if (account == null || account.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Account with ID {accountId} not found.");
                }

                // Update basic information (CCCD cannot be updated)
                account.FullName = updateDto.FullName;
                account.PhoneNumber = updateDto.Phone;

                // Handle password change if provided
                if (!string.IsNullOrWhiteSpace(updateDto.CurrentPassword) && !string.IsNullOrWhiteSpace(updateDto.NewPassword))
                {
                    var passwordHasher = new PasswordHasher();
                    
                    // Verify current password
                    if (!passwordHasher.VerifyPassword(updateDto.CurrentPassword, account.PasswordHash))
                    {
                        throw ErrorHelper.BadRequest("Current password is incorrect.");
                    }

                    // Hash and set new password
                    account.PasswordHash = passwordHasher.HashPassword(updateDto.NewPassword);
                    _logger.LogInformation($"Password updated for account {accountId}");
                }

                await _unitOfWork.Account.Update(account);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Account {accountId} updated successfully by user {currentUserId}");

                return MapToResponseDto(account);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating account {accountId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteAccountAsync(Guid accountId)
        {
            try
            {
                _logger.LogInformation($"Deleting account with ID: {accountId}");

                var account = await _unitOfWork.Account.GetByIdAsync(accountId);

                if (account == null)
                {
                    throw ErrorHelper.NotFound($"Account with ID {accountId} not found.");
                }

                if (account.IsDeleted)
                {
                    throw ErrorHelper.BadRequest($"Account with ID {accountId} is already deleted.");
                }

                await _unitOfWork.Account.SoftRemove(account);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Account {accountId} deleted successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting account {accountId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> RestoreAccountAsync(Guid accountId)
        {
            try
            {
                _logger.LogInformation($"Restoring account with ID: {accountId}");

                var account = await _unitOfWork.Account.GetByIdAsync(accountId);

                if (account == null)
                {
                    throw ErrorHelper.NotFound($"Account with ID {accountId} not found.");
                }

                if (!account.IsDeleted)
                {
                    throw ErrorHelper.BadRequest($"Account with ID {accountId} is not deleted.");
                }

                account.IsDeleted = false;
                account.DeletedAt = null;
                account.DeletedBy = null;

                await _unitOfWork.Account.Update(account);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Account {accountId} restored successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error restoring account {accountId}: {ex.Message}");
                throw;
            }
        }

        public async Task<AccountResponseDto?> GetCurrentAccountProfileAsync(Guid currentUserId)
        {
            try
            {
                _logger.LogInformation($"Getting profile for current user: {currentUserId}");

                return await GetAccountByIdAsync(currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting current account profile: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ChangeAccountRoleAsync(Guid accountId, string newRole)
        {
            try
            {
                _logger.LogInformation($"Changing role for account {accountId} to {newRole}");

                if (!Enum.TryParse<RoleType>(newRole, true, out var roleType))
                {
                    throw ErrorHelper.BadRequest($"Invalid role: {newRole}");
                }

                var account = await _unitOfWork.Account.GetByIdAsync(accountId);

                if (account == null || account.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Account with ID {accountId} not found.");
                }

                account.Role = roleType;

                // If changing to Owner, set IsOwnerApproved to false
                if (roleType == RoleType.Owner)
                {
                    account.IsOwnerApproved = false;
                }

                await _unitOfWork.Account.Update(account);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Role changed successfully for account {accountId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error changing role for account {accountId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ApproveOwnerAccountAsync(Guid accountId)
        {
            try
            {
                _logger.LogInformation($"Approving owner account: {accountId}");

                var account = await _unitOfWork.Account.GetByIdAsync(accountId);

                if (account == null || account.IsDeleted)
                {
                    throw ErrorHelper.NotFound($"Account with ID {accountId} not found.");
                }

                if (account.Role != RoleType.Owner)
                {
                    throw ErrorHelper.BadRequest($"Account {accountId} is not an owner account.");
                }

                account.IsOwnerApproved = true;

                await _unitOfWork.Account.Update(account);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Owner account {accountId} approved successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error approving owner account {accountId}: {ex.Message}");
                throw;
            }
        }

        #region Private Helper Methods

        private AccountResponseDto MapToResponseDto(Account account)
        {
            return new AccountResponseDto
            {
                Id = account.Id,
                Email = account.Email,
                Phone = account.PhoneNumber,
                FullName = account.FullName,
                Role = account.Role,
                CccdNumber = account.CccdNumber ?? string.Empty,
                CreatedAt = account.CreatedAt,
                UpdatedAt = account.UpdatedAt,
                IsDeleted = account.IsDeleted
            };
        }

        #endregion
    }
}
