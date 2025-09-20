using System.ComponentModel.DataAnnotations;

namespace InternetBanking.Models
{
    public class AdminCredentials
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(256)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        
        [StringLength(450)]
        public string? UpdatedBy { get; set; }
        
        public bool IsDefault { get; set; } = true;
    }
}
