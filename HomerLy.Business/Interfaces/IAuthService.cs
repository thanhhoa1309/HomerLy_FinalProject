using Homerly.BusinessObject.DTOs.AuthDTOs;
using Homerly.BusinessObject.DTOs.UserDTOs;
using Microsoft.Extensions.Configuration;

namespace Homerly.Business.Interfaces
{
    public interface IAuthService
    {
        Task<AccountResponseDto?> RegisterUserAsync(AccountRegistrationDto accountRegistrationDto);
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequestDto, IConfiguration configuration);
        Task<bool> LogoutAsync(Guid AccountId);
    }
}
