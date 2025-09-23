using Microsoft.AspNetCore.SignalR.Client;
using WebApplicationFrontend.Models;

namespace WebApplicationFrontend.Services
{
    /// <summary>
    /// Service for handling SignalR real-time notifications
    /// </summary>
    public interface ISignalRService
    {
        Task StartConnectionAsync();
        Task StopConnectionAsync();
        bool IsConnected { get; }
        event Action<BookingNotification>? BookingStatusChanged;
        event Action<BookingNotification>? BookingCreated;
        event Action<BookingNotification>? BookingUpdated;
        event Action<BookingNotification>? BookingDeleted;
        event Action<BulkUpdateNotification>? BulkStatusUpdate;
        Task JoinBookingGroupAsync(string bookingId);
        Task LeaveBookingGroupAsync(string bookingId);
        Task JoinStationGroupAsync(string stationId);
        Task LeaveStationGroupAsync(string stationId);
    }

    public class SignalRService : ISignalRService, IDisposable
    {
        private readonly HubConnection _connection;
        private readonly ILogger<SignalRService> _logger;
        private readonly IConfiguration _configuration;

        public SignalRService(ILogger<SignalRService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            var hubUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001";
            
            _connection = new HubConnectionBuilder()
                .WithUrl($"{hubUrl.Replace("/api", "")}/bookingNotificationHub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(GetAuthToken());
                })
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                .Build();

            RegisterEventHandlers();
        }

        public bool IsConnected => _connection.State == HubConnectionState.Connected;

        // Events for real-time notifications
        public event Action<BookingNotification>? BookingStatusChanged;
        public event Action<BookingNotification>? BookingCreated;
        public event Action<BookingNotification>? BookingUpdated;
        public event Action<BookingNotification>? BookingDeleted;
        public event Action<BulkUpdateNotification>? BulkStatusUpdate;

        private void RegisterEventHandlers()
        {
            // Register SignalR event handlers
            _connection.On<object>("BookingStatusChanged", (notification) =>
            {
                try
                {
                    var bookingNotification = ParseBookingNotification(notification);
                    if (bookingNotification != null)
                    {
                        BookingStatusChanged?.Invoke(bookingNotification);
                        _logger.LogInformation($"Received booking status changed notification for booking {bookingNotification.BookingId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing booking status changed notification");
                }
            });

            _connection.On<object>("BookingCreated", (notification) =>
            {
                try
                {
                    var bookingNotification = ParseBookingNotification(notification);
                    if (bookingNotification != null)
                    {
                        BookingCreated?.Invoke(bookingNotification);
                        _logger.LogInformation($"Received booking created notification for booking {bookingNotification.BookingId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing booking created notification");
                }
            });

            _connection.On<object>("BookingUpdated", (notification) =>
            {
                try
                {
                    var bookingNotification = ParseBookingNotification(notification);
                    if (bookingNotification != null)
                    {
                        BookingUpdated?.Invoke(bookingNotification);
                        _logger.LogInformation($"Received booking updated notification for booking {bookingNotification.BookingId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing booking updated notification");
                }
            });

            _connection.On<object>("BookingDeleted", (notification) =>
            {
                try
                {
                    var bookingNotification = ParseBookingNotification(notification);
                    if (bookingNotification != null)
                    {
                        BookingDeleted?.Invoke(bookingNotification);
                        _logger.LogInformation($"Received booking deleted notification for booking {bookingNotification.BookingId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing booking deleted notification");
                }
            });

            _connection.On<object>("BulkStatusUpdate", (notification) =>
            {
                try
                {
                    var bulkNotification = ParseBulkUpdateNotification(notification);
                    if (bulkNotification != null)
                    {
                        BulkStatusUpdate?.Invoke(bulkNotification);
                        _logger.LogInformation($"Received bulk status update notification for {bulkNotification.Count} bookings");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing bulk status update notification");
                }
            });

            _connection.On<string>("Error", (message) =>
            {
                _logger.LogError($"SignalR Hub Error: {message}");
            });

            // Connection lifecycle events
            _connection.Reconnecting += (error) =>
            {
                _logger.LogWarning(error, "SignalR connection lost. Attempting to reconnect...");
                return Task.CompletedTask;
            };

            _connection.Reconnected += (connectionId) =>
            {
                _logger.LogInformation($"SignalR reconnected. Connection ID: {connectionId}");
                return Task.CompletedTask;
            };

            _connection.Closed += async (error) =>
            {
                _logger.LogError(error, "SignalR connection closed.");
                await Task.Delay(5000);
                try
                {
                    await StartConnectionAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to restart SignalR connection");
                }
            };
        }

        public async Task StartConnectionAsync()
        {
            try
            {
                if (_connection.State == HubConnectionState.Disconnected)
                {
                    await _connection.StartAsync();
                    _logger.LogInformation("SignalR connection started successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start SignalR connection");
                throw;
            }
        }

        public async Task StopConnectionAsync()
        {
            try
            {
                if (_connection.State != HubConnectionState.Disconnected)
                {
                    await _connection.StopAsync();
                    _logger.LogInformation("SignalR connection stopped");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping SignalR connection");
            }
        }

        public async Task JoinBookingGroupAsync(string bookingId)
        {
            try
            {
                if (IsConnected)
                {
                    await _connection.InvokeAsync("JoinBookingGroup", bookingId);
                    _logger.LogInformation($"Joined booking group for booking {bookingId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to join booking group for booking {bookingId}");
            }
        }

        public async Task LeaveBookingGroupAsync(string bookingId)
        {
            try
            {
                if (IsConnected)
                {
                    await _connection.InvokeAsync("LeaveBookingGroup", bookingId);
                    _logger.LogInformation($"Left booking group for booking {bookingId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to leave booking group for booking {bookingId}");
            }
        }

        public async Task JoinStationGroupAsync(string stationId)
        {
            try
            {
                if (IsConnected)
                {
                    await _connection.InvokeAsync("JoinStationGroup", stationId);
                    _logger.LogInformation($"Joined station group for station {stationId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to join station group for station {stationId}");
            }
        }

        public async Task LeaveStationGroupAsync(string stationId)
        {
            try
            {
                if (IsConnected)
                {
                    await _connection.InvokeAsync("LeaveStationGroup", stationId);
                    _logger.LogInformation($"Left station group for station {stationId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to leave station group for station {stationId}");
            }
        }

        private string? GetAuthToken()
        {
            // TODO: Implement token retrieval logic
            // This should return the JWT token for the current user
            return null;
        }

        private BookingNotification? ParseBookingNotification(object notification)
        {
            try
            {
                // Convert the notification object to BookingNotification
                var json = System.Text.Json.JsonSerializer.Serialize(notification);
                return System.Text.Json.JsonSerializer.Deserialize<BookingNotification>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse booking notification");
                return null;
            }
        }

        private BulkUpdateNotification? ParseBulkUpdateNotification(object notification)
        {
            try
            {
                // Convert the notification object to BulkUpdateNotification
                var json = System.Text.Json.JsonSerializer.Serialize(notification);
                return System.Text.Json.JsonSerializer.Deserialize<BulkUpdateNotification>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse bulk update notification");
                return null;
            }
        }

        public void Dispose()
        {
            _connection?.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }

    // Notification models
    public class BookingNotification
    {
        public string? BookingId { get; set; }
        public string? OldStatus { get; set; }
        public string? NewStatus { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Type { get; set; }
        public BookingResponse? Booking { get; set; }
    }

    public class BulkUpdateNotification
    {
        public List<string>? BookingIds { get; set; }
        public string? NewStatus { get; set; }
        public int Count { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Type { get; set; }
    }
}