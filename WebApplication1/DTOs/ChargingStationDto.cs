/*
 * File: ChargingStationDto.cs
 * Description: Data Transfer Object for Charging Station operations
 * Author: EV Charging Team
 * Date: September 30, 2025
 */

using System;
using System.Collections.Generic;

namespace WebApplication1.DTOs
{
    public class ChargingStationDto
    {
        // Properties for ChargingStationDto
        public string? Id { get; set; }
        public string StationName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string ConnectorType { get; set; } = string.Empty;
        public decimal PowerRatingKW { get; set; }
        public decimal PricePerKWh { get; set; }
        public string Status { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Amenities { get; set; } = new List<string>();
        public string OperatingHours { get; set; } = "24/7";
        public bool IsAvailable { get; set; } = true;
        public int TotalSlots { get; set; } 
        public int MaxBookingDurationMinutes { get; set; } = 240;
        public int AvailableSlots { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }

        // Constructor for ChargingStationDto
        public ChargingStationDto()
        {
            // Ensure AvailableSlots equals TotalSlots by default
            AvailableSlots = TotalSlots;
        }
    }
}