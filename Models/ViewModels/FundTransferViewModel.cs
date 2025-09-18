using System.ComponentModel.DataAnnotations;

namespace InternetBanking.Models.ViewModels
{
    public class FundTransferViewModel
    {
        [Required]
        public int FromAccountId { get; set; }

        public List<Account> FromAccounts { get; set; } = new List<Account>();

        [Required(ErrorMessage = "Recipient account number is required")]
        [Display(Name = "To Account Number")]
        public string ToAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, 1000000, ErrorMessage = "Amount must be between $0.01 and $1,000,000")]
        [Display(Name = "Transfer Amount")]
        public decimal Amount { get; set; }

        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Transaction password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Transaction Password")]
        public string TransactionPassword { get; set; } = string.Empty;
    }
}
