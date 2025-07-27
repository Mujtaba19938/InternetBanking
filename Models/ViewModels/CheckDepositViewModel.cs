using System.ComponentModel.DataAnnotations;

namespace InternetBanking.Models.ViewModels
{
    public class CheckDepositViewModel
    {
        [Required]
        public int AccountId { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(1, 100000, ErrorMessage = "Amount must be between $1 and $100,000")]
        [Display(Name = "Check Amount")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Check number is required")]
        [StringLength(20)]
        [Display(Name = "Check Number")]
        public string CheckNumber { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Check Issuer/Bank")]
        public string? CheckIssuer { get; set; }

        [Required(ErrorMessage = "Transaction password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Transaction Password")]
        public string TransactionPassword { get; set; } = string.Empty;
    }
}
