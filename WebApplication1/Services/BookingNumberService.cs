/*
 * File: BookingNumberService.cs
 * Description: Service for generating unique booking numbers
 * Author: [Your Team Name]
 * Date: [Current Date]
 */

using MongoDB.Driver;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Services
{
    /// <summary>
    /// Service for generating unique booking numbers
    /// </summary>
    public class BookingNumberService
    {
        private readonly MongoDBService _mongoDBService;
        private readonly ILogger<BookingNumberService> _logger;

        public BookingNumberService(MongoDBService mongoDBService, ILogger<BookingNumberService> logger)
        {
            _mongoDBService = mongoDBService;
            _logger = logger;
        }

        /// <summary>
        /// Generate a unique booking number in format BK-YYYYMMDD-XXXX
        /// </summary>
        public async Task<string> GenerateBookingNumberAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var dateString = today.ToString("yyyyMMdd");
                
                // Get the count of bookings created today
                var startOfDay = today;
                var endOfDay = today.AddDays(1);
                
                var filter = Builders<Booking>.Filter.And(
                    Builders<Booking>.Filter.Gte(b => b.CreatedAt, startOfDay),
                    Builders<Booking>.Filter.Lt(b => b.CreatedAt, endOfDay)
                );
                
                var todayBookingsCount = await _mongoDBService.Bookings.CountDocumentsAsync(filter);
                var sequenceNumber = todayBookingsCount + 1;
                
                // Generate booking number: BK-YYYYMMDD-XXXX
                var bookingNumber = $"BK-{dateString}-{sequenceNumber:D4}";
                
                // Ensure uniqueness by checking if it already exists
                var existingFilter = Builders<Booking>.Filter.Eq(b => b.BookingNumber, bookingNumber);
                var existingBooking = await _mongoDBService.Bookings.Find(existingFilter).FirstOrDefaultAsync();
                
                if (existingBooking != null)
                {
                    // If by chance it exists, increment and try again
                    sequenceNumber++;
                    bookingNumber = $"BK-{dateString}-{sequenceNumber:D4}";
                }
                
                _logger.LogInformation("Generated booking number: {BookingNumber}", bookingNumber);
                return bookingNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating booking number");
                // Fallback to timestamp-based number
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                return $"BK-{timestamp}";
            }
        }
    }
}