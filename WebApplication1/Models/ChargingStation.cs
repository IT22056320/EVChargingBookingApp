/*
 * File: ChargingStation.cs
 * Description: Model for EV Charging Stations
 * Author: EV Charging Team
 * Date: September 30, 2025
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    /// <summary>
    /// Charging station status enumeration
    /// </summary>
    public enum ChargingStationStatus
    {
        Active = 0,
        Inactive = 1,
        Maintenance = 2,
        OutOfService = 3
    }

    /// <summary>
    /// Charging station connector type
    /// </summary>
    public enum ConnectorType
    {
        Unknown = 0,
        Type1 = 1,
        Type2 = 2,
        CHAdeMO = 3,
        CCS = 4,
        Tesla = 5
    }

    /// <summary>
    /// EV Charging Station model
    /// </summary>
    [BsonIgnoreExtraElements]
    public class ChargingStation
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("stationName")]
        [Required]
        public string StationName { get; set; } = string.Empty;

        [BsonElement("location")]
        [Required]
        public string Location { get; set; } = string.Empty;

        [BsonElement("address")]
        [Required]
        public string Address { get; set; } = string.Empty;

        [BsonElement("latitude")]
        public double? Latitude { get; set; }

        [BsonElement("longitude")]
        public double? Longitude { get; set; }

        [BsonElement("connectorType")]
        [BsonRepresentation(BsonType.String)]
        public ConnectorType ConnectorType { get; set; }

        [BsonElement("powerRating")]
        public decimal PowerRatingKW { get; set; }

        [BsonElement("pricePerKWh")]
        public decimal PricePerKWh { get; set; }

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public ChargingStationStatus Status { get; set; } = ChargingStationStatus.Active;

        [BsonElement("operatorId")]
        public string OperatorId { get; set; } = string.Empty;

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("amenities")]
        public List<string> Amenities { get; set; } = new List<string>();

        [BsonElement("operatingHours")]
        public string OperatingHours { get; set; } = "24/7";

        [BsonElement("isAvailable")]
        public bool IsAvailable { get; set; } = true;

        [BsonElement("maxBookingDurationMinutes")]
        public int MaxBookingDurationMinutes { get; set; } = 240; // 4 hours default

        [BsonElement("availableSlots")]
        [Required]
        public int AvailableSlots { get; set; } = 0;

        [BsonElement("totalSlots")]
        public int TotalSlots { get; set; } = 0;

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? UpdatedAt { get; set; }

        [BsonElement("lastMaintenanceDate")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastMaintenanceDate { get; set; }

        [BsonElement("nextMaintenanceDate")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? NextMaintenanceDate { get; set; }

        /// <summary>
        /// Navigation properties (not stored in MongoDB)
        /// </summary>
        [BsonIgnore]
        public List<Booking>? Bookings { get; set; }

        /// <summary>
        /// Checks if the station is currently available for booking.
        /// </summary>
        [BsonIgnore]
        public bool IsBookingAvailable => 
            Status == ChargingStationStatus.Active && IsAvailable;

        /// <summary>
        /// Checks if the station requires maintenance.
        /// </summary>
        [BsonIgnore]
        public bool RequiresMaintenance => 
            NextMaintenanceDate.HasValue && NextMaintenanceDate.Value <= DateTime.UtcNow;
    }
}