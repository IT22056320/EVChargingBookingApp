/*
 * File: BookingModels.cs
 * Description: Models for booking management in the frontend application
 * Author: EV Charging Team
 * Date: September 24, 2025
 */

namespace WebApplicationFrontend.Models
{
    /// <summary>
    /// Booking response model for frontend
    /// </summary>
    public class BookingResponse
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string ChargingStationId { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Status { get; set; }
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
        public UserResponse? User { get; set; }
        public ChargingStationResponse? ChargingStation { get; set; }
        public bool IsWithinBookingWindow { get; set; }
        public bool CanBeModified { get; set; }
        public bool CanBeCancelled { get; set; }
        public bool IsActive { get; set; }
        public int DurationMinutes { get; set; }
    }

    /// <summary>
    /// User response model for booking details
    /// </summary>
    public class UserResponse
    {
        public string Id { get; set; } = string.Empty;
        public string NIC { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }

    /// <summary>
    /// Charging station response model for booking details
    /// </summary>
    public class ChargingStationResponse
    {
        public string Id { get; set; } = string.Empty;
        public string StationName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int ConnectorType { get; set; }
        public decimal PowerRatingKW { get; set; }
        public decimal PricePerKWh { get; set; }
        public int Status { get; set; }
    }

    /// <summary>
    /// Paginated booking response
    /// </summary>
    public class BookingPagedResponse
    {
        public List<BookingResponse> Bookings { get; set; } = new List<BookingResponse>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    /// <summary>
    /// Booking statistics response
    /// </summary>
    public class BookingStatsResponse
    {
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ApprovedBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int RejectedBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageBookingDuration { get; set; }
        public List<DailyBookingStatsResponse> DailyStats { get; set; } = new List<DailyBookingStatsResponse>();
    }

    /// <summary>
    /// Daily booking statistics
    /// </summary>
    public class DailyBookingStatsResponse
    {
        public DateTime Date { get; set; }
        public int BookingCount { get; set; }
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// View model for booking management dashboard
    /// </summary>
    public class BookingManagementViewModel
    {
        public List<BookingResponse> Bookings { get; set; } = new List<BookingResponse>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public string CurrentStatus { get; set; } = "all";
        public string SearchTerm { get; set; } = "";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// View model for booking statistics
    /// </summary>
    public class BookingStatisticsViewModel
    {
        public BookingStatsResponse Statistics { get; set; } = new BookingStatsResponse();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}