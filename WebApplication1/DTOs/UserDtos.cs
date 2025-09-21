/*
 * File: UserDtos.cs
 * Description: Data Transfer Objects for User operations
 * Author: EV Charging Team
 * Date: September 21, 2025
 */

using WebApplication1.Models;

namespace WebApplication1.DTOs
{
    /// <summary>
    /// Data Transfer Object for creating new users
    /// </summary>
    public class CreateUserDto
    {
        public string NIC { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.EVOwner;
    }

    /// <summary>
    /// Data Transfer Object for updating user information
    /// </summary>
    public class UpdateUserDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Request model for EV owner registration from mobile app
    /// </summary>
    public class RegisterEVOwnerRequest
    {
        public string NIC { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response model for registration
    /// </summary>
    public class RegisterResponse
    {
        public string Message { get; set; } = string.Empty;
        public object? User { get; set; }
    }

    /// <summary>
    /// Request model for updating EV owner profile
    /// </summary>
    public class UpdateEVOwnerRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    /// <summary>
    /// Login request model
    /// </summary>
    public class LoginRequest
    {
        public string NIC { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
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