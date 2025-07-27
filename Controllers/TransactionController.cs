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

            var accounts = await _context.Accounts
                .Where(a => a.UserId == user.Id && a.IsActive)
                .ToListAsync();

            var model = new FundTransferViewModel
            {
                FromAccounts = accounts
            };

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
                // Validate that the from account belongs to the user
                var fromAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.AccountId == model.FromAccountId && a.UserId == user.Id);

                if (fromAccount == null)
                {
                    ModelState.AddModelError("", "Invalid account selected.");
                    return View(model);
                }

                // Validate transaction password
                var hashedPassword = HashPassword(model.TransactionPassword);
                if (fromAccount.TransactionPassword != hashedPassword)
                {
                    ModelState.AddModelError("", "Invalid transaction password.");
                    return View(model);
                }

                // Check if sufficient balance
                if (fromAccount.Balance < model.Amount)
                {
                    ModelState.AddModelError("", "Insufficient balance.");
                    return View(model);
                }

                // Create transaction
                var transaction = new Transaction
                {
                    FromAccountId = model.FromAccountId,
                    ToAccountNumber = model.ToAccountNumber,
                    Amount = model.Amount,
                    TransactionType = "Fund Transfer",
                    Description = model.Description,
                    TransactionDate = DateTime.Now,
                    Status = "Completed",
                    ReferenceNumber = GenerateReferenceNumber()
                };

                // Update account balance
                fromAccount.Balance -= model.Amount;

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Fund transfer of ${model.Amount} completed successfully. Reference: {transaction.ReferenceNumber}";
                return RedirectToAction("Dashboard", "Home");
            }

            // If we got this far, something failed, redisplay form
            var accounts = await _context.Accounts
                .Where(a => a.UserId == user.Id && a.IsActive)
                .ToListAsync();

            model.FromAccounts = accounts;
            return View(model);
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
