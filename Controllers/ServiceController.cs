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

            // Get user's pending requests count
            var pendingRequestsCount = await _context.ServiceRequests
                .CountAsync(sr => sr.UserId == user.Id && sr.Status == "Pending");

            ViewBag.PendingRequestsCount = pendingRequestsCount;
            ViewBag.CanSubmitNewRequest = pendingRequestsCount < 5;

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

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(requestType))
                {
                    TempData["ErrorMessage"] = "Please select a service type.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrWhiteSpace(description))
                {
                    TempData["ErrorMessage"] = "Please provide a description for your request.";
                    return RedirectToAction("Index");
                }

                if (description.Length < 10)
                {
                    TempData["ErrorMessage"] = "Description must be at least 10 characters long.";
                    return RedirectToAction("Index");
                }

                // Check if user has too many pending requests (limit to 5)
                var pendingRequestsCount = await _context.ServiceRequests
                    .CountAsync(sr => sr.UserId == user.Id && sr.Status == "Pending");

                if (pendingRequestsCount >= 5)
                {
                    TempData["ErrorMessage"] = "You have too many pending requests. Please wait for responses before submitting new ones.";
                    return RedirectToAction("Index");
                }

                // Create service request
                var serviceRequest = new ServiceRequest
                {
                    UserId = user.Id,
                    RequestType = requestType.Trim(),
                    Description = description.Trim(),
                    RequestDate = DateTime.Now,
                    Status = "Pending",
                    AdminResponse = ""
                };

                _context.ServiceRequests.Add(serviceRequest);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Your {requestType} request has been submitted successfully. Request ID: #{serviceRequest.RequestId:D6}. We will respond within 24 hours.";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while submitting your request. Please try again.";
                return RedirectToAction("Index");
            }
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
