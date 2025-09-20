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
}