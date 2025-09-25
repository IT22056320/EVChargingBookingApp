/*
 * File: BookingDtos.cs
 * Description: Data Transfer Objects for Booking operations
 * Author: EV Charging Team
 * Date: September 24, 2025
 */

using WebApplication1.Models;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTOs
{
    /// <summary>
    /// Data Transfer Object for creating new bookings
    /// </summary>
    public class CreateBookingDto
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string ChargingStationId { get; set; } = string.Empty;

        [Required]
        public DateTime BookingDate { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 2)]
        public string VehicleNumber { get; set; } = string.Empty;

        public string VehicleType { get; set; } = string.Empty;

        [Range(1, 1440)] // Max 24 hours
        public int EstimatedChargingTimeMinutes { get; set; }

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data Transfer Object for updating bookings
    /// </summary>
    public class UpdateBookingDto
    {
        public DateTime? BookingDate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        [StringLength(20, MinimumLength = 2)]
        public string? VehicleNumber { get; set; }

        public string? VehicleType { get; set; }

        [Range(1, 1440)] // Max 24 hours
        public int? EstimatedChargingTimeMinutes { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for booking status updates
    /// </summary>
    public class UpdateBookingStatusDto
    {
        [Required]
        public BookingStatus Status { get; set; }

        public string? UpdatedBy { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }
        public decimal? TotalCost { get; set; }
        public decimal? EnergyConsumedKWh { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for booking approval
    /// </summary>
    public class ApproveBookingDto
    {
        [Required]
        public bool IsApproved { get; set; }

        [Required]
        public string ApprovedBy { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for booking cancellation
    /// </summary>
    public class CancelBookingDto
    {
        [Required]
        public string CancelledBy { get; set; } = string.Empty;

        [Required]
        [StringLength(500, MinimumLength = 5)]
        public string CancellationReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Comprehensive booking response DTO
    /// </summary>
    public class BookingResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string BookingNumber { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string ChargingStationId { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public BookingStatus Status { get; set; }
        public string VehicleNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public int EstimatedChargingTimeMinutes { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string QRCode { get; set; } = string.Empty;
        public DateTime? QRCodeGeneratedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string ApprovedBy { get; set; } = string.Empty;
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string CancelledBy { get; set; } = string.Empty;
        public string CancellationReason { get; set; } = string.Empty;
        public DateTime? RejectedAt { get; set; }
        public string RejectedBy { get; set; } = string.Empty;
        public string RejectionReason { get; set; } = string.Empty;
        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }
        public decimal? TotalCost { get; set; }
        public decimal? EnergyConsumedKWh { get; set; }
        
        // Navigation properties
        public UserResponseDto? User { get; set; }
        public ChargingStationResponseDto? ChargingStation { get; set; }
        
        // Business rule properties
        public bool IsWithinBookingWindow { get; set; }
        public bool CanBeModified { get; set; }
        public bool CanBeCancelled { get; set; }
        public bool IsActive { get; set; }
        public int DurationMinutes { get; set; }
    }

    /// <summary>
    /// Simplified user response for booking details
    /// </summary>
    public class UserResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string NIC { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsApproved { get; set; }
    }

    /// <summary>
    /// Simplified charging station response for booking details
    /// </summary>
    public class ChargingStationResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string StationName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ConnectorType { get; set; } = string.Empty;
        public decimal PowerRatingKW { get; set; }
        public decimal PricePerKWh { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Amenities { get; set; } = new List<string>();
        public string OperatingHours { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public int MaxBookingDurationMinutes { get; set; }
        public CoordinatesDto? Coordinates { get; set; }
    }

    /// <summary>
    /// Coordinates DTO
    /// </summary>
    public class CoordinatesDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    /// <summary>
    /// Booking search and filter parameters
    /// </summary>
    public class BookingSearchDto
    {
        public string? UserId { get; set; }
        public string? ChargingStationId { get; set; }
        public BookingStatus? Status { get; set; }
        public DateTime? BookingDateFrom { get; set; }
        public DateTime? BookingDateTo { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public string? VehicleNumber { get; set; }
        public string? UserNIC { get; set; }
        public string? UserName { get; set; }
        public string? StationName { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = true;
    }

    /// <summary>
    /// Paginated booking response
    /// </summary>
    public class BookingPagedResponseDto
    {
        public List<BookingResponseDto> Bookings { get; set; } = new List<BookingResponseDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    /// <summary>
    /// Booking statistics DTO
    /// </summary>
    public class BookingStatsDto
    {
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ApprovedBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int RejectedBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageBookingDuration { get; set; }
        public List<DailyBookingStatsDto> DailyStats { get; set; } = new List<DailyBookingStatsDto>();
    }

    /// <summary>
    /// Daily booking statistics
    /// </summary>
    public class DailyBookingStatsDto
    {
        public DateTime Date { get; set; }
        public int BookingCount { get; set; }
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// Time slot availability check DTO
    /// </summary>
    public class TimeSlotCheckDto
    {
        [Required]
        public string ChargingStationId { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public string? ExcludeBookingId { get; set; }
    }

    /// <summary>
    /// Time slot availability response
    /// </summary>
    public class TimeSlotAvailabilityDto
    {
        public bool IsAvailable { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ConflictingBookingDto> ConflictingBookings { get; set; } = new List<ConflictingBookingDto>();
    }

    /// <summary>
    /// Conflicting booking information
    /// </summary>
    public class ConflictingBookingDto
    {
        public string BookingId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public BookingStatus Status { get; set; }
        public string UserName { get; set; } = string.Empty;
    }
}