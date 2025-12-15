using Homerly.BusinessObject.Enums;

namespace Homerly.BusinessObject.DTOs.AuthDTOs
{
    public class AccountRegistrationDto
    {
        public required string Email { get; set; }
        public required string Phone { get; set; }
        public required string FullName { get; set; }
        public required RoleType Role { get; set; }
        public required string CccdNumber { get; set; }
        public required string Password { get; set; }
    }
}
