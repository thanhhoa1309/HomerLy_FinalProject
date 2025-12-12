using Homerly.BusinessObject.Enums;
using HomerLy.DataAccess.Entities;
using System.ComponentModel.DataAnnotations;

namespace Homerly.DataAccess.Entities
{

    public class Account : BaseEntity
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string PasswordHash { get; set; }
        public RoleType Role { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string CccdNumber { get; set; }
        public bool IsOwnerApproved { get; set; } = false;


        public virtual ICollection<Property> Properties { get; set; }
    }
}
