using Microsoft.AspNetCore.SignalR;
using WebApplication1.DTOs;
using WebApplication1.Hubs;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    /// <summary>
    /// Service for sending real-time notifications via SignalR
    /// </summary>
    public interface INotificationService
    {
        Task SendBookingStatusUpdateAsync(string bookingId, BookingStatus oldStatus, BookingStatus newStatus, string? message = null);
        Task SendBookingCreatedAsync(BookingResponseDto booking);
        Task SendBookingUpdatedAsync(BookingResponseDto booking);
        Task SendBookingDeletedAsync(string bookingId, string userId);
        Task SendStationBookingNotificationAsync(string stationId, BookingResponseDto booking, string action);
        Task SendQRCodeGeneratedAsync(string bookingId, string userId);
        Task SendBulkStatusUpdateAsync(List<string> bookingIds, BookingStatus newStatus);
    }

    public class NotificationService : INotificationService
    {
        private readonly IHubContext<BookingNotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IHubContext<BookingNotificationHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Send booking status update notification
        /// </summary>
        public async Task SendBookingStatusUpdateAsync(string bookingId, BookingStatus oldStatus, BookingStatus newStatus, string? message = null)
        {
            try
            {
                var notification = new
                {
                    BookingId = bookingId,
                    OldStatus = oldStatus.ToString(),
                    NewStatus = newStatus.ToString(),
                    Message = message ?? $"Booking status changed from {oldStatus} to {newStatus}",
                    Timestamp = DateTime.UtcNow,
                    Type = "StatusUpdate"
                };

                // Send to specific booking group
                await _hubContext.Clients.Group($"Booking_{bookingId}")
                    .SendAsync("BookingStatusChanged", notification);

                // Send to relevant role groups
                await _hubContext.Clients.Groups("StationOperator", "Backoffice")
                    .SendAsync("BookingStatusChanged", notification);

                _logger.LogInformation($"Sent booking status update notification for booking {bookingId}: {oldStatus} -> {newStatus}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send booking status update notification for booking {bookingId}");
            }
        }

        /// <summary>
        /// Send new booking created notification
        /// </summary>
        public async Task SendBookingCreatedAsync(BookingResponseDto booking)
        {
            try
            {
                var notification = new
                {
                    Booking = booking,
                    Message = $"New booking created for {booking.ChargingStation?.StationName ?? "charging station"}",
                    Timestamp = DateTime.UtcNow,
                    Type = "BookingCreated"
                };

                // Send to station operators and backoffice
                await _hubContext.Clients.Groups("StationOperator", "Backoffice")
                    .SendAsync("BookingCreated", notification);

                // Send to specific station group
                if (!string.IsNullOrEmpty(booking.ChargingStationId))
                {
                    await _hubContext.Clients.Group($"Station_{booking.ChargingStationId}")
                        .SendAsync("BookingCreated", notification);
                }

                _logger.LogInformation($"Sent booking created notification for booking {booking.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send booking created notification for booking {booking.Id}");
            }
        }

        /// <summary>
        /// Send booking updated notification
        /// </summary>
        public async Task SendBookingUpdatedAsync(BookingResponseDto booking)
        {
            try
            {
                var notification = new
                {
                    Booking = booking,
                    Message = $"Booking updated for {booking.ChargingStation?.StationName ?? "charging station"}",
                    Timestamp = DateTime.UtcNow,
                    Type = "BookingUpdated"
                };

                // Send to specific booking group
                await _hubContext.Clients.Group($"Booking_{booking.Id}")
                    .SendAsync("BookingUpdated", notification);

                // Send to relevant role groups
                await _hubContext.Clients.Groups("StationOperator", "Backoffice")
                    .SendAsync("BookingUpdated", notification);

                // Send to specific station group
                if (!string.IsNullOrEmpty(booking.ChargingStationId))
                {
                    await _hubContext.Clients.Group($"Station_{booking.ChargingStationId}")
                        .SendAsync("BookingUpdated", notification);
                }

                _logger.LogInformation($"Sent booking updated notification for booking {booking.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send booking updated notification for booking {booking.Id}");
            }
        }

        /// <summary>
        /// Send booking deleted notification
        /// </summary>
        public async Task SendBookingDeletedAsync(string bookingId, string userId)
        {
            try
            {
                var notification = new
                {
                    BookingId = bookingId,
                    UserId = userId,
                    Message = $"Booking {bookingId} has been cancelled/deleted",
                    Timestamp = DateTime.UtcNow,
                    Type = "BookingDeleted"
                };

                // Send to specific booking group
                await _hubContext.Clients.Group($"Booking_{bookingId}")
                    .SendAsync("BookingDeleted", notification);

                // Send to relevant role groups
                await _hubContext.Clients.Groups("StationOperator", "Backoffice")
                    .SendAsync("BookingDeleted", notification);

                _logger.LogInformation($"Sent booking deleted notification for booking {bookingId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send booking deleted notification for booking {bookingId}");
            }
        }

        /// <summary>
        /// Send station-specific booking notification
        /// </summary>
        public async Task SendStationBookingNotificationAsync(string stationId, BookingResponseDto booking, string action)
        {
            try
            {
                var notification = new
                {
                    StationId = stationId,
                    Booking = booking,
                    Action = action,
                    Message = $"Booking {action} for station {stationId}",
                    Timestamp = DateTime.UtcNow,
                    Type = "StationBookingNotification"
                };

                // Send to specific station group
                await _hubContext.Clients.Group($"Station_{stationId}")
                    .SendAsync("StationBookingNotification", notification);

                _logger.LogInformation($"Sent station booking notification for station {stationId}, booking {booking.Id}, action {action}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send station booking notification for station {stationId}, booking {booking.Id}");
            }
        }

        /// <summary>
        /// Send QR code generated notification
        /// </summary>
        public async Task SendQRCodeGeneratedAsync(string bookingId, string userId)
        {
            try
            {
                var notification = new
                {
                    BookingId = bookingId,
                    UserId = userId,
                    Message = "QR code has been generated for your booking",
                    Timestamp = DateTime.UtcNow,
                    Type = "QRCodeGenerated"
                };

                // Send to specific booking group
                await _hubContext.Clients.Group($"Booking_{bookingId}")
                    .SendAsync("QRCodeGenerated", notification);

                // Send to user-specific connection if available
                await _hubContext.Clients.User(userId)
                    .SendAsync("QRCodeGenerated", notification);

                _logger.LogInformation($"Sent QR code generated notification for booking {bookingId}, user {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send QR code generated notification for booking {bookingId}");
            }
        }

        /// <summary>
        /// Send bulk status update notification
        /// </summary>
        public async Task SendBulkStatusUpdateAsync(List<string> bookingIds, BookingStatus newStatus)
        {
            try
            {
                var notification = new
                {
                    BookingIds = bookingIds,
                    NewStatus = newStatus.ToString(),
                    Count = bookingIds.Count,
                    Message = $"{bookingIds.Count} bookings have been updated to {newStatus}",
                    Timestamp = DateTime.UtcNow,
                    Type = "BulkStatusUpdate"
                };

                // Send to relevant role groups
                await _hubContext.Clients.Groups("StationOperator", "Backoffice")
                    .SendAsync("BulkStatusUpdate", notification);

                // Send to specific booking groups
                foreach (var bookingId in bookingIds)
                {
                    await _hubContext.Clients.Group($"Booking_{bookingId}")
                        .SendAsync("BulkStatusUpdate", notification);
                }

                _logger.LogInformation($"Sent bulk status update notification for {bookingIds.Count} bookings to status {newStatus}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send bulk status update notification for {bookingIds.Count} bookings");
            }
        }
    }
}