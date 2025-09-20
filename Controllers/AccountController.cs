using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using InternetBanking.Models;
using InternetBanking.Models.ViewModels;
using InternetBanking.Data;

namespace InternetBanking.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string message = null)
        {
            if (message == "session_expired")
            {
                TempData["ErrorMessage"] = "Session expired. Your role has changed. Please log in again.";
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.UserId);

                if (user != null)
                {
                    // Check if account is locked
                    if (user.IsAccountLocked && user.AccountLockedUntil > DateTime.Now)
                    {
                        ModelState.AddModelError("", "Your account is locked due to multiple failed login attempts. Please try again later.");
                        return View(model);
                    }

                    // Reset lock if time has passed
                    if (user.IsAccountLocked && user.AccountLockedUntil <= DateTime.Now)
                    {
                        user.IsAccountLocked = false;
                        user.FailedLoginAttempts = 0;
                        user.AccountLockedUntil = null;
                        await _userManager.UpdateAsync(user);
                    }

                    var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);

                    if (result.Succeeded)
                    {
                        // Reset failed attempts on successful login
                        user.FailedLoginAttempts = 0;
                        user.LastFailedLogin = null;
                        await _userManager.UpdateAsync(user);

                        // Check user role and redirect accordingly
                        if (await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            return RedirectToAction("Index", "Admin");
                        }
                        else
                        {
                            return RedirectToAction("Dashboard", "Home");
                        }
                    }
                    else
                    {
                        // Increment failed attempts
                        user.FailedLoginAttempts++;
                        user.LastFailedLogin = DateTime.Now;

                        if (user.FailedLoginAttempts >= 3)
                        {
                            user.IsAccountLocked = true;
                            user.AccountLockedUntil = DateTime.Now.AddMinutes(30); // Lock for 30 minutes
                            ModelState.AddModelError("", "Your account has been locked due to multiple failed login attempts.");
                        }
                        else
                        {
                            int remainingAttempts = 3 - user.FailedLoginAttempts;
                            ModelState.AddModelError("", $"Invalid login attempt. {remainingAttempts} attempts remaining before account lock.");
                        }

                        await _userManager.UpdateAsync(user);
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Invalid login attempt.");
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.UserId,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Address = model.Address,
                    DateOfBirth = model.DateOfBirth,
                    PhoneNumber = model.PhoneNumber
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Assign User role to regular users
                    await _userManager.AddToRoleAsync(user, "User");
                    
                    // Create default accounts for the new user
                    await CreateDefaultAccountsAsync(user.Id);

                    // Sign in the user
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    TempData["SuccessMessage"] = "Registration successful! Your accounts have been created.";
                    return RedirectToAction("Dashboard", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        private async Task CreateDefaultAccountsAsync(string userId)
        {
            // Generate unique account numbers
            var savingsAccountNumber = GenerateAccountNumber("SAV");
            var checkingAccountNumber = GenerateAccountNumber("CHK");

            // Create accounts without transaction password - user must set it
            var accounts = new List<Account>
            {
                new Account
                {
                    UserId = userId,
                    AccountNumber = savingsAccountNumber,
                    AccountType = "Savings",
                    Balance = 0.00m, // Starting balance
                    TransactionPassword = "", // Empty - user must set T-Pin
                    IsActive = true
                },
                new Account
                {
                    UserId = userId,
                    AccountNumber = checkingAccountNumber,
                    AccountType = "Checking",
                    Balance = 0.00m, // Starting balance
                    TransactionPassword = "", // Empty - user must set T-Pin
                    IsActive = true
                }
            };

            _context.Accounts.AddRange(accounts);
            await _context.SaveChangesAsync();
        }

        private string GenerateAccountNumber(string prefix)
        {
            var random = new Random();
            var accountNumber = $"{prefix}{DateTime.Now:yyyyMMdd}{random.Next(1000, 9999)}";
            return accountNumber;
        }

        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
