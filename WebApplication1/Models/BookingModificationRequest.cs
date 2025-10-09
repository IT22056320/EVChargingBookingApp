/*
 * File: BookingModificationRequest.cs
 * Description: Model for booking modification requests from customers
 * Author: EV Charging Team
 * Date: October 7, 2025
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    /// <summary>
    /// Modification request status enumeration
    /// </summary>
    public enum ModificationRequestStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Cancelled = 3
    }

    /// <summary>
    /// Booking modification request model - tracks customer-initiated booking changes
    /// </summary>
    [BsonIgnoreExtraElements]
    public class BookingModificationRequest
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("bookingId")]
        [BsonRepresentation(BsonType.ObjectId)]
        [Required]
        public string BookingId { get; set; } = string.Empty;

        [BsonElement("requestedBy")]
        [Required]
        public string RequestedBy { get; set; } = string.Empty; // User ID

        [BsonElement("requestedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public ModificationRequestStatus Status { get; set; } = ModificationRequestStatus.Pending;

        // Original values (for reference)
        [BsonElement("originalChargingStationId")]
        public string? OriginalChargingStationId { get; set; }

        [BsonElement("originalStartTime")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? OriginalStartTime { get; set; }

        [BsonElement("originalEndTime")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? OriginalEndTime { get; set; }

        [BsonElement("originalVehicleNumber")]
        public string? OriginalVehicleNumber { get; set; }

        // Requested new values
        [BsonElement("requestedChargingStationId")]
        public string? RequestedChargingStationId { get; set; }

        [BsonElement("requestedBookingDate")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? RequestedBookingDate { get; set; }

        [BsonElement("requestedStartTime")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? RequestedStartTime { get; set; }

        [BsonElement("requestedEndTime")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? RequestedEndTime { get; set; }

        [BsonElement("requestedVehicleNumber")]
        public string? RequestedVehicleNumber { get; set; }

        [BsonElement("requestedVehicleType")]
        public string? RequestedVehicleType { get; set; }

        [BsonElement("requestedNotes")]
        public string? RequestedNotes { get; set; }

        [BsonElement("requestReason")]
        [Required]
        public string RequestReason { get; set; } = string.Empty;

        // Approval/Rejection details
        [BsonElement("reviewedBy")]
        public string ReviewedBy { get; set; } = string.Empty; // Admin ID

        [BsonElement("reviewedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? ReviewedAt { get; set; }

        [BsonElement("reviewNotes")]
        public string ReviewNotes { get; set; } = string.Empty;

        [BsonElement("rejectionReason")]
        public string RejectionReason { get; set; } = string.Empty;

        /// <summary>
        /// Check if modification request can still be processed
        /// </summary>
        [BsonIgnore]
        public bool CanBeProcessed => Status == ModificationRequestStatus.Pending;

        /// <summary>
        /// Get a summary of changes
        /// </summary>
        [BsonIgnore]
        public string ChangesSummary
        {
            get
            {
                var changes = new List<string>();
                
                if (!string.IsNullOrEmpty(RequestedChargingStationId) && 
                    RequestedChargingStationId != OriginalChargingStationId)
                {
                    changes.Add("Station changed");
                }
                
                if (RequestedStartTime.HasValue && RequestedStartTime != OriginalStartTime)
                {
                    changes.Add("Start time changed");
                }
                
                if (RequestedEndTime.HasValue && RequestedEndTime != OriginalEndTime)
                {
                    changes.Add("End time changed");
                }
                
                if (!string.IsNullOrEmpty(RequestedVehicleNumber) && 
                    RequestedVehicleNumber != OriginalVehicleNumber)
                {
                    changes.Add("Vehicle changed");
                }

                return changes.Any() ? string.Join(", ", changes) : "No changes";
            }
        }
    }
}
