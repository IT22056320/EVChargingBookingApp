/*

 * File: Booking.cs
 * Description: Model for EV Charging Station Bookings
 * Author: [Your Team Name]
 * Date: [Current Date]
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    /// <summary>
    /// Booking status enumeration
    /// </summary>
    public enum BookingStatus
    {
        Pending = 0,
        Approved = 1,
        Completed = 2,
        Cancelled = 3,
        Rejected = 4
    }

    /// <summary>
    /// EV Charging Station Booking model
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Booking
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("bookingNumber")]
        [Required]
        public string BookingNumber { get; set; } = string.Empty;

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        [Required]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("chargingStationId")]
        [BsonRepresentation(BsonType.ObjectId)]
        [Required]
        public string ChargingStationId { get; set; } = string.Empty;

        [BsonElement("bookingDate")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        [Required]
        public DateTime BookingDate { get; set; }

        [BsonElement("startTime")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        [Required]
        public DateTime StartTime { get; set; }

        [BsonElement("endTime")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        [Required]
        public DateTime EndTime { get; set; }

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        [BsonElement("vehicleNumber")]
        [Required]
        public string VehicleNumber { get; set; } = string.Empty;

        [BsonElement("vehicleType")]
        public string VehicleType { get; set; } = string.Empty;

        [BsonElement("estimatedChargingTime")]
        public int EstimatedChargingTimeMinutes { get; set; }

        [BsonElement("notes")]
        public string Notes { get; set; } = string.Empty;

        [BsonElement("qrCode")]
        public string QRCode { get; set; } = string.Empty;

        [BsonElement("qrCodeGeneratedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? QRCodeGeneratedAt { get; set; }

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("modifiedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? ModifiedAt { get; set; }

        [BsonElement("approvedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? ApprovedAt { get; set; }

        [BsonElement("approvedBy")]
        public string ApprovedBy { get; set; } = string.Empty;

        [BsonElement("completedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? CompletedAt { get; set; }

        [BsonElement("cancelledAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? CancelledAt { get; set; }

        [BsonElement("cancelledBy")]
        public string CancelledBy { get; set; } = string.Empty;

        [BsonElement("cancellationReason")]
        public string CancellationReason { get; set; } = string.Empty;

        [BsonElement("rejectedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? RejectedAt { get; set; }

        [BsonElement("rejectedBy")]
        public string RejectedBy { get; set; } = string.Empty;

        [BsonElement("rejectionReason")]
        public string RejectionReason { get; set; } = string.Empty;

        [BsonElement("actualStartTime")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? ActualStartTime { get; set; }

        [BsonElement("actualEndTime")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? ActualEndTime { get; set; }

        [BsonElement("totalCost")]
        public decimal? TotalCost { get; set; }

        [BsonElement("energyConsumed")]
        public decimal? EnergyConsumedKWh { get; set; }

        /// <summary>
        /// Navigation properties (not stored in MongoDB)
        /// </summary>
        [BsonIgnore]
        public User? User { get; set; }

        [BsonIgnore]
        public ChargingStation? ChargingStation { get; set; }

        /// <summary>
        /// Business rule: Check if booking is within 7-day window
        /// </summary>
        [BsonIgnore]
        public bool IsWithinBookingWindow => 
            BookingDate >= DateTime.UtcNow.Date && 
            BookingDate <= DateTime.UtcNow.Date.AddDays(7);

        /// <summary>
        /// Business rule: Check if booking can be modified (within 12 hours of start time)
        /// </summary>
        [BsonIgnore]
        public bool CanBeModified => 
            Status == BookingStatus.Pending && 
            StartTime.Subtract(DateTime.UtcNow).TotalHours >= 12;

        /// <summary>
        /// Check if booking can be cancelled
        /// </summary>
        [BsonIgnore]
        public bool CanBeCancelled => 
            Status == BookingStatus.Pending || Status == BookingStatus.Approved;

        /// <summary>
        /// Check if booking is active (approved and within time window)
        /// </summary>
        [BsonIgnore]
        public bool IsActive => 
            Status == BookingStatus.Approved && 
            DateTime.UtcNow >= StartTime && 
            DateTime.UtcNow <= EndTime;

        /// <summary>
        /// Duration of the booking in minutes
        /// </summary>
        [BsonIgnore]
        public int DurationMinutes => 
            (int)(EndTime - StartTime).TotalMinutes;
    }
}