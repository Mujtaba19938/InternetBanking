using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InternetBanking.Models;
using InternetBanking.Models.ViewModels;
using InternetBanking.Data;
using System.Security.Cryptography;
using System.Text;

namespace InternetBanking.Controllers
{
    [Authorize(Roles = "User")]
    public class TransactionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TransactionController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> FundTransfer()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if user is an admin and redirect them
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            var model = new FundTransferViewModel();
            await LoadAccountsForView(user.Id, model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FundTransfer(FundTransferViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if user is an admin and redirect them
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Validate that the from account belongs to the user
                    var fromAccount = await _context.Accounts
                        .FirstOrDefaultAsync(a => a.AccountId == model.FromAccountId && a.UserId == user.Id);

                    if (fromAccount == null)
                    {
                        ModelState.AddModelError("", "Invalid account selected.");
                        return View(model);
                    }

                    // Check if T-Pin is set
                    if (string.IsNullOrEmpty(fromAccount.TransactionPassword))
                    {
                        ModelState.AddModelError("", "T-Pin not set. Please set your T-Pin from your profile first.");
                        return View(model);
                    }

                    // Validate transaction password
                    var hashedPassword = HashPassword(model.TransactionPassword);
                    if (fromAccount.TransactionPassword != hashedPassword)
                    {
                        ModelState.AddModelError("TransactionPassword", "Invalid T-Pin.");
                        return View(model);
                    }

                    // Check if sufficient balance
                    if (fromAccount.Balance < model.Amount)
                    {
                        ModelState.AddModelError("Amount", $"Insufficient balance. Available balance: ${fromAccount.Balance:N2}");
                        return View(model);
                    }

                    // Validate minimum transfer amount
                    if (model.Amount < 0.01m)
                    {
                        ModelState.AddModelError("Amount", "Minimum transfer amount is $0.01");
                        return View(model);
                    }

                    // Find recipient account
                    var toAccount = await _context.Accounts
                        .FirstOrDefaultAsync(a => a.AccountNumber == model.ToAccountNumber && a.IsActive);

                    if (toAccount == null)
                    {
                        ModelState.AddModelError("ToAccountNumber", "Recipient account not found or inactive.");
                        return View(model);
                    }

                    // Prevent transfer to same account
                    if (fromAccount.AccountId == toAccount.AccountId)
                    {
                        ModelState.AddModelError("ToAccountNumber", "Cannot transfer to the same account.");
                        return View(model);
                    }

                    // Use database transaction for data consistency
                    using var dbTransaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // Create transaction record
                        var transaction = new Transaction
                        {
                            FromAccountId = model.FromAccountId,
                            ToAccountId = toAccount.AccountId,
                            ToAccountNumber = model.ToAccountNumber,
                            Amount = model.Amount,
                            TransactionType = "Fund Transfer",
                            Description = model.Description,
                            TransactionDate = DateTime.Now,
                            Status = "Completed",
                            ReferenceNumber = GenerateReferenceNumber()
                        };

                        // Update account balances
                        fromAccount.Balance -= model.Amount;
                        toAccount.Balance += model.Amount;

                        // Add transaction to context
                        _context.Transactions.Add(transaction);

                        // Save all changes
                        await _context.SaveChangesAsync();

                        // Commit the transaction
                        await dbTransaction.CommitAsync();

                        TempData["SuccessMessage"] = $"Fund transfer of ${model.Amount:N2} completed successfully. Reference: {transaction.ReferenceNumber}";
                        return RedirectToAction("Dashboard", "Home");
                    }
                    catch (Exception)
                    {
                        // Rollback on error
                        await dbTransaction.RollbackAsync();
                        ModelState.AddModelError("", "An error occurred during the transfer. Please try again.");
                        return View(model);
                    }
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            await LoadAccountsForView(user.Id, model);
            return View(model);
        }

        private async Task LoadAccountsForView(string userId, FundTransferViewModel model)
        {
            var accounts = await _context.Accounts
                .Where(a => a.UserId == userId && a.IsActive)
                .ToListAsync();

            model.FromAccounts = accounts;
        }

        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private string GenerateReferenceNumber()
        {
            return $"TXN{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }
    }
}
