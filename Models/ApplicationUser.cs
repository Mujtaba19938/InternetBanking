using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace InternetBanking.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(200)]
        public string Address { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LastFailedLogin { get; set; }

        public bool IsAccountLocked { get; set; } = false;

        public DateTime? AccountLockedUntil { get; set; }

        public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

        public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
    }
}
