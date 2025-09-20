/*
 * File: WebUser.cs
 * Description: Model for web application users (Backoffice and Station Operators)
 * Author: [Your Team Name]
 * Date: [Current Date]
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApplication1.Models
{
    /// <summary>
    /// Web application user model for staff members
    /// </summary>
    [BsonIgnoreExtraElements]
    public class WebUser
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("password")]
        public string Password { get; set; } = string.Empty;

        [BsonElement("fullName")]
        public string FullName { get; set; } = string.Empty;

        [BsonElement("role")]
        [BsonRepresentation(BsonType.String)]
        public WebUserRole Role { get; set; } = WebUserRole.StationOperator;

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("lastLoginAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastLoginAt { get; set; }

        [BsonElement("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Web user roles for staff members only
    /// </summary>
    public enum WebUserRole
    {
        /// <summary>
        /// Back-office users with system administration access
        /// </summary>
        Backoffice,
        
        /// <summary>
        /// Station operators who manage charging stations
        /// </summary>
        StationOperator
    }
}