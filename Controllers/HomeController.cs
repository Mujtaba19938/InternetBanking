using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InternetBanking.Models;
using InternetBanking.Data;
using InternetBanking.Services;

namespace InternetBanking.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
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

            // Get user notifications
            var notifications = await _notificationService.GetUserNotificationsAsync(user.Id, includeRead: false);
            var unreadCount = await _notificationService.GetUnreadNotificationCountAsync(user.Id);

            ViewBag.Accounts = accounts;
            ViewBag.RecentTransactions = recentTransactions;
            ViewBag.Notifications = notifications;
            ViewBag.UnreadNotificationCount = unreadCount;

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
        public async Task<IActionResult> TransactionHistory(int accountId)
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

            // Verify the account belongs to the current user
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountId == accountId && a.UserId == user.Id);

            if (account == null)
            {
                return NotFound();
            }

            // Get transactions for the specific account only
            var transactions = await _context.Transactions
                .Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            ViewBag.Account = account;
            return View(transactions);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> MarkNotificationAsRead(int notificationId)
        {
            await _notificationService.MarkNotificationAsReadAsync(notificationId);
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _notificationService.MarkAllNotificationsAsReadAsync(user.Id);
            }
            return Json(new { success = true });
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
