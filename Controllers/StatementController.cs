using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InternetBanking.Models;
using InternetBanking.Data;

namespace InternetBanking.Controllers
{
    [Authorize]
    public class StatementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StatementController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Monthly(int accountId, int month = 0, int year = 0)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountId == accountId && a.UserId == user.Id);

            if (account == null)
            {
                return NotFound();
            }

            // Default to current month if not specified
            if (month == 0) month = DateTime.Now.Month;
            if (year == 0) year = DateTime.Now.Year;

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var transactions = await _context.Transactions
                .Where(t => (t.FromAccountId == accountId || t.ToAccountId == accountId) &&
                           t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                .OrderBy(t => t.TransactionDate)
                .Include(t => t.FromAccount)
                .Include(t => t.ToAccount)
                .ToListAsync();

            ViewBag.Account = account;
            ViewBag.Month = month;
            ViewBag.Year = year;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            return View(transactions);
        }

        public async Task<IActionResult> Annual(int accountId, int year = 0)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountId == accountId && a.UserId == user.Id);

            if (account == null)
            {
                return NotFound();
            }

            // Default to current year if not specified
            if (year == 0) year = DateTime.Now.Year;

            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year, 12, 31);

            var transactions = await _context.Transactions
                .Where(t => (t.FromAccountId == accountId || t.ToAccountId == accountId) &&
                           t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                .OrderBy(t => t.TransactionDate)
                .Include(t => t.FromAccount)
                .Include(t => t.ToAccount)
                .ToListAsync();

            ViewBag.Account = account;
            ViewBag.Year = year;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            return View(transactions);
        }
    }
}
