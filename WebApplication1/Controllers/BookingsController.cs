/*
 * File: BookingsController.cs
 * Description: API Controller for booking management with comprehensive CRUD operations
 * Author: EV Charging Team
 * Date: September 24, 2025
 */

using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using WebApplication1.DTOs;
using WebApplication1.Models;
using WebApplication1.Services;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// API Controller for booking operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class BookingsController : ControllerBase
    {
        private readonly BookingService _bookingService;
        private readonly QRCodeService _qrCodeService;
        private readonly MongoDBService _mongoDBService;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(
            BookingService bookingService,
            QRCodeService qrCodeService,
            MongoDBService mongoDBService,
            ILogger<BookingsController> logger)
        {
            _bookingService = bookingService;
            _qrCodeService = qrCodeService;
            _mongoDBService = mongoDBService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new booking
        /// </summary>
        /// <param name="createBookingDto">Booking creation data</param>
        /// <returns>Created booking information</returns>
        [HttpPost]
        [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto createBookingDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _bookingService.CreateBookingAsync(createBookingDto);

                if (!result.Success)
                {
                    return BadRequest(result.Message);
                }

                var bookingResponse = MapToBookingResponseDto(result.Booking!);
                return CreatedAtAction(nameof(GetBookingById), new { id = result.Booking!.Id }, bookingResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking");
                return StatusCode(500, "An error occurred while creating the booking");
            }
        }

        /// <summary>
        /// Get booking by ID
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <returns>Booking details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBookingById(string id)
        {
            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(id);

                if (booking == null)
                {
                    return NotFound("Booking not found");
                }

                var bookingResponse = await MapToBookingResponseDtoWithDetailsAsync(booking);
                return Ok(bookingResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving booking {id}");
                return StatusCode(500, "An error occurred while retrieving the booking");
            }
        }

        /// <summary>
        /// Get bookings with search and filter
        /// </summary>
        /// <param name="searchDto">Search and filter parameters</param>
        /// <returns>Paginated booking list</returns>
        [HttpGet]
        [ProducesResponseType(typeof(BookingPagedResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBookings([FromQuery] BookingSearchDto searchDto)
        {
            try
            {
                var result = await _bookingService.GetBookingsAsync(searchDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bookings");
                return StatusCode(500, "An error occurred while retrieving bookings");
            }
        }

        /// <summary>
        /// Get bookings for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20)</param>
        /// <returns>User's bookings</returns>
        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(BookingPagedResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserBookings(
            string userId, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var searchDto = new BookingSearchDto
                {
                    UserId = userId,
                    Page = page,
                    PageSize = pageSize
                };

                var result = await _bookingService.GetBookingsAsync(searchDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving bookings for user {userId}");
                return StatusCode(500, "An error occurred while retrieving user bookings");
            }
        }

        /// <summary>
        /// Get bookings for a specific charging station
        /// </summary>
        /// <param name="stationId">Charging station ID</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20)</param>
        /// <returns>Station's bookings</returns>
        [HttpGet("station/{stationId}")]
        [ProducesResponseType(typeof(BookingPagedResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStationBookings(
            string stationId, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var searchDto = new BookingSearchDto
                {
                    ChargingStationId = stationId,
                    Page = page,
                    PageSize = pageSize
                };

                var result = await _bookingService.GetBookingsAsync(searchDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving bookings for station {stationId}");
                return StatusCode(500, "An error occurred while retrieving station bookings");
            }
        }

        /// <summary>
        /// Update an existing booking
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <param name="updateBookingDto">Updated booking data</param>
        /// <returns>Updated booking information</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateBooking(string id, [FromBody] UpdateBookingDto updateBookingDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _bookingService.UpdateBookingAsync(id, updateBookingDto);

                if (!result.Success)
                {
                    if (result.Message.Contains("not found"))
                    {
                        return NotFound(result.Message);
                    }
                    return BadRequest(result.Message);
                }

                var bookingResponse = await MapToBookingResponseDtoWithDetailsAsync(result.Booking!);
                return Ok(bookingResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating booking {id}");
                return StatusCode(500, "An error occurred while updating the booking");
            }
        }

        /// <summary>
        /// Update booking status
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <param name="statusDto">Status update data</param>
        /// <returns>Success message</returns>
        [HttpPatch("{id}/status")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateBookingStatus(string id, [FromBody] UpdateBookingStatusDto statusDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _bookingService.UpdateBookingStatusAsync(id, statusDto);

                if (!result.Success)
                {
                    if (result.Message.Contains("not found"))
                    {
                        return NotFound(result.Message);
                    }
                    return BadRequest(result.Message);
                }

                // Generate QR code if booking is approved
                if (statusDto.Status == BookingStatus.Approved)
                {
                    var qrResult = await _qrCodeService.GenerateQRCodeForBookingAsync(id);
                    if (!qrResult.Success)
                    {
                        _logger.LogWarning($"Failed to generate QR code for approved booking {id}: {qrResult.Message}");
                    }
                }

                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating booking status {id}");
                return StatusCode(500, "An error occurred while updating the booking status");
            }
        }

        /// <summary>
        /// Approve a booking
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <param name="approveDto">Approval data</param>
        /// <returns>Success message</returns>
        [HttpPost("{id}/approve")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApproveBooking(string id, [FromBody] ApproveBookingDto approveDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var statusDto = new UpdateBookingStatusDto
                {
                    Status = approveDto.IsApproved ? BookingStatus.Approved : BookingStatus.Rejected,
                    UpdatedBy = approveDto.ApprovedBy,
                    Reason = approveDto.Reason
                };

                var result = await _bookingService.UpdateBookingStatusAsync(id, statusDto);

                if (!result.Success)
                {
                    if (result.Message.Contains("not found"))
                    {
                        return NotFound(result.Message);
                    }
                    return BadRequest(result.Message);
                }

                // Generate QR code if approved
                string? qrCode = null;
                if (approveDto.IsApproved)
                {
                    var qrResult = await _qrCodeService.GenerateQRCodeForBookingAsync(id);
                    if (qrResult.Success)
                    {
                        qrCode = qrResult.QRCode;
                    }
                }

                return Ok(new { 
                    message = result.Message, 
                    qrCode = qrCode,
                    approved = approveDto.IsApproved 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving booking {id}");
                return StatusCode(500, "An error occurred while processing the booking approval");
            }
        }

        /// <summary>
        /// Cancel a booking
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <param name="cancelDto">Cancellation data</param>
        /// <returns>Success message</returns>
        [HttpPost("{id}/cancel")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelBooking(string id, [FromBody] CancelBookingDto cancelDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var statusDto = new UpdateBookingStatusDto
                {
                    Status = BookingStatus.Cancelled,
                    UpdatedBy = cancelDto.CancelledBy,
                    Reason = cancelDto.CancellationReason
                };

                var result = await _bookingService.UpdateBookingStatusAsync(id, statusDto);

                if (!result.Success)
                {
                    if (result.Message.Contains("not found"))
                    {
                        return NotFound(result.Message);
                    }
                    return BadRequest(result.Message);
                }

                // Invalidate QR code
                await _qrCodeService.InvalidateQRCodeAsync(id);

                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling booking {id}");
                return StatusCode(500, "An error occurred while cancelling the booking");
            }
        }

        /// <summary>
        /// Delete a booking (soft delete)
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <param name="deletedBy">User performing the deletion</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteBooking(string id, [FromQuery] [Required] string deletedBy)
        {
            try
            {
                if (string.IsNullOrEmpty(deletedBy))
                {
                    return BadRequest("deletedBy parameter is required");
                }

                var result = await _bookingService.DeleteBookingAsync(id, deletedBy);

                if (!result.Success)
                {
                    if (result.Message.Contains("not found"))
                    {
                        return NotFound(result.Message);
                    }
                    return BadRequest(result.Message);
                }

                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting booking {id}");
                return StatusCode(500, "An error occurred while deleting the booking");
            }
        }

        /// <summary>
        /// Check time slot availability
        /// </summary>
        /// <param name="timeSlotDto">Time slot check parameters</param>
        /// <returns>Availability status</returns>
        [HttpPost("check-availability")]
        [ProducesResponseType(typeof(TimeSlotAvailabilityDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CheckTimeSlotAvailability([FromBody] TimeSlotCheckDto timeSlotDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _bookingService.CheckTimeSlotAvailabilityAsync(timeSlotDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking time slot availability");
                return StatusCode(500, "An error occurred while checking availability");
            }
        }

        /// <summary>
        /// Get booking statistics
        /// </summary>
        /// <param name="fromDate">Start date for statistics (optional)</param>
        /// <param name="toDate">End date for statistics (optional)</param>
        /// <returns>Booking statistics</returns>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(BookingStatsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBookingStatistics(
            [FromQuery] DateTime? fromDate = null, 
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var stats = await _bookingService.GetBookingStatsAsync(fromDate, toDate);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving booking statistics");
                return StatusCode(500, "An error occurred while retrieving statistics");
            }
        }

        /// <summary>
        /// Get QR code for a booking
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <returns>QR code image</returns>
        [HttpGet("{id}/qrcode")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetQRCode(string id)
        {
            try
            {
                var result = await _qrCodeService.GetQRCodeImageAsync(id);

                if (!result.Success)
                {
                    if (result.Message.Contains("not found"))
                    {
                        return NotFound(result.Message);
                    }
                    return BadRequest(result.Message);
                }

                return File(result.ImageData!, "image/png", $"booking-{id}-qrcode.png");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving QR code for booking {id}");
                return StatusCode(500, "An error occurred while retrieving the QR code");
            }
        }

        /// <summary>
        /// Validate QR code
        /// </summary>
        /// <param name="qrData">QR code data</param>
        /// <returns>Validation result</returns>
        [HttpPost("validate-qr")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ValidateQRCode([FromBody] string qrData)
        {
            try
            {
                if (string.IsNullOrEmpty(qrData))
                {
                    return BadRequest("QR code data is required");
                }

                var result = await _qrCodeService.ValidateQRCodeAsync(qrData);

                return Ok(new
                {
                    isValid = result.IsValid,
                    message = result.Message,
                    booking = result.IsValid ? MapToBookingResponseDto(result.Booking!) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating QR code");
                return StatusCode(500, "An error occurred while validating the QR code");
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Map Booking entity to BookingResponseDto
        /// </summary>
        private BookingResponseDto MapToBookingResponseDto(Booking booking)
        {
            return new BookingResponseDto
            {
                Id = booking.Id ?? "",
                UserId = booking.UserId,
                ChargingStationId = booking.ChargingStationId,
                BookingDate = booking.BookingDate,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Status = booking.Status,
                VehicleNumber = booking.VehicleNumber,
                VehicleType = booking.VehicleType,
                EstimatedChargingTimeMinutes = booking.EstimatedChargingTimeMinutes,
                Notes = booking.Notes,
                QRCode = booking.QRCode,
                QRCodeGeneratedAt = booking.QRCodeGeneratedAt,
                CreatedAt = booking.CreatedAt,
                ModifiedAt = booking.ModifiedAt,
                ApprovedAt = booking.ApprovedAt,
                ApprovedBy = booking.ApprovedBy,
                CompletedAt = booking.CompletedAt,
                CancelledAt = booking.CancelledAt,
                CancelledBy = booking.CancelledBy,
                CancellationReason = booking.CancellationReason,
                RejectedAt = booking.RejectedAt,
                RejectedBy = booking.RejectedBy,
                RejectionReason = booking.RejectionReason,
                ActualStartTime = booking.ActualStartTime,
                ActualEndTime = booking.ActualEndTime,
                TotalCost = booking.TotalCost,
                EnergyConsumedKWh = booking.EnergyConsumedKWh,
                IsWithinBookingWindow = booking.IsWithinBookingWindow,
                CanBeModified = booking.CanBeModified,
                CanBeCancelled = booking.CanBeCancelled,
                IsActive = booking.IsActive,
                DurationMinutes = booking.DurationMinutes
            };
        }

        /// <summary>
        /// Map Booking entity to BookingResponseDto with user and station details
        /// </summary>
        private async Task<BookingResponseDto> MapToBookingResponseDtoWithDetailsAsync(Booking booking)
        {
            var response = MapToBookingResponseDto(booking);

            // Get user details
            try
            {
                var user = await _mongoDBService.EVOwners
                    .Find(u => u.Id == booking.UserId)
                    .FirstOrDefaultAsync();

                if (user != null)
                {
                    response.User = new UserResponseDto
                    {
                        Id = user.Id ?? "",
                        NIC = user.NIC,
                        FullName = user.FullName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error loading user details for booking {booking.Id}");
            }

            // Get charging station details
            try
            {
                var station = await _mongoDBService.ChargingStations
                    .Find(cs => cs.Id == booking.ChargingStationId)
                    .FirstOrDefaultAsync();

                if (station != null)
                {
                    response.ChargingStation = new ChargingStationResponseDto
                    {
                        Id = station.Id ?? "",
                        StationName = station.StationName,
                        Location = station.Location,
                        Address = station.Address,
                        ConnectorType = station.ConnectorType.ToString(),
                        PowerRatingKW = station.PowerRatingKW,
                        PricePerKWh = station.PricePerKWh,
                        Status = station.Status.ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error loading charging station details for booking {booking.Id}");
            }

            return response;
        }

        /// <summary>
        /// Get booking history for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="limit">Maximum number of bookings to return</param>
        /// <returns>User's booking history</returns>
        [HttpGet("history/{userId}")]
        [ProducesResponseType(typeof(List<BookingResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserBookingHistory(string userId, [FromQuery] int limit = 50)
        {
            try
            {
                var bookings = await _bookingService.GetUserBookingHistoryAsync(userId, limit);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving booking history for user {userId}");
                return StatusCode(500, "An error occurred while retrieving booking history");
            }
        }

        /// <summary>
        /// Get bookings by date range
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="stationId">Optional station ID filter</param>
        /// <returns>Bookings in date range</returns>
        [HttpGet("date-range")]
        [ProducesResponseType(typeof(List<BookingResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBookingsByDateRange(
            [FromQuery] DateTime startDate, 
            [FromQuery] DateTime endDate, 
            [FromQuery] string? stationId = null)
        {
            try
            {
                var bookings = await _bookingService.GetBookingsByDateRangeAsync(startDate, endDate, stationId);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bookings by date range");
                return StatusCode(500, "An error occurred while retrieving bookings");
            }
        }

        /// <summary>
        /// Check for booking conflicts
        /// </summary>
        /// <param name="stationId">Charging station ID</param>
        /// <param name="startTime">Proposed start time</param>
        /// <param name="endTime">Proposed end time</param>
        /// <param name="excludeBookingId">Booking ID to exclude from conflict check</param>
        /// <returns>True if there are conflicts</returns>
        [HttpGet("check-conflict")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckBookingConflict(
            [FromQuery] string stationId,
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime,
            [FromQuery] string? excludeBookingId = null)
        {
            try
            {
                var hasConflict = await _bookingService.HasBookingConflictAsync(stationId, startTime, endTime, excludeBookingId);
                return Ok(hasConflict);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking booking conflicts");
                return StatusCode(500, "An error occurred while checking booking conflicts");
            }
        }

        #endregion
    }
}