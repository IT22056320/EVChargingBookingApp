/*
 * File: QRCodeService.cs
 * Description: Service for generating QR codes for approved bookings
 * Author: EV Charging Team
 * Date: September 24, 2025
 */

using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using WebApplication1.Models;
using MongoDB.Driver;

namespace WebApplication1.Services
{
    /// <summary>
    /// Service for QR code generation and management
    /// </summary>
    public class QRCodeService
    {
        private readonly MongoDBService _mongoDBService;
        private readonly ILogger<QRCodeService> _logger;

        public QRCodeService(MongoDBService mongoDBService, ILogger<QRCodeService> logger)
        {
            _mongoDBService = mongoDBService;
            _logger = logger;
        }

        /// <summary>
        /// Generate QR code for an approved booking
        /// </summary>
        public async Task<(bool Success, string Message, string? QRCode)> GenerateQRCodeForBookingAsync(string bookingId)
        {
            try
            {
                var booking = await _mongoDBService.Bookings
                    .Find(b => b.Id == bookingId)
                    .FirstOrDefaultAsync();

                if (booking == null)
                {
                    return (false, "Booking not found", null);
                }

                if (booking.Status != BookingStatus.Approved)
                {
                    return (false, "QR code can only be generated for approved bookings", null);
                }

                // Check if QR code already exists
                if (!string.IsNullOrEmpty(booking.QRCode))
                {
                    return (true, "QR code already exists", booking.QRCode);
                }

                // Generate QR code data
                var qrData = GenerateQRCodeData(booking);
                var qrCodeString = GenerateQRCodeImage(qrData);

                // Update booking with QR code
                var updateDefinition = Builders<Booking>.Update
                    .Set(b => b.QRCode, qrCodeString)
                    .Set(b => b.QRCodeGeneratedAt, DateTime.UtcNow)
                    .Set(b => b.ModifiedAt, DateTime.UtcNow);

                await _mongoDBService.Bookings.UpdateOneAsync(
                    b => b.Id == bookingId,
                    updateDefinition
                );

                _logger.LogInformation($"QR code generated successfully for booking {bookingId}");
                return (true, "QR code generated successfully", qrCodeString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating QR code for booking {bookingId}");
                return (false, "An error occurred while generating the QR code", null);
            }
        }

        /// <summary>
        /// Validate QR code and get booking information
        /// </summary>
        public async Task<(bool IsValid, string Message, Booking? Booking)> ValidateQRCodeAsync(string qrCodeData)
        {
            try
            {
                // Parse QR code data to extract booking ID
                var bookingId = ParseQRCodeData(qrCodeData);
                if (string.IsNullOrEmpty(bookingId))
                {
                    return (false, "Invalid QR code format", null);
                }

                var booking = await _mongoDBService.Bookings
                    .Find(b => b.Id == bookingId)
                    .FirstOrDefaultAsync();

                if (booking == null)
                {
                    return (false, "Booking not found", null);
                }

                if (booking.Status != BookingStatus.Approved)
                {
                    return (false, $"Booking is not approved. Current status: {booking.Status}", booking);
                }

                // Check if booking is within valid time window
                var currentTime = DateTime.UtcNow;
                if (currentTime < booking.StartTime.AddMinutes(-15)) // Allow 15 minutes early
                {
                    return (false, "Booking time has not started yet", booking);
                }

                if (currentTime > booking.EndTime.AddMinutes(15)) // Allow 15 minutes late
                {
                    return (false, "Booking time has expired", booking);
                }

                return (true, "QR code is valid", booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating QR code");
                return (false, "Error validating QR code", null);
            }
        }

        /// <summary>
        /// Generate QR code for multiple bookings (bulk operation)
        /// </summary>
        public async Task<Dictionary<string, (bool Success, string Message, string? QRCode)>> GenerateQRCodesForBookingsAsync(List<string> bookingIds)
        {
            var results = new Dictionary<string, (bool Success, string Message, string? QRCode)>();

            foreach (var bookingId in bookingIds)
            {
                var result = await GenerateQRCodeForBookingAsync(bookingId);
                results[bookingId] = result;
            }

            return results;
        }

        /// <summary>
        /// Get QR code image as byte array
        /// </summary>
        public async Task<(bool Success, string Message, byte[]? ImageData)> GetQRCodeImageAsync(string bookingId)
        {
            try
            {
                var booking = await _mongoDBService.Bookings
                    .Find(b => b.Id == bookingId)
                    .FirstOrDefaultAsync();

                if (booking == null)
                {
                    return (false, "Booking not found", null);
                }

                if (string.IsNullOrEmpty(booking.QRCode))
                {
                    return (false, "QR code not found for this booking", null);
                }

                // Convert base64 QR code back to image
                var imageBytes = Convert.FromBase64String(booking.QRCode);
                return (true, "QR code image retrieved successfully", imageBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving QR code image for booking {bookingId}");
                return (false, "Error retrieving QR code image", null);
            }
        }

        /// <summary>
        /// Invalidate QR code (when booking is cancelled or completed)
        /// </summary>
        public async Task<(bool Success, string Message)> InvalidateQRCodeAsync(string bookingId)
        {
            try
            {
                var updateDefinition = Builders<Booking>.Update
                    .Set(b => b.QRCode, "")
                    .Set(b => b.QRCodeGeneratedAt, null)
                    .Set(b => b.ModifiedAt, DateTime.UtcNow);

                var result = await _mongoDBService.Bookings.UpdateOneAsync(
                    b => b.Id == bookingId,
                    updateDefinition
                );

                if (result.ModifiedCount > 0)
                {
                    return (true, "QR code invalidated successfully");
                }

                return (false, "Booking not found or QR code already invalidated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error invalidating QR code for booking {bookingId}");
                return (false, "Error invalidating QR code");
            }
        }

        #region Private Methods

        /// <summary>
        /// Generate QR code data structure
        /// </summary>
        private string GenerateQRCodeData(Booking booking)
        {
            var qrData = new
            {
                BookingId = booking.Id,
                UserId = booking.UserId,
                ChargingStationId = booking.ChargingStationId,
                StartTime = booking.StartTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                EndTime = booking.EndTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                VehicleNumber = booking.VehicleNumber,
                GeneratedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Signature = GenerateSignature(booking)
            };

            return System.Text.Json.JsonSerializer.Serialize(qrData);
        }

        /// <summary>
        /// Generate QR code image and return as base64 string
        /// </summary>
        private string GenerateQRCodeImage(string data)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);
            
            return Convert.ToBase64String(qrCodeBytes);
        }

        /// <summary>
        /// Parse QR code data to extract booking ID
        /// </summary>
        private string? ParseQRCodeData(string qrCodeData)
        {
            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(qrCodeData);
                var root = document.RootElement;
                
                if (root.TryGetProperty("BookingId", out var bookingIdElement))
                {
                    return bookingIdElement.GetString();
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Generate signature for QR code security
        /// </summary>
        private string GenerateSignature(Booking booking)
        {
            var signatureData = $"{booking.Id}:{booking.UserId}:{booking.ChargingStationId}:{booking.StartTime:yyyyMMddHHmmss}";
            
            // Simple hash-based signature (in production, use proper cryptographic signing)
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signatureData));
            return Convert.ToBase64String(hashBytes)[..16]; // Take first 16 characters
        }

        #endregion
    }
}