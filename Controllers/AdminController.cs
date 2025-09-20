using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InternetBanking.Models;
using InternetBanking.Models.ViewModels;
using InternetBanking.Data;
using InternetBanking.Services;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace InternetBanking.Controllers
{
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            INotificationService notificationService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string message = null)
        {
            if (message == "session_expired")
            {
                TempData["ErrorMessage"] = "Session expired. Your role has changed. Please log in again.";
                return View("AdminForm");
            }

            // If user is authenticated and has admin role, show dashboard
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return View("Dashboard");
                }
            }
            
            // Get admin credentials to check if default credentials are still in use
            var adminCredentials = await _context.AdminCredentials.FirstOrDefaultAsync();
            ViewBag.AdminCredentials = adminCredentials;
            
            // Otherwise show the login/register form
            return View("AdminForm");
        }

        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check admin credentials from AdminCredentials table
                var adminCredentials = await _context.AdminCredentials.FirstOrDefaultAsync();
                if (adminCredentials != null)
                {
                    // Verify username
                    if (adminCredentials.Username == model.UserId)
                    {
                        // Verify password using the same hasher
                        var passwordVerifier = new PasswordHasher<ApplicationUser>();
                        var result = passwordVerifier.VerifyHashedPassword(null, adminCredentials.PasswordHash, model.Password);
                        
                        if (result == PasswordVerificationResult.Success)
                        {
                            // Find the admin user in Identity system and sign them in
                            var adminUser = await _userManager.FindByNameAsync(model.UserId);
                            if (adminUser != null)
                            {
                                var signInResult = await _signInManager.PasswordSignInAsync(adminUser, model.Password, model.RememberMe, false);
                                if (signInResult.Succeeded)
                                {
                                    return RedirectToAction("Index");
                                }
                            }
                            
                            // If Identity sign-in fails, try to sign in with the verified password
                            var directSignIn = await _signInManager.PasswordSignInAsync(adminUser, model.Password, model.RememberMe, false);
                            if (directSignIn.Succeeded)
                            {
                                return RedirectToAction("Index");
                            }
                            
                            ModelState.AddModelError("", "Authentication failed. Please try again.");
                        }
                        else
                        {
                            ModelState.AddModelError("", "Invalid password.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Invalid username.");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Admin credentials not found. Please contact system administrator.");
                }
            }
            
            // Get admin credentials for the view
            var credentials = await _context.AdminCredentials.FirstOrDefaultAsync();
            ViewBag.AdminCredentials = credentials;
            
            return View("AdminForm");
        }


        [Authorize(Roles = "Admin")]
        [HttpGet("dashboard")]
        public IActionResult Dashboard()
        {
            return View();
        }


        [Authorize(Roles = "Admin")]
        [HttpGet("service-requests")]
        public async Task<IActionResult> ServiceRequests()
        {
            var serviceRequests = await _context.ServiceRequests
                .Include(sr => sr.User)
                .OrderByDescending(sr => sr.RequestDate)
                .ToListAsync();
            return View(serviceRequests);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("service-requests/{id}/respond")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondToServiceRequest(int id, string response, string action)
        {
            var serviceRequest = await _context.ServiceRequests
                .Include(sr => sr.User)
                .FirstOrDefaultAsync(sr => sr.RequestId == id);
                
            if (serviceRequest != null)
            {
                serviceRequest.AdminResponse = response;
                serviceRequest.ResponseDate = DateTime.Now;
                serviceRequest.Status = "Responded";

                // Handle card request specific logic
                if (serviceRequest.RequestType == "Debit Card Request")
                {
                    if (action == "approve")
                    {
                        serviceRequest.CardStatus = "approved";
                        serviceRequest.EtaDate = DateTime.Now.AddBusinessDays(7); // 7 business days
                        
                        // Create approval notification
                        await _notificationService.CreateCardRequestApprovalNotificationAsync(
                            serviceRequest.UserId, 
                            serviceRequest.RequestId
                        );
                    }
                    else if (action == "reject")
                    {
                        serviceRequest.CardStatus = "rejected";
                        
                        // Create rejection notification
                        await _notificationService.CreateCardRequestRejectionNotificationAsync(
                            serviceRequest.UserId, 
                            serviceRequest.RequestId
                        );
                    }
                }
                else
                {
                    // Generic service request notification
                    var title = $"Response to {serviceRequest.RequestType}";
                    var message = $"Your {serviceRequest.RequestType} request has been responded to: {response}";
                    
                    await _notificationService.CreateNotificationAsync(
                        serviceRequest.UserId,
                        title,
                        message,
                        "ServiceRequest",
                        serviceRequest.RequestId,
                        "ServiceRequest"
                    );
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Response sent successfully!";
            }
            return RedirectToAction("ServiceRequests");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("service-requests/{id}/mark-ready")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkCardAsReady(int id)
        {
            var serviceRequest = await _context.ServiceRequests
                .FirstOrDefaultAsync(sr => sr.RequestId == id && sr.RequestType == "Debit Card Request");
                
            if (serviceRequest != null && serviceRequest.CardStatus == "approved")
            {
                serviceRequest.CardStatus = "ready";
                await _context.SaveChangesAsync();

                // Create card ready notification
                await _notificationService.CreateCardReadyNotificationAsync(
                    serviceRequest.UserId, 
                    serviceRequest.RequestId
                );

                TempData["SuccessMessage"] = "Card marked as ready for pickup!";
            }
            else
            {
                TempData["ErrorMessage"] = "Card request not found or not approved.";
            }
            
            return RedirectToAction("ServiceRequests");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("custom-queries")]
        public IActionResult CustomQueries()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("custom-queries")]
        public async Task<IActionResult> ExecuteCustomQuery(string query)
        {
            try
            {
                // For security, only allow SELECT queries
                if (!query.Trim().ToUpper().StartsWith("SELECT"))
                {
                    TempData["ErrorMessage"] = "Only SELECT queries are allowed for security reasons.";
                    return RedirectToAction("CustomQueries");
                }

                var result = await _context.Database.SqlQueryRaw<object>(query).ToListAsync();
                ViewBag.QueryResult = result;
                ViewBag.ExecutedQuery = query;
                return View("CustomQueries");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Query execution failed: {ex.Message}";
                return RedirectToAction("CustomQueries");
            }
        }

        [HttpGet("change-credentials")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeCredentials()
        {
            var currentCredentials = await _context.AdminCredentials.FirstOrDefaultAsync();
            if (currentCredentials == null)
            {
                TempData["ErrorMessage"] = "Admin credentials not found. Please contact system administrator.";
                return RedirectToAction("Index");
            }

            var model = new ChangeAdminCredentialsViewModel
            {
                CurrentUsername = currentCredentials.Username
            };

            return View(model);
        }

        [HttpPost("change-credentials")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeCredentials(ChangeAdminCredentialsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Get current admin credentials
                var currentCredentials = await _context.AdminCredentials.FirstOrDefaultAsync();
                if (currentCredentials == null)
                {
                    TempData["ErrorMessage"] = "Admin credentials not found. Please contact system administrator.";
                    return RedirectToAction("Index");
                }

                // Verify current credentials
                if (currentCredentials.Username != model.CurrentUsername)
                {
                    ModelState.AddModelError("CurrentUsername", "Current username is incorrect.");
                    return View(model);
                }

                // Verify current password by checking against the stored hash
                var passwordVerifier = new PasswordHasher<ApplicationUser>();
                var result = passwordVerifier.VerifyHashedPassword(null, currentCredentials.PasswordHash, model.CurrentPassword);
                if (result == PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                    return View(model);
                }

                // Check if new username already exists in the system
                var existingUser = await _userManager.FindByNameAsync(model.NewUsername);
                if (existingUser != null && existingUser.UserName != currentCredentials.Username)
                {
                    ModelState.AddModelError("NewUsername", "This username is already taken.");
                    return View(model);
                }

                // Hash the new password
                var hasher = new PasswordHasher<ApplicationUser>();
                var newPasswordHash = hasher.HashPassword(null, model.NewPassword);

                // Update admin credentials
                currentCredentials.Username = model.NewUsername;
                currentCredentials.PasswordHash = newPasswordHash;
                currentCredentials.UpdatedAt = DateTime.UtcNow;
                currentCredentials.UpdatedBy = User.Identity?.Name;
                currentCredentials.IsDefault = false;

                // Update the admin user in Identity system
                var adminUser = await _userManager.FindByNameAsync(model.CurrentUsername);
                if (adminUser != null)
                {
                    adminUser.UserName = model.NewUsername;
                    adminUser.NormalizedUserName = model.NewUsername.ToUpper();
                    
                    // Update password
                    var token = await _userManager.GeneratePasswordResetTokenAsync(adminUser);
                    await _userManager.ResetPasswordAsync(adminUser, token, model.NewPassword);
                    
                    await _userManager.UpdateAsync(adminUser);
                }

                await _context.SaveChangesAsync();

                // Log the credential change
                Console.WriteLine($"[AUDIT] Admin credentials changed by {User.Identity?.Name} at {DateTime.UtcNow}");

                TempData["SuccessMessage"] = "Your new credentials are saved successfully. The old credentials are no longer valid.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to change admin credentials: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while changing credentials. Please try again.";
                return View(model);
            }
        }

    }

    // Extension method for adding business days
    public static class DateTimeExtensions
    {
        public static DateTime AddBusinessDays(this DateTime date, int businessDays)
        {
            var result = date;
            var addedDays = 0;
            
            while (addedDays < businessDays)
            {
                result = result.AddDays(1);
                if (result.DayOfWeek != DayOfWeek.Saturday && result.DayOfWeek != DayOfWeek.Sunday)
                {
                    addedDays++;
                }
            }
            
            return result;
        }
    }
} 