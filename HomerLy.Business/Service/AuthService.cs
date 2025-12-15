using Homerly.Business.Interfaces;
using Homerly.Business.Utils;
using Homerly.BusinessObject.DTOs.AuthDTOs;
using Homerly.BusinessObject.DTOs.UserDTOs;
using Homerly.BusinessObject.Enums;
using Homerly.DataAccess.Entities;
using HomerLy.DataAccess.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Homerly.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger _logger;

        public AuthService(IUnitOfWork unitOfWork, ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequestDto, IConfiguration configuration)
        {
            try
            {
                _logger.LogInformation($"Attempting login for {loginRequestDto?.Email}");

                if (loginRequestDto == null || string.IsNullOrWhiteSpace(loginRequestDto.Email) || string.IsNullOrWhiteSpace(loginRequestDto.Password))
                {
                    _logger.LogWarning("Login failed: missing email or password.");
                    throw ErrorHelper.BadRequest("Email and password are required.");
                }

                var account = await _unitOfWork.Account.FirstOrDefaultAsync(u => u.Email == loginRequestDto.Email && !u.IsDeleted);

                if (account == null)
                {
                    _logger.LogWarning($"Login failed: account {loginRequestDto.Email} not found or deleted.");
                    throw ErrorHelper.NotFound("Account not found or has been deleted.");
                }

                var passwordHasher = new PasswordHasher();
                if (!passwordHasher.VerifyPassword(loginRequestDto.Password, account.PasswordHash))
                {
                    _logger.LogWarning($"Login failed: invalid password for {loginRequestDto.Email}.");
                    throw ErrorHelper.Unauthorized("Invalid email or password.");
                }

                var jwtToken = JwtUtils.GenerateJwtToken(
                    account.Id,
                    account.Email,
                    account.Role.ToString(),
                    configuration,
                    TimeSpan.FromHours(8)
                );

                var response = new LoginResponseDto
                {
                    Token = jwtToken
                };

                _logger.LogInformation($"Login successful for {account.Email}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Login error for {loginRequestDto?.Email}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> LogoutAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation($"Account with ID {userId} logged out");
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Logout error for account {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task<AccountResponseDto?> RegisterUserAsync(AccountRegistrationDto accountRegistrationDto)
        {
            try
            {
                _logger.LogInformation("Registering new account");

                if (accountRegistrationDto == null)
                {
                    throw ErrorHelper.BadRequest("Registration data is required.");
                }

                if (await AccountExistsAsync(accountRegistrationDto.Email))
                {
                    _logger.LogWarning($"Registration failed: email {accountRegistrationDto.Email} already in use.");
                    throw ErrorHelper.Conflict($"Email {accountRegistrationDto.Email} is already registered.");
                }

                var hashedPassword = new PasswordHasher().HashPassword(accountRegistrationDto.Password);

                var account = new Account
                {
                    FullName = accountRegistrationDto.FullName,
                    Email = accountRegistrationDto.Email,
                    PhoneNumber = accountRegistrationDto.Phone,
                    CccdNumber = accountRegistrationDto.CccdNumber,
                    Role = RoleType.User,
                    PasswordHash = hashedPassword ?? throw ErrorHelper.Internal("Password hashing failed."),
                    IsOwnerApproved = false
                };

                await _unitOfWork.Account.AddAsync(account);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Account {account.Email} registered successfully with ID {account.Id}.");

                var accountResponseDto = new AccountResponseDto
                {
                    Id = account.Id,
                    FullName = account.FullName,
                    Email = account.Email,
                    Phone = account.PhoneNumber,
                    Role = account.Role,
                    CccdNumber = account.CccdNumber ?? string.Empty,
                    CreatedAt = account.CreatedAt,
                    UpdatedAt = account.UpdatedAt,
                    IsDeleted = account.IsDeleted
                };

                return accountResponseDto;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating account: {ex.Message}");
                throw;
            }
        }

        private async Task<bool> AccountExistsAsync(string email)
        {
            var accounts = await _unitOfWork.Account.GetAllAsync();
            return accounts.Any(a => a.Email == email && !a.IsDeleted);
        }
    }
}
