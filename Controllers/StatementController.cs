using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InternetBanking.Models;
using InternetBanking.Data;
using InternetBanking.Services;

namespace InternetBanking.Controllers
{
    [Authorize]
    public class StatementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStatementService _statementService;

        public StatementController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IStatementService statementService)
        {
            _context = context;
            _userManager = userManager;
            _statementService = statementService;
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

        [HttpGet("export-pdf")]
        public async Task<IActionResult> ExportPdf(int accountId, int month = 0, int year = 0)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Default to current month if not specified
            if (month == 0) month = DateTime.Now.Month;
            if (year == 0) year = DateTime.Now.Year;

            var pdfBytes = await _statementService.GeneratePdfStatementAsync(accountId, month, year);
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                return NotFound();
            }

            var fileName = $"MonthlyStatement_{accountId}_{year}_{month:D2}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportExcel(int accountId, int month = 0, int year = 0)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Default to current month if not specified
            if (month == 0) month = DateTime.Now.Month;
            if (year == 0) year = DateTime.Now.Year;

            var excelBytes = await _statementService.GenerateExcelStatementAsync(accountId, month, year);
            if (excelBytes == null || excelBytes.Length == 0)
            {
                return NotFound();
            }

            var fileName = $"MonthlyStatement_{accountId}_{year}_{month:D2}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("export-annual-pdf")]
        public async Task<IActionResult> ExportAnnualPdf(int accountId, int year = 0)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Default to current year if not specified
            if (year == 0) year = DateTime.Now.Year;

            var pdfBytes = await _statementService.GenerateAnnualPdfStatementAsync(accountId, year);
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                return NotFound();
            }

            var fileName = $"AnnualStatement_{accountId}_{year}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [HttpGet("export-annual-excel")]
        public async Task<IActionResult> ExportAnnualExcel(int accountId, int year = 0)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Default to current year if not specified
            if (year == 0) year = DateTime.Now.Year;

            var excelBytes = await _statementService.GenerateAnnualExcelStatementAsync(accountId, year);
            if (excelBytes == null || excelBytes.Length == 0)
            {
                return NotFound();
            }

            var fileName = $"AnnualStatement_{accountId}_{year}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
