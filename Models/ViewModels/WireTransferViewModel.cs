using System.ComponentModel.DataAnnotations;

namespace InternetBanking.Models.ViewModels
{
    public class WireTransferViewModel
    {
        [Required]
        public int AccountId { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(1, 1000000, ErrorMessage = "Amount must be between $1 and $1,000,000")]
        [Display(Name = "Wire Amount")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Sender name is required")]
        [StringLength(100)]
        [Display(Name = "Sender Name")]
        public string SenderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sender bank is required")]
        [StringLength(100)]
        [Display(Name = "Sender Bank")]
        public string SenderBank { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Wire Reference Number")]
        public string? WireReference { get; set; }

        [Required(ErrorMessage = "Transaction password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Transaction Password")]
        public string TransactionPassword { get; set; } = string.Empty;
    }
}
