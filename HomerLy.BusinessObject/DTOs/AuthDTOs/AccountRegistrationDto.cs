using Homerly.BusinessObject.Enums;

namespace Homerly.BusinessObject.DTOs.AuthDTOs
{
    public class AccountRegistrationDto
    {
        public required string Email { get; set; }
        public required string Phone { get; set; }
        public required string FullName { get; set; }
        public RoleType Role { get; set; } = RoleType.User; // Default role is User
        public required string CccdNumber { get; set; }
        public required string Password { get; set; }
    }
}
