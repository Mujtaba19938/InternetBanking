using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InternetBanking.Models;
using InternetBanking.Data;
using System.Security.Cryptography;
using System.Text;

namespace InternetBanking.Controllers
{
    [Authorize(Roles = "User")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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
                .Where(a => a.UserId == user.Id)
                .ToListAsync();

            ViewBag.Accounts = accounts;
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> ChangeTransactionPassword()
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

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeTransactionPassword(string currentPassword, string newPassword, string confirmPassword)
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

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "New T-Pin and confirmation do not match.");
                return View();
            }

            if (string.IsNullOrWhiteSpace(user.TransactionPassword))
            {
                ModelState.AddModelError("", "No T-Pin set. Please use Reset T-Pin instead.");
                return View();
            }

            // Verify current password
            var hashedCurrentPassword = HashPassword(currentPassword);
            if (user.TransactionPassword != hashedCurrentPassword)
            {
                ModelState.AddModelError("", "Current T-Pin is incorrect.");
                return View();
            }

            // Update password
            user.TransactionPassword = HashPassword(newPassword);
            await _userManager.UpdateAsync(user);

            // Also update all user's accounts for backward compatibility
            var userAccounts = await _context.Accounts
                .Where(a => a.UserId == user.Id && a.IsActive)
                .ToListAsync();
            
            foreach (var account in userAccounts)
            {
                account.TransactionPassword = HashPassword(newPassword);
            }
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "T-Pin updated successfully for all your accounts.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ResetTransactionPassword()
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

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetTransactionPassword(string newPassword, string confirmPassword)
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

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "New T-Pin and confirmation do not match.");
                return View();
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
            {
                ModelState.AddModelError("", "T-Pin must be at least 4 characters long.");
                return View();
            }

            // Update user's T-Pin
            user.TransactionPassword = HashPassword(newPassword);
            await _userManager.UpdateAsync(user);

            // Also update all user's accounts for backward compatibility
            var userAccounts = await _context.Accounts
                .Where(a => a.UserId == user.Id && a.IsActive)
                .ToListAsync();
            
            foreach (var account in userAccounts)
            {
                account.TransactionPassword = HashPassword(newPassword);
            }
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "T-Pin reset successfully for all your accounts.";
            return RedirectToAction("Index");
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

        [HttpGet]
        public async Task<IActionResult> UpdateProfile()
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

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string firstName, string lastName, string email, string phoneNumber, string address, DateTime dateOfBirth)
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

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    TempData["ErrorMessage"] = "First name and last name are required.";
                    return RedirectToAction("UpdateProfile");
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    TempData["ErrorMessage"] = "Email address is required.";
                    return RedirectToAction("UpdateProfile");
                }

                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    TempData["ErrorMessage"] = "Phone number is required.";
                    return RedirectToAction("UpdateProfile");
                }

                // Check if email is already taken by another user
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    TempData["ErrorMessage"] = "Email address is already in use by another account.";
                    return RedirectToAction("UpdateProfile");
                }

                // Update user profile
                user.FirstName = firstName.Trim();
                user.LastName = lastName.Trim();
                user.Email = email.Trim();
                user.PhoneNumber = phoneNumber.Trim();
                user.Address = address?.Trim() ?? "";
                user.DateOfBirth = dateOfBirth;

                // Update user in Identity system
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update profile. Please try again.";
                    return RedirectToAction("UpdateProfile");
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while updating your profile. Please try again.";
                return RedirectToAction("UpdateProfile");
            }
        }
    }
}
