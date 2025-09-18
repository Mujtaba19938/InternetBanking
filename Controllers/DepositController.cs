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
    public class DepositController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DepositController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
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

            ViewBag.Accounts = accounts;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CashDeposit(CashDepositViewModel model)
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
                var account = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.AccountId == model.AccountId && a.UserId == user.Id);

                if (account == null)
                {
                    ModelState.AddModelError("", "Invalid account selected.");
                    return RedirectToAction("Index");
                }

                // Create deposit transaction
                var transaction = new Transaction
                {
                    FromAccountId = account.AccountId,
                    ToAccountNumber = account.AccountNumber,
                    Amount = model.Amount,
                    TransactionType = "Cash Deposit",
                    Description = $"Cash deposit of ${model.Amount}",
                    TransactionDate = DateTime.Now,
                    Status = "Completed",
                    ReferenceNumber = GenerateReferenceNumber()
                };

                // Update account balance
                account.Balance += model.Amount;

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Cash deposit of ${model.Amount} completed successfully. Reference: {transaction.ReferenceNumber}";
                return RedirectToAction("Dashboard", "Home");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckDeposit(CheckDepositViewModel model)
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
                var account = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.AccountId == model.AccountId && a.UserId == user.Id);

                if (account == null)
                {
                    ModelState.AddModelError("", "Invalid account selected.");
                    return RedirectToAction("Index");
                }

                // Create check deposit transaction (pending until cleared)
                var transaction = new Transaction
                {
                    FromAccountId = account.AccountId,
                    ToAccountNumber = account.AccountNumber,
                    Amount = model.Amount,
                    TransactionType = "Check Deposit",
                    Description = $"Check deposit of ${model.Amount} - Check #: {model.CheckNumber}",
                    TransactionDate = DateTime.Now,
                    Status = "Pending",
                    ReferenceNumber = GenerateReferenceNumber()
                };

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Check deposit of ${model.Amount} submitted successfully. Reference: {transaction.ReferenceNumber}. Status: Pending clearance.";
                return RedirectToAction("Dashboard", "Home");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WireTransfer(WireTransferViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                var account = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.AccountId == model.AccountId && a.UserId == user.Id);

                if (account == null)
                {
                    ModelState.AddModelError("", "Invalid account selected.");
                    await LoadAccountsForView(user.Id);
                    return View("Index", model);
                }

                // Check if T-Pin is set
                if (string.IsNullOrEmpty(account.TransactionPassword))
                {
                    ModelState.AddModelError("", "T-Pin not set. Please set your T-Pin from your profile first.");
                    await LoadAccountsForView(user.Id);
                    return View("Index", model);
                }

                // Verify transaction password
                var hashedPassword = HashPassword(model.TransactionPassword);
                if (account.TransactionPassword != hashedPassword)
                {
                    ModelState.AddModelError("TransactionPassword", "Invalid T-Pin.");
                    await LoadAccountsForView(user.Id);
                    return View("Index", model);
                }

                // Process wire transfer (incoming)
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Add funds to account
                    account.Balance += model.Amount;

                    // Create transaction record
                    var wireTransaction = new Transaction
                    {
                        FromAccountId = account.AccountId,
                        ToAccountId = account.AccountId,
                        ToAccountNumber = account.AccountNumber,
                        Amount = model.Amount,
                        TransactionType = "Wire Transfer (Incoming)",
                        Description = $"Wire from {model.SenderName} - {model.SenderBank}",
                        Status = "Completed",
                        ReferenceNumber = GenerateReferenceNumber()
                    };

                    _context.Transactions.Add(wireTransaction);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Wire transfer of ${model.Amount:F2} received successfully. Reference: {wireTransaction.ReferenceNumber}";
                    return RedirectToAction("Dashboard", "Home");
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Wire transfer processing failed. Please try again.");
                }
            }

            await LoadAccountsForView(user.Id);
            return View("Index", model);
        }

        private async Task LoadAccountsForView(string userId)
        {
            var accounts = await _context.Accounts
                .Where(a => a.UserId == userId && a.IsActive)
                .ToListAsync();
            ViewBag.Accounts = accounts;
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private string GenerateReferenceNumber()
        {
            return $"DEP{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }
    }
}
