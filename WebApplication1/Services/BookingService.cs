/*
 * File: BookingService.cs
 * Description: Service for booking-related business logic and database operations
 * Author: EV Charging Team
 * Date: September 24, 2025
 */

using MongoDB.Driver;
using WebApplication1.Models;
using WebApplication1.DTOs;
using System.Linq.Expressions;

namespace WebApplication1.Services
{
    /// <summary>
    /// Service for booking-related operations
    /// </summary>
    public class BookingService
    {
        private readonly MongoDBService _mongoDBService;
        private readonly BookingNumberService _bookingNumberService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<BookingService> _logger;

        public BookingService(
            MongoDBService mongoDBService,
            BookingNumberService bookingNumberService,
            INotificationService notificationService,
            ILogger<BookingService> logger)
        {
            _mongoDBService = mongoDBService;
            _bookingNumberService = bookingNumberService;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new booking with business rule validation
        /// </summary>
        public async Task<(bool Success, string Message, Booking? Booking)> CreateBookingAsync(CreateBookingDto createBookingDto)
        {
            try
            {
                // Validate business rules
                var validationResult = await ValidateBookingRulesAsync(createBookingDto);
                if (!validationResult.IsValid)
                {
                    return (false, validationResult.Message, null);
                }

                // Check for booking conflicts
                var hasConflict = await HasBookingConflictAsync(
                    createBookingDto.ChargingStationId,
                    createBookingDto.StartTime,
                    createBookingDto.EndTime
                );

                if (hasConflict)
                {
                    return (false, "The selected time slot conflicts with an existing booking. Please choose a different time.", null);
                }

                // Generate unique booking number
                var bookingNumber = await _bookingNumberService.GenerateBookingNumberAsync();

                // Calculate estimated cost
                var chargingStation = await _mongoDBService.ChargingStations
                    .Find(cs => cs.Id == createBookingDto.ChargingStationId)
                    .FirstOrDefaultAsync();
                
                decimal? estimatedCost = null;
                if (chargingStation != null)
                {
                    var durationHours = (decimal)(createBookingDto.EndTime - createBookingDto.StartTime).TotalHours;
                    var estimatedKWh = durationHours * (chargingStation.PowerRatingKW * 0.8m); // Assume 80% efficiency
                    estimatedCost = estimatedKWh * chargingStation.PricePerKWh;
                }

                var booking = new Booking
                {
                    BookingNumber = bookingNumber,
                    UserId = createBookingDto.UserId,
                    ChargingStationId = createBookingDto.ChargingStationId,
                    BookingDate = createBookingDto.BookingDate,
                    StartTime = createBookingDto.StartTime,
                    EndTime = createBookingDto.EndTime,
                    VehicleNumber = createBookingDto.VehicleNumber,
                    VehicleType = createBookingDto.VehicleType,
                    EstimatedChargingTimeMinutes = createBookingDto.EstimatedChargingTimeMinutes,
                    Notes = createBookingDto.Notes,
                    Status = BookingStatus.Pending,
                    TotalCost = estimatedCost,
                    CreatedAt = DateTime.UtcNow
                };

                await _mongoDBService.Bookings.InsertOneAsync(booking);

                // Send real-time notification
                var bookingDto = await MapToBookingResponseDtoAsync(booking);
                await _notificationService.SendBookingCreatedAsync(bookingDto);

                _logger.LogInformation($"Booking created successfully for user {createBookingDto.UserId}");
                return (true, "Booking created successfully", booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking");
                return (false, "An error occurred while creating the booking", null);
            }
        }

        /// <summary>
        /// Update an existing booking with business rule validation
        /// </summary>
        public async Task<(bool Success, string Message, Booking? Booking)> UpdateBookingAsync(string bookingId, UpdateBookingDto updateBookingDto)
        {
            try
            {
                var existingBooking = await GetBookingByIdAsync(bookingId);
                if (existingBooking == null)
                {
                    return (false, "Booking not found", null);
                }

                // Check if booking can be modified
                if (!existingBooking.CanBeModified)
                {
                    return (false, "Booking cannot be modified. Either it's not in pending status or the modification window has passed (12 hours before start time)", null);
                }

                var updateDefinition = Builders<Booking>.Update;
                var updates = new List<UpdateDefinition<Booking>>();

                if (updateBookingDto.BookingDate.HasValue)
                {
                    updates.Add(updateDefinition.Set(b => b.BookingDate, updateBookingDto.BookingDate.Value));
                }

                if (updateBookingDto.StartTime.HasValue)
                {
                    updates.Add(updateDefinition.Set(b => b.StartTime, updateBookingDto.StartTime.Value));
                }

                if (updateBookingDto.EndTime.HasValue)
                {
                    updates.Add(updateDefinition.Set(b => b.EndTime, updateBookingDto.EndTime.Value));
                }

                if (!string.IsNullOrEmpty(updateBookingDto.VehicleNumber))
                {
                    updates.Add(updateDefinition.Set(b => b.VehicleNumber, updateBookingDto.VehicleNumber));
                }

                if (!string.IsNullOrEmpty(updateBookingDto.VehicleType))
                {
                    updates.Add(updateDefinition.Set(b => b.VehicleType, updateBookingDto.VehicleType));
                }

                if (updateBookingDto.EstimatedChargingTimeMinutes.HasValue)
                {
                    updates.Add(updateDefinition.Set(b => b.EstimatedChargingTimeMinutes, updateBookingDto.EstimatedChargingTimeMinutes.Value));
                }

                if (!string.IsNullOrEmpty(updateBookingDto.Notes))
                {
                    updates.Add(updateDefinition.Set(b => b.Notes, updateBookingDto.Notes));
                }

                updates.Add(updateDefinition.Set(b => b.ModifiedAt, DateTime.UtcNow));

                if (updates.Any())
                {
                    var combinedUpdate = updateDefinition.Combine(updates);
                    await _mongoDBService.Bookings.UpdateOneAsync(
                        b => b.Id == bookingId,
                        combinedUpdate
                    );
                }

                var updatedBooking = await GetBookingByIdAsync(bookingId);
                
                // Send real-time notification
                if (updatedBooking != null)
                {
                    var bookingDto = MapToBookingResponseDto(updatedBooking);
                    await _notificationService.SendBookingUpdatedAsync(bookingDto);
                }
                
                return (true, "Booking updated successfully", updatedBooking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating booking {bookingId}");
                return (false, "An error occurred while updating the booking", null);
            }
        }

        /// <summary>
        /// Update booking status
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateBookingStatusAsync(string bookingId, UpdateBookingStatusDto statusDto)
        {
            try
            {
                var existingBooking = await GetBookingByIdAsync(bookingId);
                if (existingBooking == null)
                {
                    return (false, "Booking not found");
                }

                var updateDefinition = Builders<Booking>.Update
                    .Set(b => b.Status, statusDto.Status)
                    .Set(b => b.ModifiedAt, DateTime.UtcNow);

                switch (statusDto.Status)
                {
                    case BookingStatus.Approved:
                        updateDefinition = updateDefinition
                            .Set(b => b.ApprovedAt, DateTime.UtcNow)
                            .Set(b => b.ApprovedBy, statusDto.UpdatedBy ?? "");
                        break;

                    case BookingStatus.Completed:
                        updateDefinition = updateDefinition
                            .Set(b => b.CompletedAt, DateTime.UtcNow);
                        
                        if (statusDto.ActualStartTime.HasValue)
                            updateDefinition = updateDefinition.Set(b => b.ActualStartTime, statusDto.ActualStartTime.Value);
                        
                        if (statusDto.ActualEndTime.HasValue)
                            updateDefinition = updateDefinition.Set(b => b.ActualEndTime, statusDto.ActualEndTime.Value);
                        
                        if (statusDto.TotalCost.HasValue)
                            updateDefinition = updateDefinition.Set(b => b.TotalCost, statusDto.TotalCost.Value);
                        
                        if (statusDto.EnergyConsumedKWh.HasValue)
                            updateDefinition = updateDefinition.Set(b => b.EnergyConsumedKWh, statusDto.EnergyConsumedKWh.Value);
                        break;

                    case BookingStatus.Cancelled:
                        updateDefinition = updateDefinition
                            .Set(b => b.CancelledAt, DateTime.UtcNow)
                            .Set(b => b.CancelledBy, statusDto.UpdatedBy ?? "")
                            .Set(b => b.CancellationReason, statusDto.Reason ?? "");
                        break;

                    case BookingStatus.Rejected:
                        updateDefinition = updateDefinition
                            .Set(b => b.RejectedAt, DateTime.UtcNow)
                            .Set(b => b.RejectedBy, statusDto.UpdatedBy ?? "")
                            .Set(b => b.RejectionReason, statusDto.Reason ?? "");
                        break;
                }

                await _mongoDBService.Bookings.UpdateOneAsync(
                    b => b.Id == bookingId,
                    updateDefinition
                );

                // Send real-time notification for status change
                await _notificationService.SendBookingStatusUpdateAsync(
                    bookingId, 
                    existingBooking.Status, 
                    statusDto.Status, 
                    statusDto.Reason);

                return (true, "Booking status updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating booking status {bookingId}");
                return (false, "An error occurred while updating the booking status");
            }
        }

        /// <summary>
        /// Get booking by ID
        /// </summary>
        public async Task<Booking?> GetBookingByIdAsync(string bookingId)
        {
            try
            {
                var filter = Builders<Booking>.Filter.Eq(b => b.Id, bookingId);
                return await _mongoDBService.Bookings.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving booking {bookingId}");
                return null;
            }
        }

        /// <summary>
        /// Get bookings with search and filter
        /// </summary>
        public async Task<BookingPagedResponseDto> GetBookingsAsync(BookingSearchDto searchDto)
        {
            try
            {
                var filterBuilder = Builders<Booking>.Filter;
                var filters = new List<FilterDefinition<Booking>>();

                if (!string.IsNullOrEmpty(searchDto.UserId))
                    filters.Add(filterBuilder.Eq(b => b.UserId, searchDto.UserId));

                if (!string.IsNullOrEmpty(searchDto.ChargingStationId))
                    filters.Add(filterBuilder.Eq(b => b.ChargingStationId, searchDto.ChargingStationId));

                if (searchDto.Status.HasValue)
                    filters.Add(filterBuilder.Eq(b => b.Status, searchDto.Status.Value));

                if (searchDto.BookingDateFrom.HasValue)
                    filters.Add(filterBuilder.Gte(b => b.BookingDate, searchDto.BookingDateFrom.Value));

                if (searchDto.BookingDateTo.HasValue)
                    filters.Add(filterBuilder.Lte(b => b.BookingDate, searchDto.BookingDateTo.Value));

                if (searchDto.CreatedFrom.HasValue)
                    filters.Add(filterBuilder.Gte(b => b.CreatedAt, searchDto.CreatedFrom.Value));

                if (searchDto.CreatedTo.HasValue)
                    filters.Add(filterBuilder.Lte(b => b.CreatedAt, searchDto.CreatedTo.Value));

                if (!string.IsNullOrEmpty(searchDto.VehicleNumber))
                    filters.Add(filterBuilder.Regex(b => b.VehicleNumber, new MongoDB.Bson.BsonRegularExpression(searchDto.VehicleNumber, "i")));

                var finalFilter = filters.Any() ? filterBuilder.And(filters) : filterBuilder.Empty;

                // Sorting
                SortDefinition<Booking> sort;
                switch (searchDto.SortBy.ToLower())
                {
                    case "bookingdate":
                        sort = searchDto.SortDescending ? 
                            Builders<Booking>.Sort.Descending(b => b.BookingDate) :
                            Builders<Booking>.Sort.Ascending(b => b.BookingDate);
                        break;
                    case "status":
                        sort = searchDto.SortDescending ? 
                            Builders<Booking>.Sort.Descending(b => b.Status) :
                            Builders<Booking>.Sort.Ascending(b => b.Status);
                        break;
                    default:
                        sort = searchDto.SortDescending ? 
                            Builders<Booking>.Sort.Descending(b => b.CreatedAt) :
                            Builders<Booking>.Sort.Ascending(b => b.CreatedAt);
                        break;
                }

                var totalCount = await _mongoDBService.Bookings.CountDocumentsAsync(finalFilter);
                var bookings = await _mongoDBService.Bookings
                    .Find(finalFilter)
                    .Sort(sort)
                    .Skip((searchDto.Page - 1) * searchDto.PageSize)
                    .Limit(searchDto.PageSize)
                    .ToListAsync();

                // Map bookings with related data in parallel
                var bookingTasks = bookings.Select(MapToBookingResponseDtoAsync);
                var mappedBookings = await Task.WhenAll(bookingTasks);

                var totalPages = (int)Math.Ceiling((double)totalCount / searchDto.PageSize);

                return new BookingPagedResponseDto
                {
                    Bookings = mappedBookings.ToList(),
                    TotalCount = (int)totalCount,
                    Page = searchDto.Page,
                    PageSize = searchDto.PageSize,
                    TotalPages = totalPages,
                    HasNextPage = searchDto.Page < totalPages,
                    HasPreviousPage = searchDto.Page > 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bookings");
                return new BookingPagedResponseDto();
            }
        }

        /// <summary>
        /// Check time slot availability
        /// </summary>
        public async Task<TimeSlotAvailabilityDto> CheckTimeSlotAvailabilityAsync(TimeSlotCheckDto timeSlotDto)
        {
            try
            {
                var filterBuilder = Builders<Booking>.Filter;
                var filters = new List<FilterDefinition<Booking>>
                {
                    filterBuilder.Eq(b => b.ChargingStationId, timeSlotDto.ChargingStationId),
                    filterBuilder.In(b => b.Status, new[] { BookingStatus.Pending, BookingStatus.Approved }),
                    filterBuilder.Or(
                        filterBuilder.And(
                            filterBuilder.Lt(b => b.StartTime, timeSlotDto.EndTime),
                            filterBuilder.Gt(b => b.EndTime, timeSlotDto.StartTime)
                        )
                    )
                };

                if (!string.IsNullOrEmpty(timeSlotDto.ExcludeBookingId))
                {
                    filters.Add(filterBuilder.Ne(b => b.Id, timeSlotDto.ExcludeBookingId));
                }

                var finalFilter = filterBuilder.And(filters);
                var conflictingBookings = await _mongoDBService.Bookings.Find(finalFilter).ToListAsync();

                if (conflictingBookings.Any())
                {
                    return new TimeSlotAvailabilityDto
                    {
                        IsAvailable = false,
                        Message = "Time slot is not available due to existing bookings",
                        ConflictingBookings = conflictingBookings.Select(b => new ConflictingBookingDto
                        {
                            BookingId = b.Id ?? "",
                            StartTime = b.StartTime,
                            EndTime = b.EndTime,
                            Status = b.Status,
                            UserName = b.UserId // TODO: Join with user data for actual name
                        }).ToList()
                    };
                }

                return new TimeSlotAvailabilityDto
                {
                    IsAvailable = true,
                    Message = "Time slot is available"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking time slot availability");
                return new TimeSlotAvailabilityDto
                {
                    IsAvailable = false,
                    Message = "Error checking availability"
                };
            }
        }

        /// <summary>
        /// Get booking statistics
        /// </summary>
        public async Task<BookingStatsDto> GetBookingStatsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var filterBuilder = Builders<Booking>.Filter;
                var filters = new List<FilterDefinition<Booking>>();

                if (fromDate.HasValue)
                    filters.Add(filterBuilder.Gte(b => b.CreatedAt, fromDate.Value));

                if (toDate.HasValue)
                    filters.Add(filterBuilder.Lte(b => b.CreatedAt, toDate.Value));

                var finalFilter = filters.Any() ? filterBuilder.And(filters) : filterBuilder.Empty;

                var bookings = await _mongoDBService.Bookings.Find(finalFilter).ToListAsync();

                return new BookingStatsDto
                {
                    TotalBookings = bookings.Count,
                    PendingBookings = bookings.Count(b => b.Status == BookingStatus.Pending),
                    ApprovedBookings = bookings.Count(b => b.Status == BookingStatus.Approved),
                    CompletedBookings = bookings.Count(b => b.Status == BookingStatus.Completed),
                    CancelledBookings = bookings.Count(b => b.Status == BookingStatus.Cancelled),
                    RejectedBookings = bookings.Count(b => b.Status == BookingStatus.Rejected),
                    TotalRevenue = bookings.Where(b => b.TotalCost.HasValue).Sum(b => b.TotalCost ?? 0),
                    AverageBookingDuration = bookings.Any() ? (decimal)bookings.Average(b => b.DurationMinutes) : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving booking statistics");
                return new BookingStatsDto();
            }
        }

        /// <summary>
        /// Delete a booking (soft delete by marking as cancelled)
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteBookingAsync(string bookingId, string deletedBy)
        {
            try
            {
                var existingBooking = await GetBookingByIdAsync(bookingId);
                if (existingBooking == null)
                {
                    return (false, "Booking not found");
                }

                if (!existingBooking.CanBeCancelled)
                {
                    return (false, "Booking cannot be cancelled");
                }

                var updateDefinition = Builders<Booking>.Update
                    .Set(b => b.Status, BookingStatus.Cancelled)
                    .Set(b => b.CancelledAt, DateTime.UtcNow)
                    .Set(b => b.CancelledBy, deletedBy)
                    .Set(b => b.CancellationReason, "Booking deleted")
                    .Set(b => b.ModifiedAt, DateTime.UtcNow);

                await _mongoDBService.Bookings.UpdateOneAsync(
                    b => b.Id == bookingId,
                    updateDefinition
                );

                // Send real-time notification for deletion
                await _notificationService.SendBookingDeletedAsync(bookingId, existingBooking.UserId);

                return (true, "Booking deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting booking {bookingId}");
                return (false, "An error occurred while deleting the booking");
            }
        }

        /// <summary>
        /// Cancel a booking with reason
        /// </summary>
        public async Task<(bool Success, string Message)> CancelBookingAsync(string bookingId, string cancelledBy, string reason)
        {
            try
            {
                var existingBooking = await GetBookingByIdAsync(bookingId);
                if (existingBooking == null)
                {
                    return (false, "Booking not found");
                }

                if (!existingBooking.CanBeCancelled)
                {
                    return (false, "Booking cannot be cancelled. It may already be completed or cancelled.");
                }

                var updateDefinition = Builders<Booking>.Update
                    .Set(b => b.Status, BookingStatus.Cancelled)
                    .Set(b => b.CancelledAt, DateTime.UtcNow)
                    .Set(b => b.CancelledBy, cancelledBy)
                    .Set(b => b.CancellationReason, reason)
                    .Set(b => b.ModifiedAt, DateTime.UtcNow);

                var updateResult = await _mongoDBService.Bookings.UpdateOneAsync(
                    b => b.Id == bookingId,
                    updateDefinition
                );

                if (updateResult.ModifiedCount > 0)
                {
                    existingBooking.Status = BookingStatus.Cancelled;
                    existingBooking.CancelledAt = DateTime.UtcNow;
                    existingBooking.CancelledBy = cancelledBy;
                    existingBooking.CancellationReason = reason;

                    await _notificationService.SendBookingStatusUpdateAsync(bookingId, existingBooking.Status, BookingStatus.Cancelled, reason);

                    _logger.LogInformation($"Booking {bookingId} cancelled successfully by {cancelledBy}");
                    return (true, "Booking cancelled successfully");
                }

                return (false, "Failed to cancel booking");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling booking {bookingId}");
                return (false, "An error occurred while cancelling the booking");
            }
        }

        /// <summary>
        /// Approve a booking
        /// </summary>
        public async Task<(bool Success, string Message)> ApproveBookingAsync(string bookingId, string approvedBy)
        {
            try
            {
                var existingBooking = await GetBookingByIdAsync(bookingId);
                if (existingBooking == null)
                {
                    return (false, "Booking not found");
                }

                if (existingBooking.Status != BookingStatus.Pending)
                {
                    return (false, "Only pending bookings can be approved");
                }

                var updateDefinition = Builders<Booking>.Update
                    .Set(b => b.Status, BookingStatus.Approved)
                    .Set(b => b.ApprovedAt, DateTime.UtcNow)
                    .Set(b => b.ApprovedBy, approvedBy)
                    .Set(b => b.ModifiedAt, DateTime.UtcNow);

                var updateResult = await _mongoDBService.Bookings.UpdateOneAsync(
                    b => b.Id == bookingId,
                    updateDefinition
                );

                if (updateResult.ModifiedCount > 0)
                {
                    await _notificationService.SendBookingStatusUpdateAsync(bookingId, existingBooking.Status, BookingStatus.Approved, $"Booking approved by {approvedBy}");

                    _logger.LogInformation($"Booking {bookingId} approved successfully by {approvedBy}");
                    return (true, "Booking approved successfully");
                }

                return (false, "Failed to approve booking");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving booking {bookingId}");
                return (false, "An error occurred while approving the booking");
            }
        }

        /// <summary>
        /// Reject a booking with reason
        /// </summary>
        public async Task<(bool Success, string Message)> RejectBookingAsync(string bookingId, string rejectedBy, string reason)
        {
            try
            {
                var existingBooking = await GetBookingByIdAsync(bookingId);
                if (existingBooking == null)
                {
                    return (false, "Booking not found");
                }

                if (existingBooking.Status != BookingStatus.Pending)
                {
                    return (false, "Only pending bookings can be rejected");
                }

                var updateDefinition = Builders<Booking>.Update
                    .Set(b => b.Status, BookingStatus.Rejected)
                    .Set(b => b.RejectedAt, DateTime.UtcNow)
                    .Set(b => b.RejectedBy, rejectedBy)
                    .Set(b => b.RejectionReason, reason)
                    .Set(b => b.ModifiedAt, DateTime.UtcNow);

                var updateResult = await _mongoDBService.Bookings.UpdateOneAsync(
                    b => b.Id == bookingId,
                    updateDefinition
                );

                if (updateResult.ModifiedCount > 0)
                {
                    await _notificationService.SendBookingStatusUpdateAsync(bookingId, existingBooking.Status, BookingStatus.Rejected, reason);

                    _logger.LogInformation($"Booking {bookingId} rejected successfully by {rejectedBy}");
                    return (true, "Booking rejected successfully");
                }

                return (false, "Failed to reject booking");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting booking {bookingId}");
                return (false, "An error occurred while rejecting the booking");
            }
        }

        /// <summary>
        /// Complete a booking
        /// </summary>
        public async Task<(bool Success, string Message)> CompleteBookingAsync(string bookingId, string completedBy, decimal? energyConsumed = null)
        {
            try
            {
                var existingBooking = await GetBookingByIdAsync(bookingId);
                if (existingBooking == null)
                {
                    return (false, "Booking not found");
                }

                if (existingBooking.Status != BookingStatus.Approved)
                {
                    return (false, "Only approved bookings can be completed");
                }

                var updateDefinition = Builders<Booking>.Update
                    .Set(b => b.Status, BookingStatus.Completed)
                    .Set(b => b.CompletedAt, DateTime.UtcNow)
                    .Set(b => b.ModifiedAt, DateTime.UtcNow);

                if (energyConsumed.HasValue)
                {
                    updateDefinition = updateDefinition.Set(b => b.EnergyConsumedKWh, energyConsumed.Value);
                    
                    // Recalculate actual cost based on energy consumed
                    var chargingStation = await _mongoDBService.ChargingStations.Find(
                        cs => cs.Id == existingBooking.ChargingStationId
                    ).FirstOrDefaultAsync();

                    if (chargingStation != null)
                    {
                        var actualCost = energyConsumed.Value * chargingStation.PricePerKWh;
                        updateDefinition = updateDefinition.Set(b => b.TotalCost, actualCost);
                    }
                }

                var updateResult = await _mongoDBService.Bookings.UpdateOneAsync(
                    b => b.Id == bookingId,
                    updateDefinition
                );

                if (updateResult.ModifiedCount > 0)
                {
                    await _notificationService.SendBookingStatusUpdateAsync(bookingId, existingBooking.Status, BookingStatus.Completed, $"Booking completed by {completedBy}");

                    _logger.LogInformation($"Booking {bookingId} completed successfully by {completedBy}");
                    return (true, "Booking completed successfully");
                }

                return (false, "Failed to complete booking");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error completing booking {bookingId}");
                return (false, "An error occurred while completing the booking");
            }
        }

        /// <summary>
        /// Get booking history for a specific user
        /// </summary>
        public async Task<List<BookingResponseDto>> GetUserBookingHistoryAsync(string userId, int limit = 50)
        {
            try
            {
                var bookings = await _mongoDBService.Bookings.Find(b => b.UserId == userId)
                    .SortByDescending(b => b.CreatedAt)
                    .Limit(limit)
                    .ToListAsync();

                var bookingDtos = new List<BookingResponseDto>();
                foreach (var booking in bookings)
                {
                    var dto = await MapToBookingResponseDtoAsync(booking);
                    bookingDtos.Add(dto);
                }

                return bookingDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting booking history for user {userId}");
                return new List<BookingResponseDto>();
            }
        }

        /// <summary>
        /// Get bookings by date range
        /// </summary>
        public async Task<List<BookingResponseDto>> GetBookingsByDateRangeAsync(DateTime startDate, DateTime endDate, string? stationId = null)
        {
            try
            {
                var filterBuilder = Builders<Booking>.Filter;
                var filter = filterBuilder.And(
                    filterBuilder.Gte(b => b.BookingDate, startDate.Date),
                    filterBuilder.Lte(b => b.BookingDate, endDate.Date)
                );

                if (!string.IsNullOrEmpty(stationId))
                {
                    filter = filterBuilder.And(filter, filterBuilder.Eq(b => b.ChargingStationId, stationId));
                }

                var bookings = await _mongoDBService.Bookings.Find(filter)
                    .SortByDescending(b => b.CreatedAt)
                    .ToListAsync();

                var bookingDtos = new List<BookingResponseDto>();
                foreach (var booking in bookings)
                {
                    var dto = await MapToBookingResponseDtoAsync(booking);
                    bookingDtos.Add(dto);
                }

                return bookingDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting bookings by date range");
                return new List<BookingResponseDto>();
            }
        }

        /// <summary>
        /// Check for booking conflicts
        /// </summary>
        public async Task<bool> HasBookingConflictAsync(string chargingStationId, DateTime startTime, DateTime endTime, string? excludeBookingId = null)
        {
            try
            {
                var filterBuilder = Builders<Booking>.Filter;
                var filter = filterBuilder.And(
                    filterBuilder.Eq(b => b.ChargingStationId, chargingStationId),
                    filterBuilder.In(b => b.Status, new[] { BookingStatus.Pending, BookingStatus.Approved }),
                    filterBuilder.Or(
                        // New booking starts during existing booking
                        filterBuilder.And(
                            filterBuilder.Lte(b => b.StartTime, startTime),
                            filterBuilder.Gt(b => b.EndTime, startTime)
                        ),
                        // New booking ends during existing booking
                        filterBuilder.And(
                            filterBuilder.Lt(b => b.StartTime, endTime),
                            filterBuilder.Gte(b => b.EndTime, endTime)
                        ),
                        // New booking completely contains existing booking
                        filterBuilder.And(
                            filterBuilder.Gte(b => b.StartTime, startTime),
                            filterBuilder.Lte(b => b.EndTime, endTime)
                        ),
                        // Existing booking completely contains new booking
                        filterBuilder.And(
                            filterBuilder.Lte(b => b.StartTime, startTime),
                            filterBuilder.Gte(b => b.EndTime, endTime)
                        )
                    )
                );

                if (!string.IsNullOrEmpty(excludeBookingId))
                {
                    filter = filterBuilder.And(filter, filterBuilder.Ne(b => b.Id, excludeBookingId));
                }

                var conflictingBooking = await _mongoDBService.Bookings.Find(filter).FirstOrDefaultAsync();
                return conflictingBooking != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking booking conflicts");
                return true; // Return true to be safe in case of error
            }
        }

        #region Private Methods

        /// <summary>
        /// Validate booking business rules
        /// </summary>
        private async Task<(bool IsValid, string Message)> ValidateBookingRulesAsync(CreateBookingDto createBookingDto)
        {
            // Check 7-day booking window
            if (createBookingDto.BookingDate < DateTime.UtcNow.Date)
            {
                return (false, "Cannot book for past dates");
            }

            if (createBookingDto.BookingDate > DateTime.UtcNow.Date.AddDays(7))
            {
                return (false, "Booking is only allowed within 7 days from today");
            }

            // Check start and end time logic
            if (createBookingDto.StartTime >= createBookingDto.EndTime)
            {
                return (false, "End time must be after start time");
            }

            // Check if start time is in the future
            if (createBookingDto.StartTime <= DateTime.UtcNow)
            {
                return (false, "Start time must be in the future");
            }

            // Check time slot availability
            var timeSlotCheck = new TimeSlotCheckDto
            {
                ChargingStationId = createBookingDto.ChargingStationId,
                Date = createBookingDto.BookingDate,
                StartTime = createBookingDto.StartTime,
                EndTime = createBookingDto.EndTime
            };

            var availability = await CheckTimeSlotAvailabilityAsync(timeSlotCheck);
            if (!availability.IsAvailable)
            {
                return (false, availability.Message);
            }

            // Check if charging station exists and is available
            var chargingStation = await _mongoDBService.ChargingStations
                .Find(cs => cs.Id == createBookingDto.ChargingStationId)
                .FirstOrDefaultAsync();

            if (chargingStation == null)
            {
                return (false, "Charging station not found");
            }

            if (!chargingStation.IsBookingAvailable)
            {
                return (false, "Charging station is not available for booking");
            }

            return (true, "Validation passed");
        }

        /// <summary>
        /// Map Booking entity to BookingResponseDto with related data
        /// </summary>
        private async Task<BookingResponseDto> MapToBookingResponseDtoAsync(Booking booking)
        {
            var dto = new BookingResponseDto
            {
                Id = booking.Id ?? "",
                BookingNumber = booking.BookingNumber,
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
                DurationMinutes = booking.DurationMinutes
            };

            // Load user information
            try
            {
                var user = await _mongoDBService.EVOwners
                    .Find(u => u.Id == booking.UserId)
                    .FirstOrDefaultAsync();

                if (user != null)
                {
                    dto.User = new UserResponseDto
                    {
                        Id = user.Id ?? "",
                        NIC = user.NIC,
                        FullName = user.FullName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        Address = user.Address,
                        IsActive = user.IsActive,
                        IsApproved = user.IsApproved
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load user for booking {BookingId}", booking.Id);
            }

            // Load charging station information
            try
            {
                var station = await _mongoDBService.ChargingStations
                    .Find(cs => cs.Id == booking.ChargingStationId)
                    .FirstOrDefaultAsync();

                if (station != null)
                {
                    dto.ChargingStation = new ChargingStationResponseDto
                    {
                        Id = station.Id ?? "",
                        StationName = station.StationName,
                        Location = station.Location,
                        Address = station.Address,
                        ConnectorType = station.ConnectorType.ToString(),
                        PowerRatingKW = station.PowerRatingKW,
                        PricePerKWh = station.PricePerKWh,
                        Status = station.Status.ToString(),
                        Description = station.Description,
                        Amenities = station.Amenities,
                        OperatingHours = station.OperatingHours,
                        IsAvailable = station.IsAvailable,
                        MaxBookingDurationMinutes = station.MaxBookingDurationMinutes,
                        Coordinates = new CoordinatesDto
                        {
                            Latitude = station.Latitude ?? 0,
                            Longitude = station.Longitude ?? 0
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load charging station for booking {BookingId}", booking.Id);
            }

            return dto;
        }

        /// <summary>
        /// Legacy method - kept for compatibility but should use async version
        /// </summary>
        private BookingResponseDto MapToBookingResponseDto(Booking booking)
        {
            return new BookingResponseDto
            {
                Id = booking.Id ?? "",
                BookingNumber = booking.BookingNumber,
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

        #endregion
    }
}