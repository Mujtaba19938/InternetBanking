using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InternetBanking.Models;
using InternetBanking.Data;

namespace InternetBanking.Controllers
{
    [Authorize(Roles = "User")]
    public class ServiceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ServiceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRequest(string requestType, string description)
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

            if (string.IsNullOrEmpty(requestType) || string.IsNullOrEmpty(description))
            {
                TempData["ErrorMessage"] = "Please provide both request type and description.";
                return RedirectToAction("Index");
            }

            var serviceRequest = new ServiceRequest
            {
                UserId = user.Id,
                RequestType = requestType,
                Description = description,
                RequestDate = DateTime.Now,
                Status = "Pending",
                AdminResponse = ""
            };

            _context.ServiceRequests.Add(serviceRequest);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Service request submitted successfully. We will respond within 24 hours.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> MyRequests()
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

            var requests = await _context.ServiceRequests
                .Where(sr => sr.UserId == user.Id)
                .OrderByDescending(sr => sr.RequestDate)
                .ToListAsync();

            return View(requests);
        }
    }
}
