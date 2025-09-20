/*
 * File: EVOwnersController.cs
 * Description: Controller for EV Owner management (mobile app registrations)
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
    public class EVOwnersController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;

        /// <summary>
        /// Initialize EVOwners controller with MongoDB service
        /// </summary>
        public EVOwnersController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        /// <summary>
        /// Get all EV owners (for web application management)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<User>>> GetEVOwners()
        {
            try
            {
                var evOwners = await _mongoDBService.EVOwners.Find(_ => true).ToListAsync();
                
                // Remove passwords from response
                var sanitizedOwners = evOwners.Select(u => new
                {
                    u.Id,
                    u.NIC,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    u.Address,
                    u.IsActive,
                    u.IsApproved,
                    u.RegisteredAt,
                    u.ApprovedAt,
                    u.ApprovedBy,
                    u.LastLoginAt
                }).ToList();

                return Ok(sanitizedOwners);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve EV owners", error = ex.Message });
            }
        }

        /// <summary>
        /// Get EV owner by NIC
        /// </summary>
        [HttpGet("{nic}")]
        public async Task<ActionResult> GetEVOwnerByNIC(string nic)
        {
            try
            {
                var evOwner = await _mongoDBService.EVOwners
                    .Find(u => u.NIC == nic)
                    .FirstOrDefaultAsync();

                if (evOwner == null)
                    return NotFound($"EV Owner with NIC {nic} not found");

                var response = new
                {
                    evOwner.Id,
                    evOwner.NIC,
                    evOwner.FullName,
                    evOwner.Email,
                    evOwner.PhoneNumber,
                    evOwner.Address,
                    evOwner.IsActive,
                    evOwner.IsApproved,
                    evOwner.RegisteredAt,
                    evOwner.ApprovedAt,
                    evOwner.ApprovedBy,
                    evOwner.LastLoginAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve EV owner", error = ex.Message });
            }
        }

        /// <summary>
        /// Register new EV owner (from mobile app)
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult> RegisterEVOwner([FromBody] RegisterEVOwnerRequest request)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.NIC))
                    return BadRequest("NIC is required");

                if (string.IsNullOrWhiteSpace(request.FullName))
                    return BadRequest("Full name is required");

                if (string.IsNullOrWhiteSpace(request.Email))
                    return BadRequest("Email is required");

                if (string.IsNullOrWhiteSpace(request.Password))
                    return BadRequest("Password is required");

                // Check if EV owner with same NIC already exists
                var existingOwner = await _mongoDBService.EVOwners
                    .Find(u => u.NIC == request.NIC)
                    .FirstOrDefaultAsync();

                if (existingOwner != null)
                    return BadRequest($"EV Owner with NIC {request.NIC} already exists");

                // Create new EV owner
                var newOwner = new User
                {
                    NIC = request.NIC,
                    FullName = request.FullName,
                    Email = request.Email.ToLower(),
                    Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    PhoneNumber = request.PhoneNumber ?? string.Empty,
                    Address = request.Address ?? string.Empty,
                    IsActive = true,
                    IsApproved = false, // Requires approval by backoffice
                    RegisteredAt = DateTime.UtcNow
                };

                await _mongoDBService.EVOwners.InsertOneAsync(newOwner);

                return Ok(new { 
                    message = "EV Owner registered successfully. Awaiting approval.",
                    owner = new {
                        newOwner.Id,
                        newOwner.NIC,
                        newOwner.FullName,
                        newOwner.Email,
                        newOwner.IsApproved,
                        newOwner.RegisteredAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Registration failed", error = ex.Message });
            }
        }

        /// <summary>
        /// Approve/Reject EV owner (Backoffice only)
        /// </summary>
        [HttpPatch("{nic}/approval")]
        public async Task<ActionResult> UpdateApprovalStatus(string nic, [FromBody] ApprovalRequest request)
        {
            try
            {
                var filter = Builders<User>.Filter.Eq(u => u.NIC, nic);
                var updateBuilder = Builders<User>.Update
                    .Set(u => u.IsApproved, request.IsApproved)
                    .Set(u => u.ApprovedBy, request.ApprovedBy ?? "System");

                if (request.IsApproved)
                {
                    updateBuilder = updateBuilder.Set(u => u.ApprovedAt, DateTime.UtcNow);
                }

                var result = await _mongoDBService.EVOwners.UpdateOneAsync(filter, updateBuilder);

                if (result.MatchedCount == 0)
                    return NotFound($"EV Owner with NIC {nic} not found");

                return Ok(new { 
                    message = $"EV Owner {(request.IsApproved ? "approved" : "rejected")} successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update approval status", error = ex.Message });
            }
        }

        /// <summary>
        /// Activate/Deactivate EV owner account (Backoffice only)
        /// </summary>
        [HttpPatch("{nic}/status")]
        public async Task<ActionResult> UpdateAccountStatus(string nic, [FromBody] StatusUpdateRequest request)
        {
            try
            {
                var filter = Builders<User>.Filter.Eq(u => u.NIC, nic);
                var update = Builders<User>.Update.Set(u => u.IsActive, request.IsActive);

                var result = await _mongoDBService.EVOwners.UpdateOneAsync(filter, update);

                if (result.MatchedCount == 0)
                    return NotFound($"EV Owner with NIC {nic} not found");

                return Ok(new { 
                    message = $"EV Owner account {(request.IsActive ? "activated" : "deactivated")} successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update account status", error = ex.Message });
            }
        }

        /// <summary>
        /// Get pending approvals (Backoffice dashboard)
        /// </summary>
        [HttpGet("pending-approvals")]
        public async Task<ActionResult> GetPendingApprovals()
        {
            try
            {
                var pendingOwners = await _mongoDBService.EVOwners
                    .Find(u => !u.IsApproved && u.IsActive)
                    .ToListAsync();

                var response = pendingOwners.Select(u => new
                {
                    u.Id,
                    u.NIC,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    u.RegisteredAt
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve pending approvals", error = ex.Message });
            }
        }
    }

    /// <summary>
    /// EV Owner registration request model
    /// </summary>
    public class RegisterEVOwnerRequest
    {
        public string NIC { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
    }

    /// <summary>
    /// Approval request model
    /// </summary>
    public class ApprovalRequest
    {
        public bool IsApproved { get; set; }
        public string? ApprovedBy { get; set; }
    }

    /// <summary>
    /// Status update request model
    /// </summary>
    public class StatusUpdateRequest
    {
        public bool IsActive { get; set; }
    }
}