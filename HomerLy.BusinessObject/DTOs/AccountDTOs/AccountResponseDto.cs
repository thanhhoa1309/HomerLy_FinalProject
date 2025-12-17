using Homerly.BusinessObject.Enums;


namespace Homerly.BusinessObject.DTOs.UserDTOs
{
    public class AccountResponseDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string FullName { get; set; }
        public RoleType Role { get; set; }
        public string CccdNumber { get; set; }
        public bool IsOwnerApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
