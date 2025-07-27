using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InternetBanking.Models;
using InternetBanking.Models.ViewModels;
using InternetBanking.Data;
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

        public AdminController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
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
                var user = await _userManager.FindByNameAsync(model.UserId);
                if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Index");
                    }
                }
                ModelState.AddModelError("", "Invalid admin login attempt.");
            }
            return View("AdminForm");
        }

        [HttpPost("register")]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
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
                    // Assign Admin role
                    await _userManager.AddToRoleAsync(user, "Admin");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    TempData["SuccessMessage"] = "Admin registration successful!";
                    return RedirectToAction("Index");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View("AdminForm");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("dashboard")]
        public IActionResult Dashboard()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("manage-admins")]
        public async Task<IActionResult> ManageAdmins()
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            return View(adminUsers);
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
        public async Task<IActionResult> RespondToServiceRequest(int id, string response)
        {
            var serviceRequest = await _context.ServiceRequests.FindAsync(id);
            if (serviceRequest != null)
            {
                serviceRequest.AdminResponse = response;
                serviceRequest.ResponseDate = DateTime.Now;
                serviceRequest.Status = "Responded";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Response sent successfully!";
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

        [Authorize(Roles = "Admin")]
        [HttpPost("change-user-role")]
        public async Task<IActionResult> ChangeUserRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("ManageAdmins");
            }

            // Get current roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            
            // Remove current roles
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            
            // Add new role
            await _userManager.AddToRoleAsync(user, newRole);
            
            TempData["SuccessMessage"] = $"User {user.UserName} role changed to {newRole}. They will be logged out on their next request.";
            return RedirectToAction("ManageAdmins");
        }
    }
} 