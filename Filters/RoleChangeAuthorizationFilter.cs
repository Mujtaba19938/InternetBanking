using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using InternetBanking.Models;
using InternetBanking.Data;
using Microsoft.EntityFrameworkCore;

namespace InternetBanking.Filters
{
    public class RoleChangeAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public RoleChangeAuthorizationFilter(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Skip for unauthenticated users
            if (!context.HttpContext.User.Identity.IsAuthenticated)
            {
                return;
            }

            var user = await _userManager.GetUserAsync(context.HttpContext.User);
            if (user == null)
            {
                await _signInManager.SignOutAsync();
                context.Result = new RedirectToActionResult("Login", "Account", new { message = "session_expired" });
                return;
            }

            // Check if user is an admin
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            var isUser = await _userManager.IsInRoleAsync(user, "User");

            // Get the current route
            var routeData = context.RouteData;
            var controller = routeData.Values["controller"]?.ToString()?.ToLower();
            var action = routeData.Values["action"]?.ToString()?.ToLower();

            // Check for role mismatch and redirect accordingly
            if (isAdmin)
            {
                // Admin trying to access user controllers
                if (IsUserController(controller))
                {
                    await _signInManager.SignOutAsync();
                    context.Result = new RedirectToActionResult("Index", "Admin", new { message = "session_expired" });
                    return;
                }
            }
            else if (isUser)
            {
                // User trying to access admin controllers
                if (IsAdminController(controller))
                {
                    await _signInManager.SignOutAsync();
                    context.Result = new RedirectToActionResult("Login", "Account", new { message = "session_expired" });
                    return;
                }
            }
            else
            {
                // User has no roles - log them out
                await _signInManager.SignOutAsync();
                context.Result = new RedirectToActionResult("Login", "Account", new { message = "session_expired" });
                return;
            }
        }

        private bool IsUserController(string controller)
        {
            var userControllers = new[] { "home", "transaction", "deposit", "service", "profile", "statement" };
            return userControllers.Contains(controller);
        }

        private bool IsAdminController(string controller)
        {
            var adminControllers = new[] { "admin" };
            return adminControllers.Contains(controller);
        }
    }
} 