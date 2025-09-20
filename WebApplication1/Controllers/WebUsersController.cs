/*
 * File: WebUsersController.cs
 * Description: Controller for web application user management (Backoffice and Station Operators)
 * Author: [Your Team Name]
 * Date: [Current Date]
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
    public class WebUsersController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;

        /// <summary>
        /// Initialize WebUsers controller with MongoDB service
        /// </summary>
        public WebUsersController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        /// <summary>
        /// Authenticate web user login
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] WebUserLoginRequest request)
        {
            try
            {
                var user = await _mongoDBService.WebUsers
                    .Find(u => u.Email.ToLower() == request.Email.ToLower() && u.IsActive)
                    .FirstOrDefaultAsync();

                if (user == null)
                    return NotFound(new { message = "User not found or inactive" });

                // Verify password using BCrypt
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                    return Unauthorized(new { message = "Invalid password" });

                // Update last login time
                var updateFilter = Builders<WebUser>.Filter.Eq(u => u.Id, user.Id);
                var updateDef = Builders<WebUser>.Update.Set(u => u.LastLoginAt, DateTime.UtcNow);
                await _mongoDBService.WebUsers.UpdateOneAsync(updateFilter, updateDef);

                // Return user info (without password)
                var userResponse = new
                {
                    user.Id,
                    user.Email,
                    user.FullName,
                    Role = user.Role.ToString(),
                    RoleId = (int)user.Role,
                    user.IsActive,
                    user.CreatedAt,
                    LastLoginAt = DateTime.UtcNow
                };

                return Ok(new { 
                    success = true, 
                    message = "Login successful", 
                    user = userResponse 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all web users (Backoffice only)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<WebUser>>> GetWebUsers()
        {
            try
            {
                var users = await _mongoDBService.WebUsers.Find(_ => true).ToListAsync();
                
                // Remove passwords from response
                var sanitizedUsers = users.Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FullName,
                    Role = u.Role.ToString(),
                    RoleId = (int)u.Role,
                    u.IsActive,
                    u.CreatedAt,
                    u.LastLoginAt,
                    u.CreatedBy
                }).ToList();

                return Ok(sanitizedUsers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve users", error = ex.Message });
            }
        }

        /// <summary>
        /// Create new web user (Backoffice only)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> CreateWebUser([FromBody] CreateWebUserRequest request)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.Email))
                    return BadRequest("Email is required");

                if (string.IsNullOrWhiteSpace(request.FullName))
                    return BadRequest("Full name is required");

                if (string.IsNullOrWhiteSpace(request.Password))
                    return BadRequest("Password is required");

                // Check if user with same email already exists
                var existingUser = await _mongoDBService.WebUsers
                    .Find(u => u.Email.ToLower() == request.Email.ToLower())
                    .FirstOrDefaultAsync();

                if (existingUser != null)
                    return BadRequest($"User with email {request.Email} already exists");

                // Create new web user
                var newUser = new WebUser
                {
                    Email = request.Email.ToLower(),
                    FullName = request.FullName,
                    Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = request.Role,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = request.CreatedBy ?? "System"
                };

                await _mongoDBService.WebUsers.InsertOneAsync(newUser);

                return Ok(new { 
                    message = "Web user created successfully",
                    user = new {
                        newUser.Id,
                        newUser.Email,
                        newUser.FullName,
                        Role = newUser.Role.ToString(),
                        newUser.IsActive,
                        newUser.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to create user", error = ex.Message });
            }
        }

        /// <summary>
        /// Update web user status
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<ActionResult> UpdateUserStatus(string id, [FromBody] bool isActive)
        {
            try
            {
                var filter = Builders<WebUser>.Filter.Eq(u => u.Id, id);
                var update = Builders<WebUser>.Update.Set(u => u.IsActive, isActive);

                var result = await _mongoDBService.WebUsers.UpdateOneAsync(filter, update);

                if (result.MatchedCount == 0)
                    return NotFound($"User with ID {id} not found");

                return Ok(new { message = $"User {(isActive ? "activated" : "deactivated")} successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update user status", error = ex.Message });
            }
        }
    }

    /// <summary>
    /// Login request model for web users
    /// </summary>
    public class WebUserLoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Create web user request model
    /// </summary>
    public class CreateWebUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public WebUserRole Role { get; set; }
        public string? CreatedBy { get; set; }
    }
}