/*
 * File: AccountController.cs
 * Description: Handles web user authentication and session management
 * Author: [Your Team Name]
 * Date: [Current Date]
 */

using Microsoft.AspNetCore.Mvc;
using WebApplicationFrontend.Services;
using System.Text.Json;

namespace WebApplicationFrontend.Controllers
{
    /// <summary>
    /// Controller for web user authentication (Email/Password)
    /// </summary>
    public class AccountController : Controller
    {
        private readonly ApiService _apiService;

        public AccountController(ApiService apiService)
        {
            _apiService = apiService;
        }

        /// <summary>
        /// Display login page for web users
        /// </summary>
        public IActionResult Login(string? returnUrl = null)
        {
            // If user is already logged in, redirect to dashboard
            if (IsUserLoggedIn())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        /// <summary>
        /// Process login attempt for web users (Email/Password)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    ViewBag.Error = "Please enter both email and password";
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.Email = email;
                    return View();
                }

                // Call API for authentication
                var result = await _apiService.LoginAsync(email, password);

                if (result.Success && result.User != null)
                {
                    // Store user information in session
                    var userJson = JsonSerializer.Serialize(result.User);
                    HttpContext.Session.SetString("CurrentUser", userJson);
                    HttpContext.Session.SetString("UserRole", result.User.Role);
                    HttpContext.Session.SetInt32("UserRoleId", result.User.RoleId);
                    HttpContext.Session.SetString("UserEmail", result.User.Email);
                    HttpContext.Session.SetString("UserFullName", result.User.FullName);

                    // Log successful login
                    Console.WriteLine($"Web User {result.User.FullName} ({result.User.Role}) logged in successfully");

                    // Redirect based on return URL or to dashboard
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Dashboard");
                }
                else
                {
                    ViewBag.Error = result.Message ?? "Invalid login credentials";
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.Email = email;
                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Login failed. Please try again.";
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.Email = email;
                Console.WriteLine($"Login error: {ex.Message}");
                return View();
            }
        }

        /// <summary>
        /// Logout user and clear session
        /// </summary>
        public IActionResult Logout()
        {
            // Log user logout
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (!string.IsNullOrEmpty(userEmail))
            {
                Console.WriteLine($"User {userEmail} logged out");
            }

            // Clear session
            HttpContext.Session.Clear();
            
            // Add success message
            TempData["Message"] = "You have been logged out successfully";
            
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Check if user is currently logged in
        /// </summary>
        private bool IsUserLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("CurrentUser"));
        }
    }
}