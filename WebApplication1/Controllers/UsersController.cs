/*
 * File: UsersController.cs
 * Description: Legacy controller - kept for backward compatibility during transition
 * Author: [Your Team Name]
 * Date: [Current Date]
 * 
 * NOTE: This controller is deprecated. Use WebUsersController and EVOwnersController instead.
 */

using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using WebApplication1.Models;
using WebApplication1.Services;
using BCrypt.Net;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Obsolete("This controller is deprecated. Use WebUsersController and EVOwnersController instead.")]
    public class UsersController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;

        /// <summary>
        /// Initialize Users controller with MongoDB service
        /// </summary>
        public UsersController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        /// <summary>
        /// Get all users (legacy - returns EV owners only)
        /// </summary>
        [HttpGet]
        [Obsolete("Use EVOwnersController.GetEVOwners() instead")]
        public async Task<ActionResult<List<User>>> GetUsers()
        {
            try
            {
                var users = await _mongoDBService.EVOwners.Find(_ => true).ToListAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve users", error = ex.Message });
            }
        }

        /// <summary>
        /// Get user by NIC (legacy)
        /// </summary>
        [HttpGet("{nic}")]
        [Obsolete("Use EVOwnersController.GetEVOwnerByNIC() instead")]
        public async Task<ActionResult<User>> GetUser(string nic)
        {
            try
            {
                var user = await _mongoDBService.EVOwners.Find(u => u.NIC == nic).FirstOrDefaultAsync();

                if (user == null)
                    return NotFound($"User with NIC {nic} not found");

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve user", error = ex.Message });
            }
        }

        /// <summary>
        /// Create new user (legacy - redirects to registration)
        /// </summary>
        [HttpPost]
        [Obsolete("Use EVOwnersController.RegisterEVOwner() instead")]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            return BadRequest(new { 
                message = "This endpoint is deprecated. EV Owners should register via mobile app using /api/EVOwners/register",
                redirectTo = "/api/EVOwners/register"
            });
        }

        /// <summary>
        /// Update user information (legacy)
        /// </summary>
        [HttpPut("{nic}")]
        [Obsolete("Use EVOwnersController endpoints instead")]
        public async Task<ActionResult<User>> UpdateUser(string nic, User updatedUser)
        {
            return BadRequest(new { 
                message = "This endpoint is deprecated. Use EVOwnersController endpoints instead",
                alternatives = new[] {
                    "/api/EVOwners/{nic}/approval",
                    "/api/EVOwners/{nic}/status"
                }
            });
        }

        /// <summary>
        /// Activate/Deactivate user account (legacy)
        /// </summary>
        [HttpPatch("{nic}/status")]
        [Obsolete("Use EVOwnersController.UpdateAccountStatus() instead")]
        public async Task<ActionResult> UpdateUserStatus(string nic, [FromBody] bool isActive)
        {
            return BadRequest(new { 
                message = "This endpoint is deprecated. Use /api/EVOwners/{nic}/status instead",
                redirectTo = $"/api/EVOwners/{nic}/status"
            });
        }

        /// <summary>
        /// Delete user (legacy)
        /// </summary>
        [HttpDelete("{nic}")]
        [Obsolete("User deletion not supported in new architecture")]
        public async Task<ActionResult> DeleteUser(string nic)
        {
            return BadRequest(new { 
                message = "User deletion is not supported. Use account deactivation instead.",
                alternative = $"/api/EVOwners/{nic}/status"
            });
        }
    }

    /// <summary>
    /// Legacy login request model
    /// </summary>
    [Obsolete("Use WebUserLoginRequest instead")]
    public class LoginRequest
    {
        public string NIC { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
