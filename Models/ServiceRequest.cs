using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetBanking.Models
{
    public class ServiceRequest
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string RequestType { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public DateTime RequestDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [StringLength(200)]
        public string AdminResponse { get; set; } = string.Empty;

        public DateTime? ResponseDate { get; set; }

        // New fields for card request tracking
        [StringLength(20)]
        public string? CardStatus { get; set; } // pending, approved, rejected, ready

        public DateTime? EtaDate { get; set; } // Expected arrival date for approved cards
    }
}
