using Homerly.BusinessObject.Enums;

namespace Homerly.BusinessObject.DTOs.AuthDTOs
{
    public class AccountDto
    {
        public string Email { get; set; }
        public string Phone { get; set; }
        public string FullName { get; set; }
        public RoleType Role { get; set; }
        public string CccdNumber { get; set; }
    }
}
