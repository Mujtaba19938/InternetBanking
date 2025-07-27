using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InternetBanking.Models;
using InternetBanking.Data;

namespace InternetBanking.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Dashboard()
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
                .Where(a => a.UserId == user.Id)
                .ToListAsync();

            List<Transaction> recentTransactions = new List<Transaction>();
            if (accounts.Any())
            {
                var accountId = accounts.First().AccountId;
                recentTransactions = await _context.Transactions
                    .Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId)
                    .OrderByDescending(t => t.TransactionDate)
                    .Take(5)
                    .ToListAsync();
            }

            ViewBag.Accounts = accounts;
            ViewBag.RecentTransactions = recentTransactions;

            return View(accounts);
        }

        [Authorize]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> AccountDetails(int accountId)
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

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountId == accountId && a.UserId == user.Id);

            if (account == null)
            {
                return NotFound();
            }

            var transactions = await _context.Transactions
                .Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            ViewBag.Account = account;
            ViewBag.Transactions = transactions;

            return View();
        }

        [Authorize]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> TransactionHistory()
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

            var userAccounts = await _context.Accounts
                .Where(a => a.UserId == user.Id)
                .Select(a => a.AccountId)
                .ToListAsync();

            List<Transaction> transactions = new List<Transaction>();
            if (userAccounts.Any())
            {
                transactions = await _context.Transactions
                    .Where(t => userAccounts.Contains(t.FromAccountId) || (t.ToAccountId.HasValue && userAccounts.Contains(t.ToAccountId.Value)))
                    .OrderByDescending(t => t.TransactionDate)
                    .ToListAsync();
            }

            return View(transactions);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
