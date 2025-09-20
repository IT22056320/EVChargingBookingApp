/*
 * File: DashboardController.cs
 * Description: Main dashboard controller for role-based landing pages
 * Author: [Your Team Name]
 * Date: [Current Date]
 */

using Microsoft.AspNetCore.Mvc;
using WebApplicationFrontend.Services;
using WebApplicationFrontend.Filters;
using System.Text.Json;

namespace WebApplicationFrontend.Controllers
{
    /// <summary>
    /// Dashboard controller with role-based views
    /// </summary>
    [Filters.Authorize]
    public class DashboardController : Controller
    {
        private readonly ApiService _apiService;

        public DashboardController(ApiService apiService)
        {
            _apiService = apiService;
        }

        /// <summary>
        /// Main dashboard - redirects based on user role
        /// </summary>
        public IActionResult Index()
        {
            // Check if user is logged in
            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get role and redirect to appropriate dashboard
            var userRole = HttpContext.Session.GetInt32("UserRoleId") ?? -1;

            switch (userRole)
            {
                case 0: // Backoffice
                    return RedirectToAction("BackofficeDashboard");
                case 1: // Station Operator
                    return RedirectToAction("OperatorDashboard");
                default:
                    return RedirectToAction("Login", "Account");
            }
        }

        /// <summary>
        /// Back office dashboard with admin functions
        /// </summary>
        [Filters.Authorize("Backoffice")]
        public async Task<IActionResult> BackofficeDashboard()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null || currentUser.RoleId != 0)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get statistics for dashboard
            try
            {
                var webUsers = await _apiService.GetWebUsersAsync();
                var evOwners = await _apiService.GetEVOwnersAsync();
                var pendingApprovals = await _apiService.GetPendingApprovalsAsync();

                ViewBag.TotalWebUsers = webUsers.Count;
                ViewBag.BackofficeUsers = webUsers.Count(u => u.RoleId == 0);
                ViewBag.StationOperators = webUsers.Count(u => u.RoleId == 1);
                ViewBag.ActiveWebUsers = webUsers.Count(u => u.IsActive);

                ViewBag.TotalEVOwners = evOwners.Count;
                ViewBag.ApprovedEVOwners = evOwners.Count(u => u.IsApproved);
                ViewBag.PendingApprovals = pendingApprovals.Count;
                ViewBag.ActiveEVOwners = evOwners.Count(u => u.IsActive);
            }
            catch
            {
                ViewBag.TotalWebUsers = 0;
                ViewBag.BackofficeUsers = 0;
                ViewBag.StationOperators = 0;
                ViewBag.ActiveWebUsers = 0;
                ViewBag.TotalEVOwners = 0;
                ViewBag.ApprovedEVOwners = 0;
                ViewBag.PendingApprovals = 0;
                ViewBag.ActiveEVOwners = 0;
            }

            return View(currentUser);
        }

        /// <summary>
        /// Station operator dashboard
        /// </summary>
        [Filters.Authorize("StationOperator")]
        public IActionResult OperatorDashboard()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null || currentUser.RoleId != 1)
            {
                return RedirectToAction("Login", "Account");
            }

            // TODO: Add station operator specific data
            ViewBag.StationsManaged = 5; // Placeholder
            ViewBag.ActiveBookings = 12; // Placeholder
            ViewBag.TodaysBookings = 8; // Placeholder

            return View(currentUser);
        }

        /// <summary>
        /// Get current logged-in user from session
        /// </summary>
        private WebUserLoginResponse? GetCurrentUser()
        {
            var userJson = HttpContext.Session.GetString("CurrentUser");
            if (string.IsNullOrEmpty(userJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<WebUserLoginResponse>(userJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }
    }
}