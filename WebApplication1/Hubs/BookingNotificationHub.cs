using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Security.Claims;

namespace WebApplication1.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time booking notifications
    /// Provides real-time updates to connected clients about booking status changes
    /// </summary>
    // [Authorize] // Temporarily removed for testing
    public class BookingNotificationHub : Hub
    {
        private readonly ILogger<BookingNotificationHub> _logger;

        public BookingNotificationHub(ILogger<BookingNotificationHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Called when a client connects to the hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            _logger.LogInformation($"User {userId} with role {userRole} connected to BookingNotificationHub");

            // Add user to appropriate groups based on their role
            if (!string.IsNullOrEmpty(userRole))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userRole);
                _logger.LogInformation($"Added user {userId} to group {userRole}");
            }

            // Add all users to a general notifications group
            await Groups.AddToGroupAsync(Context.ConnectionId, "AllUsers");

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects from the hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            _logger.LogInformation($"User {userId} disconnected from BookingNotificationHub");

            if (exception != null)
            {
                _logger.LogError(exception, $"User {userId} disconnected with error");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Join a specific booking group for real-time updates
        /// </summary>
        /// <param name="bookingId">The booking ID to subscribe to</param>
        public async Task JoinBookingGroup(string bookingId)
        {
            var userId = Context.UserIdentifier;
            var groupName = $"Booking_{bookingId}";

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation($"User {userId} joined booking group {groupName}");

            await Clients.Caller.SendAsync("JoinedBookingGroup", bookingId);
        }

        /// <summary>
        /// Leave a specific booking group
        /// </summary>
        /// <param name="bookingId">The booking ID to unsubscribe from</param>
        public async Task LeaveBookingGroup(string bookingId)
        {
            var userId = Context.UserIdentifier;
            var groupName = $"Booking_{bookingId}";

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation($"User {userId} left booking group {groupName}");

            await Clients.Caller.SendAsync("LeftBookingGroup", bookingId);
        }

        /// <summary>
        /// Join station notifications group for station operators
        /// </summary>
        /// <param name="stationId">The charging station ID</param>
        public async Task JoinStationGroup(string stationId)
        {
            var userId = Context.UserIdentifier;
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            // Only allow station operators to join station groups
            if (userRole == "StationOperator" || userRole == "Backoffice")
            {
                var groupName = $"Station_{stationId}";
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                _logger.LogInformation($"User {userId} joined station group {groupName}");

                await Clients.Caller.SendAsync("JoinedStationGroup", stationId);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Insufficient permissions to join station group");
            }
        }

        /// <summary>
        /// Leave station notifications group
        /// </summary>
        /// <param name="stationId">The charging station ID</param>
        public async Task LeaveStationGroup(string stationId)
        {
            var userId = Context.UserIdentifier;
            var groupName = $"Station_{stationId}";

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation($"User {userId} left station group {groupName}");

            await Clients.Caller.SendAsync("LeftStationGroup", stationId);
        }

        /// <summary>
        /// Send heartbeat to maintain connection
        /// </summary>
        public async Task Heartbeat()
        {
            await Clients.Caller.SendAsync("HeartbeatResponse", DateTime.UtcNow);
        }
    }
}