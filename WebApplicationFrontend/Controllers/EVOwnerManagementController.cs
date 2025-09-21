/*
 * File: EVOwnerManagementController.cs
 * Description: Controller for managing EV owners in the web application
 * Author: [Your Team Name]
 * Date: [Current Date]
 */

using Microsoft.AspNetCore.Mvc;
using WebApplicationFrontend.Services;
using WebApplicationFrontend.Filters;

namespace WebApplicationFrontend.Controllers
{
    /// <summary>
    /// Controller for EV Owner management by Backoffice users
    /// </summary>
    [Filters.Authorize("Backoffice")]
    public class EVOwnerManagementController : Controller
    {
        private readonly ApiService _apiService;

        public EVOwnerManagementController(ApiService apiService)
        {
            _apiService = apiService;
        }

        /// <summary>
        /// Display all EV owners
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var evOwners = await _apiService.GetEVOwnersAsync();
                return View(evOwners);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load EV owners: " + ex.Message;
                return View(new List<EVOwnerDto>());
            }
        }

        /// <summary>
        /// Display pending approvals
        /// </summary>
        public async Task<IActionResult> PendingApprovals()
        {
            try
            {
                var pendingOwners = await _apiService.GetPendingApprovalsAsync();
                ViewBag.Title = "Pending Approvals";
                return View("Index", pendingOwners);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load pending approvals: " + ex.Message;
                return View("Index", new List<EVOwnerDto>());
            }
        }

        /// <summary>
        /// Approve EV owner
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Approve(string nic, string returnTo = "")
        {
            try
            {
                var currentUserEmail = HttpContext.Session.GetString("UserEmail") ?? "System";
                var success = await _apiService.UpdateEVOwnerApprovalAsync(nic, true, currentUserEmail);
                
                if (success)
                {
                    TempData["Success"] = "EV Owner approved successfully";
                }
                else
                {
                    TempData["Error"] = "Failed to approve EV Owner";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error approving EV Owner: " + ex.Message;
            }

            // Redirect based on where the user came from
            if (!string.IsNullOrEmpty(returnTo) && returnTo.ToLower() == "details")
            {
                return RedirectToAction("Details", new { nic = nic });
            }
            
            return RedirectToAction("PendingApprovals");
        }

        /// <summary>
        /// Reject EV owner
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Reject(string nic, string returnTo = "")
        {
            try
            {
                var currentUserEmail = HttpContext.Session.GetString("UserEmail") ?? "System";
                var success = await _apiService.UpdateEVOwnerApprovalAsync(nic, false, currentUserEmail);
                
                if (success)
                {
                    TempData["Success"] = "EV Owner rejected";
                }
                else
                {
                    TempData["Error"] = "Failed to reject EV Owner";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error rejecting EV Owner: " + ex.Message;
            }

            // Redirect based on where the user came from
            if (!string.IsNullOrEmpty(returnTo) && returnTo.ToLower() == "details")
            {
                return RedirectToAction("Index"); // Can't stay on details page after rejection
            }
            
            return RedirectToAction("PendingApprovals");
        }

        /// <summary>
        /// Toggle EV owner account status
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string nic, bool isActive, string returnTo = "")
        {
            try
            {
                var success = await _apiService.UpdateEVOwnerStatusAsync(nic, isActive);
                
                if (success)
                {
                    TempData["Success"] = $"EV Owner account {(isActive ? "activated" : "deactivated")} successfully";
                }
                else
                {
                    TempData["Error"] = "Failed to update EV Owner status";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error updating EV Owner status: " + ex.Message;
            }

            // Redirect based on where the user came from
            if (!string.IsNullOrEmpty(returnTo) && returnTo.ToLower() == "details")
            {
                return RedirectToAction("Details", new { nic = nic });
            }
            
            return RedirectToAction("Index");
        }

        /// <summary>
        /// View EV owner details
        /// </summary>
        public async Task<IActionResult> Details(string nic)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nic))
                {
                    TempData["Error"] = "Invalid NIC provided";
                    return RedirectToAction("Index");
                }

                var evOwner = await _apiService.GetEVOwnerByNICAsync(nic);
                
                if (evOwner == null)
                {
                    TempData["Error"] = "EV Owner not found";
                    return RedirectToAction("Index");
                }

                return View(evOwner);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load EV owner details: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}