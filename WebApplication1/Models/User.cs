/*
 * File: User.cs
 * Description: Model for EV Owners (mobile app users only)
 * Author: [Your Team Name]
 * Date: [Current Date]
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApplication1.Models
{
    /// <summary>
    /// EV Owner model - these users register via mobile app only
    /// </summary>
    [BsonIgnoreExtraElements]
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("nic")]
        public string NIC { get; set; } = string.Empty;

        [BsonElement("fullName")]
        public string FullName { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("password")]
        public string Password { get; set; } = string.Empty;

        [BsonElement("phoneNumber")]
        public string PhoneNumber { get; set; } = string.Empty;

        [BsonElement("address")]
        public string Address { get; set; } = string.Empty;

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("isApproved")]
        public bool IsApproved { get; set; } = false; // Requires approval by backoffice

        [BsonElement("registeredAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        [BsonElement("approvedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? ApprovedAt { get; set; }

        [BsonElement("approvedBy")]
        public string ApprovedBy { get; set; } = string.Empty;

        [BsonElement("lastLoginAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastLoginAt { get; set; }
    }
}