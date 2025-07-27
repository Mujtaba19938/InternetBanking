using System.ComponentModel.DataAnnotations;

namespace InternetBanking.Models.ViewModels
{
    public class CashDepositViewModel
    {
        [Required]
        public int AccountId { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(1, 50000, ErrorMessage = "Amount must be between $1 and $50,000")]
        [Display(Name = "Deposit Amount")]
        public decimal Amount { get; set; }

        [StringLength(200)]
        [Display(Name = "Description (Optional)")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Transaction password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Transaction Password")]
        public string TransactionPassword { get; set; } = string.Empty;
    }
}
