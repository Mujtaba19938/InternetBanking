using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetBanking.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public int FromAccountId { get; set; }

        [ForeignKey("FromAccountId")]
        public virtual Account FromAccount { get; set; } = null!;

        public int? ToAccountId { get; set; }

        [ForeignKey("ToAccountId")]
        public virtual Account? ToAccount { get; set; }

        [Required]
        [StringLength(20)]
        public string ToAccountNumber { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string TransactionType { get; set; } = string.Empty;

        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [StringLength(100)]
        public string ReferenceNumber { get; set; } = string.Empty;
    }
}
