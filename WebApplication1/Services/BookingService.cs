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

                // ✅ NEW: Check if slots are available for the requested time range
                var (slotsAvailable, availableSlots) = await CheckSlotAvailabilityAsync(
                    createBookingDto.ChargingStationId,
                    createBookingDto.StartTime,
                    createBookingDto.EndTime
                );

                if (!slotsAvailable)
                {
                    return (false, $"No available slots at this station for the selected time. Available slots: {availableSlots}", null);
                }

                // Check for booking conflicts (additional validation)
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
                _logger.LogError(ex, "Error creating booking. Details: {Message}. StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                _logger.LogError("BookingDto: UserId={UserId}, StationId={StationId}, Date={Date}, StartTime={StartTime}, EndTime={EndTime}", 
                    createBookingDto.UserId, createBookingDto.ChargingStationId, createBookingDto.BookingDate, 
                    createBookingDto.StartTime, createBookingDto.EndTime);
                return (false, $"An error occurred while creating the booking: {ex.Message}", null);
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

                // Re-validate slot availability before approval to prevent race conditions
                var (slotsAvailable, availableSlots) = await CheckSlotAvailabilityAsync(
                    existingBooking.ChargingStationId,
                    existingBooking.StartTime,
                    existingBooking.EndTime
                );

                if (!slotsAvailable)
                {
                    _logger.LogWarning($"Cannot approve booking {bookingId}: No available slots. Available: {availableSlots}");
                    return (false, $"Cannot approve booking: No available slots for the selected time range. Available slots: {availableSlots}");
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

        /// <summary>
        /// Check if there are active bookings for the charging station
        /// </summary>
        public async Task<bool> HasActiveBookingsAsync(string chargingStationId)
        {
            // Query for bookings at this station with status ACTIVE or CONFIRMED
            var filter = Builders<Booking>.Filter.Eq(b => b.ChargingStationId, chargingStationId) &
                         Builders<Booking>.Filter.In(b => b.Status, new[] { BookingStatus.Pending, BookingStatus.Approved });

            var count = await _mongoDBService.Bookings.CountDocumentsAsync(filter);
            return count > 0;
        }

        /// <summary>
        /// Get available slots for a charging station at a specific time range (public API method)
        /// </summary>
        public async Task<(int AvailableSlots, int TotalSlots, int OccupiedSlots)> GetAvailableSlotsAsync(
            string chargingStationId,
            DateTime startTime,
            DateTime endTime)
        {
            try
            {
                // Get the charging station
                var station = await _mongoDBService.ChargingStations
                    .Find(cs => cs.Id == chargingStationId)
                    .FirstOrDefaultAsync();

                if (station == null)
                {
                    _logger.LogWarning($"Station {chargingStationId} not found");
                    return (0, 0, 0);
                }

                // Get all active bookings that overlap with the requested time
                // Only Pending and Approved bookings occupy slots
                var overlappingBookingsFilter = Builders<Booking>.Filter.And(
                    Builders<Booking>.Filter.Eq(b => b.ChargingStationId, chargingStationId),
                    Builders<Booking>.Filter.In(b => b.Status, new[] {
                        BookingStatus.Pending,
                        BookingStatus.Approved
                    }),
                    Builders<Booking>.Filter.And(
                        Builders<Booking>.Filter.Lt(b => b.StartTime, endTime),
                        Builders<Booking>.Filter.Gt(b => b.EndTime, startTime)
                    )
                );

                var overlappingBookings = await _mongoDBService.Bookings
                    .Find(overlappingBookingsFilter)
                    .ToListAsync();

                int occupiedSlots = overlappingBookings.Count;
                int availableSlots = station.TotalSlots - occupiedSlots;

                return (availableSlots, station.TotalSlots, occupiedSlots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting available slots for station {chargingStationId}");
                return (0, 0, 0);
            }
        }

        #region Private Methods

        /// <summary>
        /// Check if slots are available for the requested time range (time-based slot management)
        /// </summary>
        private async Task<(bool Available, int AvailableSlots)> CheckSlotAvailabilityAsync(
            string chargingStationId,
            DateTime startTime,
            DateTime endTime)
        {
            return await CheckSlotAvailabilityAsync(chargingStationId, startTime, endTime, null);
        }

        private async Task<(bool Available, int AvailableSlots)> CheckSlotAvailabilityAsync(
            string chargingStationId,
            DateTime startTime,
            DateTime endTime,
            string? excludeBookingId)
        {
            try
            {
                // Get the charging station
                var station = await _mongoDBService.ChargingStations
                    .Find(cs => cs.Id == chargingStationId)
                    .FirstOrDefaultAsync();

                if (station == null)
                {
                    _logger.LogWarning($"Station {chargingStationId} not found for slot availability check");
                    return (false, 0);
                }

                // Build filter for overlapping bookings
                var filterBuilder = Builders<Booking>.Filter;
                var filters = new List<FilterDefinition<Booking>>
                {
                    filterBuilder.Eq(b => b.ChargingStationId, chargingStationId),
                    filterBuilder.In(b => b.Status, new[] { BookingStatus.Pending, BookingStatus.Approved }),
                    filterBuilder.Lt(b => b.StartTime, endTime),
                    filterBuilder.Gt(b => b.EndTime, startTime)
                };

                // Exclude specific booking if provided (for modification scenarios)
                if (!string.IsNullOrEmpty(excludeBookingId))
                {
                    filters.Add(filterBuilder.Ne(b => b.Id, excludeBookingId));
                }

                var overlappingBookingsFilter = filterBuilder.And(filters);
                
                // Get all active bookings (not cancelled or completed) that overlap with the requested time
                // Only Pending and Approved bookings occupy slots);

                var overlappingBookings = await _mongoDBService.Bookings
                    .Find(overlappingBookingsFilter)
                    .ToListAsync();

                int occupiedSlots = overlappingBookings.Count;
                int availableSlots = station.TotalSlots - occupiedSlots;

                _logger.LogInformation(
                    $"Slot availability check for station {chargingStationId} ({station.StationName}): " +
                    $"Total={station.TotalSlots}, Occupied={occupiedSlots}, Available={availableSlots}, " +
                    $"TimeRange={startTime:yyyy-MM-dd HH:mm} to {endTime:yyyy-MM-dd HH:mm}");

                return (availableSlots > 0, availableSlots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking slot availability for station {chargingStationId}");
                return (false, 0);
            }
        }

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
                    .Find(u => u.NIC == booking.UserId)
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

        #region Booking Modification Methods

        /// <summary>
        /// Customer requests a booking modification (requires admin approval)
        /// </summary>
        public async Task<(bool Success, string Message, string? ModificationRequestId)> RequestBookingModificationAsync(
            string bookingId, 
            RequestBookingModificationDto modificationDto, 
            string requestedBy)
        {
            try
            {
                var booking = await GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    return (false, "Booking not found", null);
                }

                // Validate booking can be modified
                if (!booking.CanBeModified)
                {
                    return (false, "This booking cannot be modified. Must be Pending status and at least 12 hours before start time.", null);
                }

                // Check if there's already a pending modification request
                if (booking.HasPendingModification)
                {
                    return (false, "There is already a pending modification request for this booking.", null);
                }

                // Validate requested changes
                if (modificationDto.StartTime.HasValue && modificationDto.EndTime.HasValue)
                {
                    if (modificationDto.EndTime.Value <= modificationDto.StartTime.Value)
                    {
                        return (false, "End time must be after start time.", null);
                    }

                    var duration = (modificationDto.EndTime.Value - modificationDto.StartTime.Value).TotalMinutes;
                    if (duration > 1440) // 24 hours
                    {
                        return (false, "Booking duration cannot exceed 24 hours.", null);
                    }

                    // Check 7-day booking window
                    if (modificationDto.BookingDate.HasValue)
                    {
                        var daysDifference = (modificationDto.BookingDate.Value.Date - DateTime.UtcNow.Date).TotalDays;
                        if (daysDifference < 0 || daysDifference > 7)
                        {
                            return (false, "Bookings must be made within a 7-day window.", null);
                        }
                    }
                }

                // If station is being changed, verify slot availability
                if (!string.IsNullOrEmpty(modificationDto.ChargingStationId) && 
                    modificationDto.ChargingStationId != booking.ChargingStationId &&
                    modificationDto.StartTime.HasValue && 
                    modificationDto.EndTime.HasValue)
                {
                    var (slotsAvailable, _) = await CheckSlotAvailabilityAsync(
                        modificationDto.ChargingStationId,
                        modificationDto.StartTime.Value,
                        modificationDto.EndTime.Value,
                        bookingId // Exclude current booking
                    );

                    if (!slotsAvailable)
                    {
                        return (false, "No available slots at the requested station for the selected time.", null);
                    }

                    // Check for conflicts
                    var hasConflict = await HasBookingConflictAsync(
                        modificationDto.ChargingStationId,
                        modificationDto.StartTime.Value,
                        modificationDto.EndTime.Value,
                        bookingId
                    );

                    if (hasConflict)
                    {
                        return (false, "The requested time slot conflicts with an existing booking.", null);
                    }
                }

                // Create modification request
                var modificationRequest = new BookingModificationRequest
                {
                    BookingId = bookingId,
                    RequestedBy = requestedBy,
                    RequestedAt = DateTime.UtcNow,
                    Status = ModificationRequestStatus.Pending,

                    // Original values
                    OriginalChargingStationId = booking.ChargingStationId,
                    OriginalStartTime = booking.StartTime,
                    OriginalEndTime = booking.EndTime,
                    OriginalVehicleNumber = booking.VehicleNumber,

                    // Requested values
                    RequestedChargingStationId = modificationDto.ChargingStationId,
                    RequestedBookingDate = modificationDto.BookingDate,
                    RequestedStartTime = modificationDto.StartTime,
                    RequestedEndTime = modificationDto.EndTime,
                    RequestedVehicleNumber = modificationDto.VehicleNumber,
                    RequestedVehicleType = modificationDto.VehicleType,
                    RequestedNotes = modificationDto.Notes,
                    RequestReason = modificationDto.RequestReason
                };

                await _mongoDBService.ModificationRequests.InsertOneAsync(modificationRequest);

                // Update booking to mark pending modification
                var updateDefinition = Builders<Booking>.Update
                    .Set(b => b.HasPendingModification, true)
                    .Set(b => b.ModificationRequestId, modificationRequest.Id)
                    .Set(b => b.ModificationRequestedAt, DateTime.UtcNow)
                    .Set(b => b.ModificationRequestedBy, requestedBy);

                await _mongoDBService.Bookings.UpdateOneAsync(
                    b => b.Id == bookingId,
                    updateDefinition
                );

                // Send notification to admins
                await _notificationService.SendModificationRequestedAsync(bookingId, modificationRequest.Id!, requestedBy);

                _logger.LogInformation($"Modification request created for booking {bookingId}");
                return (true, "Modification request submitted successfully. Awaiting admin approval.", modificationRequest.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error requesting modification for booking {bookingId}");
                return (false, "An error occurred while submitting the modification request.", null);
            }
        }

        /// <summary>
        /// Admin approves or rejects a modification request
        /// </summary>
        public async Task<(bool Success, string Message)> ReviewModificationRequestAsync(
            string modificationRequestId,
            ReviewModificationRequestDto reviewDto)
        {
            try
            {
                var modRequest = await _mongoDBService.ModificationRequests
                    .Find(mr => mr.Id == modificationRequestId)
                    .FirstOrDefaultAsync();

                if (modRequest == null)
                {
                    return (false, "Modification request not found");
                }

                if (!modRequest.CanBeProcessed)
                {
                    return (false, $"Modification request cannot be processed. Current status: {modRequest.Status}");
                }

                var booking = await GetBookingByIdAsync(modRequest.BookingId);
                if (booking == null)
                {
                    return (false, "Associated booking not found");
                }

                if (reviewDto.IsApproved)
                {
                    // Re-validate before applying changes
                    if (modRequest.RequestedStartTime.HasValue && modRequest.RequestedEndTime.HasValue)
                    {
                        var targetStationId = modRequest.RequestedChargingStationId ?? booking.ChargingStationId;
                        
                        var (slotsAvailable, _) = await CheckSlotAvailabilityAsync(
                            targetStationId,
                            modRequest.RequestedStartTime.Value,
                            modRequest.RequestedEndTime.Value,
                            booking.Id
                        );

                        if (!slotsAvailable)
                        {
                            return (false, "Requested time slot is no longer available.");
                        }

                        var hasConflict = await HasBookingConflictAsync(
                            targetStationId,
                            modRequest.RequestedStartTime.Value,
                            modRequest.RequestedEndTime.Value,
                            booking.Id
                        );

                        if (hasConflict)
                        {
                            return (false, "Requested time slot now has a conflict.");
                        }
                    }

                    // Apply modifications to booking
                    var updateBuilder = Builders<Booking>.Update
                        .Set(b => b.HasPendingModification, false)
                        .Set(b => b.ModificationRequestId, null)
                        .Set(b => b.ModifiedAt, DateTime.UtcNow)
                        .Set(b => b.LastModifiedBy, reviewDto.ReviewedBy);

                    if (!string.IsNullOrEmpty(modRequest.RequestedChargingStationId))
                    {
                        updateBuilder = updateBuilder.Set(b => b.ChargingStationId, modRequest.RequestedChargingStationId);
                    }

                    if (modRequest.RequestedBookingDate.HasValue)
                    {
                        updateBuilder = updateBuilder.Set(b => b.BookingDate, modRequest.RequestedBookingDate.Value);
                    }

                    if (modRequest.RequestedStartTime.HasValue)
                    {
                        updateBuilder = updateBuilder.Set(b => b.StartTime, modRequest.RequestedStartTime.Value);
                    }

                    if (modRequest.RequestedEndTime.HasValue)
                    {
                        updateBuilder = updateBuilder.Set(b => b.EndTime, modRequest.RequestedEndTime.Value);
                    }

                    if (!string.IsNullOrEmpty(modRequest.RequestedVehicleNumber))
                    {
                        updateBuilder = updateBuilder.Set(b => b.VehicleNumber, modRequest.RequestedVehicleNumber);
                    }

                    if (!string.IsNullOrEmpty(modRequest.RequestedVehicleType))
                    {
                        updateBuilder = updateBuilder.Set(b => b.VehicleType, modRequest.RequestedVehicleType);
                    }

                    if (!string.IsNullOrEmpty(modRequest.RequestedNotes))
                    {
                        updateBuilder = updateBuilder.Set(b => b.Notes, modRequest.RequestedNotes);
                    }

                    // Add to modification history
                    var historyEntry = new BookingModificationHistory
                    {
                        ModifiedAt = DateTime.UtcNow,
                        ModifiedBy = reviewDto.ReviewedBy,
                        ChangeType = "ModificationApproved",
                        ChangeDescription = $"Modification request approved: {modRequest.ChangesSummary}",
                        Changes = new Dictionary<string, string>
                        {
                            { "Reason", modRequest.RequestReason },
                            { "ReviewNotes", reviewDto.ReviewNotes }
                        }
                    };

                    updateBuilder = updateBuilder.Push(b => b.ModificationHistory, historyEntry);

                    await _mongoDBService.Bookings.UpdateOneAsync(
                        b => b.Id == modRequest.BookingId,
                        updateBuilder
                    );

                    // Update modification request
                    var mrUpdate = Builders<BookingModificationRequest>.Update
                        .Set(mr => mr.Status, ModificationRequestStatus.Approved)
                        .Set(mr => mr.ReviewedBy, reviewDto.ReviewedBy)
                        .Set(mr => mr.ReviewedAt, DateTime.UtcNow)
                        .Set(mr => mr.ReviewNotes, reviewDto.ReviewNotes);

                    await _mongoDBService.ModificationRequests.UpdateOneAsync(
                        mr => mr.Id == modificationRequestId,
                        mrUpdate
                    );

                    // Send notification to customer
                    await _notificationService.SendModificationApprovedAsync(modRequest.BookingId, modRequest.RequestedBy);

                    _logger.LogInformation($"Modification request {modificationRequestId} approved for booking {modRequest.BookingId}");
                    return (true, "Modification request approved and applied successfully.");
                }
                else
                {
                    // Reject the modification
                    var updateDefinition = Builders<Booking>.Update
                        .Set(b => b.HasPendingModification, false)
                        .Set(b => b.ModificationRequestId, null);

                    await _mongoDBService.Bookings.UpdateOneAsync(
                        b => b.Id == modRequest.BookingId,
                        updateDefinition
                    );

                    var mrUpdate = Builders<BookingModificationRequest>.Update
                        .Set(mr => mr.Status, ModificationRequestStatus.Rejected)
                        .Set(mr => mr.ReviewedBy, reviewDto.ReviewedBy)
                        .Set(mr => mr.ReviewedAt, DateTime.UtcNow)
                        .Set(mr => mr.ReviewNotes, reviewDto.ReviewNotes)
                        .Set(mr => mr.RejectionReason, reviewDto.RejectionReason);

                    await _mongoDBService.ModificationRequests.UpdateOneAsync(
                        mr => mr.Id == modificationRequestId,
                        mrUpdate
                    );

                    // Send notification to customer
                    await _notificationService.SendModificationRejectedAsync(modRequest.BookingId, modRequest.RequestedBy, reviewDto.RejectionReason);

                    _logger.LogInformation($"Modification request {modificationRequestId} rejected for booking {modRequest.BookingId}");
                    return (true, "Modification request rejected.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reviewing modification request {modificationRequestId}");
                return (false, "An error occurred while reviewing the modification request.");
            }
        }

        /// <summary>
        /// Admin directly updates booking (no approval needed)
        /// </summary>
        public async Task<(bool Success, string Message, Booking? Booking)> AdminUpdateBookingAsync(
            string bookingId,
            AdminUpdateBookingDto updateDto)
        {
            try
            {
                var booking = await GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    return (false, "Booking not found", null);
                }

                // Validate booking status - only allow updates for Pending and Approved bookings
                if (booking.Status == BookingStatus.Completed || booking.Status == BookingStatus.Cancelled)
                {
                    return (false, $"Cannot update {booking.Status.ToString().ToLower()} bookings.", null);
                }

                // Validate changes with real-time verification
                if (updateDto.StartTime.HasValue && updateDto.EndTime.HasValue)
                {
                    if (updateDto.EndTime.Value <= updateDto.StartTime.Value)
                    {
                        return (false, "End time must be after start time.", null);
                    }

                    var duration = (updateDto.EndTime.Value - updateDto.StartTime.Value).TotalMinutes;
                    if (duration > 1440)
                    {
                        return (false, "Booking duration cannot exceed 24 hours.", null);
                    }

                    if (updateDto.BookingDate.HasValue)
                    {
                        var daysDifference = (updateDto.BookingDate.Value.Date - DateTime.UtcNow.Date).TotalDays;
                        if (daysDifference < 0 || daysDifference > 7)
                        {
                            return (false, "Bookings must be within a 7-day window.", null);
                        }
                    }

                    var targetStationId = updateDto.ChargingStationId ?? booking.ChargingStationId;

                    // Check slot availability
                    var (slotsAvailable, _) = await CheckSlotAvailabilityAsync(
                        targetStationId,
                        updateDto.StartTime.Value,
                        updateDto.EndTime.Value,
                        bookingId
                    );

                    if (!slotsAvailable)
                    {
                        return (false, "No available slots for the selected time.", null);
                    }

                    // Check for conflicts
                    var hasConflict = await HasBookingConflictAsync(
                        targetStationId,
                        updateDto.StartTime.Value,
                        updateDto.EndTime.Value,
                        bookingId
                    );

                    if (hasConflict)
                    {
                        return (false, "Time slot conflicts with an existing booking.", null);
                    }
                }

                // Build update definition
                var updateBuilder = Builders<Booking>.Update
                    .Set(b => b.ModifiedAt, DateTime.UtcNow)
                    .Set(b => b.LastModifiedBy, updateDto.UpdatedBy);

                var changes = new Dictionary<string, string>();

                if (!string.IsNullOrEmpty(updateDto.ChargingStationId) && updateDto.ChargingStationId != booking.ChargingStationId)
                {
                    updateBuilder = updateBuilder.Set(b => b.ChargingStationId, updateDto.ChargingStationId);
                    changes["ChargingStation"] = $"{booking.ChargingStationId} → {updateDto.ChargingStationId}";
                }

                if (updateDto.BookingDate.HasValue && updateDto.BookingDate != booking.BookingDate)
                {
                    updateBuilder = updateBuilder.Set(b => b.BookingDate, updateDto.BookingDate.Value);
                    changes["BookingDate"] = $"{booking.BookingDate:yyyy-MM-dd} → {updateDto.BookingDate:yyyy-MM-dd}";
                }

                if (updateDto.StartTime.HasValue && updateDto.StartTime != booking.StartTime)
                {
                    updateBuilder = updateBuilder.Set(b => b.StartTime, updateDto.StartTime.Value);
                    changes["StartTime"] = $"{booking.StartTime:HH:mm} → {updateDto.StartTime:HH:mm}";
                }

                if (updateDto.EndTime.HasValue && updateDto.EndTime != booking.EndTime)
                {
                    updateBuilder = updateBuilder.Set(b => b.EndTime, updateDto.EndTime.Value);
                    changes["EndTime"] = $"{booking.EndTime:HH:mm} → {updateDto.EndTime:HH:mm}";
                }

                if (!string.IsNullOrEmpty(updateDto.VehicleNumber) && updateDto.VehicleNumber != booking.VehicleNumber)
                {
                    updateBuilder = updateBuilder.Set(b => b.VehicleNumber, updateDto.VehicleNumber);
                    changes["VehicleNumber"] = $"{booking.VehicleNumber} → {updateDto.VehicleNumber}";
                }

                if (!string.IsNullOrEmpty(updateDto.VehicleType) && updateDto.VehicleType != booking.VehicleType)
                {
                    updateBuilder = updateBuilder.Set(b => b.VehicleType, updateDto.VehicleType);
                    changes["VehicleType"] = $"{booking.VehicleType} → {updateDto.VehicleType}";
                }

                if (updateDto.EstimatedChargingTimeMinutes.HasValue && updateDto.EstimatedChargingTimeMinutes != booking.EstimatedChargingTimeMinutes)
                {
                    updateBuilder = updateBuilder.Set(b => b.EstimatedChargingTimeMinutes, updateDto.EstimatedChargingTimeMinutes.Value);
                    changes["EstimatedTime"] = $"{booking.EstimatedChargingTimeMinutes} → {updateDto.EstimatedChargingTimeMinutes}";
                }

                if (!string.IsNullOrEmpty(updateDto.Notes))
                {
                    var adminNote = $"[Admin Update by {updateDto.UpdatedBy}] {updateDto.Notes}";
                    var updatedNotes = string.IsNullOrWhiteSpace(booking.Notes) 
                        ? adminNote 
                        : $"{booking.Notes}\n{adminNote}";
                    updateBuilder = updateBuilder.Set(b => b.Notes, updatedNotes);
                }

                // Add to modification history
                var historyEntry = new BookingModificationHistory
                {
                    ModifiedAt = DateTime.UtcNow,
                    ModifiedBy = updateDto.UpdatedBy,
                    ChangeType = "AdminUpdate",
                    ChangeDescription = $"Admin direct update: {updateDto.UpdateReason}",
                    Changes = changes
                };

                updateBuilder = updateBuilder.Push(b => b.ModificationHistory, historyEntry);

                var updateResult = await _mongoDBService.Bookings.UpdateOneAsync(
                    b => b.Id == bookingId,
                    updateBuilder
                );

                if (updateResult.ModifiedCount == 0)
                {
                    _logger.LogWarning($"Update operation for booking {bookingId} reported 0 modified documents");
                }

                // Force a fresh read from database to ensure we get the updated data
                var updatedBooking = await _mongoDBService.Bookings
                    .Find(b => b.Id == bookingId)
                    .FirstOrDefaultAsync();

                if (updatedBooking == null)
                {
                    _logger.LogError($"Failed to retrieve booking {bookingId} after update");
                    return (false, "Failed to retrieve updated booking.", null);
                }

                // Send notification to customer
                await _notificationService.SendAdminUpdatedBookingAsync(bookingId, booking.UserId, updateDto.UpdateReason);

                // Send notification to station operator
                try
                {
                    var stationId = updateDto.ChargingStationId ?? booking.ChargingStationId;
                    var station = await _mongoDBService.ChargingStations
                        .Find(s => s.Id == stationId)
                        .FirstOrDefaultAsync();

                    if (station != null && !string.IsNullOrEmpty(station.OperatorId))
                    {
                        var operatorMessage = $"Booking {booking.BookingNumber} at your station '{station.StationName}' has been updated by admin. Reason: {updateDto.UpdateReason}";
                        await _notificationService.SendStationOperatorNotificationAsync(
                            station.OperatorId, 
                            bookingId, 
                            operatorMessage, 
                            "BookingUpdated"
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to notify station operator for booking {bookingId} update");
                }

                _logger.LogInformation($"Admin {updateDto.UpdatedBy} updated booking {bookingId}");
                return (true, "Booking updated successfully by admin.", updatedBooking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in admin update for booking {bookingId}");
                return (false, "An error occurred while updating the booking.", null);
            }
        }

        /// <summary>
        /// Admin deletes a booking
        /// </summary>
        public async Task<(bool Success, string Message)> AdminDeleteBookingAsync(
            string bookingId,
            AdminDeleteBookingDto deleteDto)
        {
            try
            {
                var booking = await GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    return (false, "Booking not found");
                }

                // Use cancellation for soft delete
                var updateDefinition = Builders<Booking>.Update
                    .Set(b => b.Status, BookingStatus.Cancelled)
                    .Set(b => b.CancelledAt, DateTime.UtcNow)
                    .Set(b => b.CancelledBy, deleteDto.DeletedBy)
                    .Set(b => b.CancellationReason, $"[ADMIN DELETED] {deleteDto.DeletionReason}")
                    .Set(b => b.ModifiedAt, DateTime.UtcNow)
                    .Set(b => b.QRCode, string.Empty); // Clear QR code

                await _mongoDBService.Bookings.UpdateOneAsync(
                    b => b.Id == bookingId,
                    updateDefinition
                );

                if (deleteDto.NotifyCustomer)
                {
                    await _notificationService.SendAdminDeletedBookingAsync(bookingId, booking.UserId, deleteDto.DeletionReason);
                }

                _logger.LogInformation($"Admin {deleteDto.DeletedBy} deleted booking {bookingId}");
                return (true, "Booking deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting booking {bookingId}");
                return (false, "An error occurred while deleting the booking.");
            }
        }

        /// <summary>
        /// Get all pending modification requests
        /// </summary>
        public async Task<List<ModificationRequestResponseDto>> GetPendingModificationRequestsAsync()
        {
            try
            {
                var pendingRequests = await _mongoDBService.ModificationRequests
                    .Find(mr => mr.Status == ModificationRequestStatus.Pending)
                    .SortByDescending(mr => mr.RequestedAt)
                    .ToListAsync();

                var responseDtos = new List<ModificationRequestResponseDto>();

                foreach (var request in pendingRequests)
                {
                    var booking = await GetBookingByIdAsync(request.BookingId);
                    var user = booking != null ? await _mongoDBService.EVOwners.Find(u => u.Id == booking.UserId).FirstOrDefaultAsync() : null;

                    var dto = new ModificationRequestResponseDto
                    {
                        Id = request.Id ?? "",
                        BookingId = request.BookingId,
                        RequestedBy = request.RequestedBy,
                        RequestedAt = request.RequestedAt,
                        Status = request.Status.ToString(),

                        OriginalChargingStationId = request.OriginalChargingStationId,
                        OriginalStartTime = request.OriginalStartTime,
                        OriginalEndTime = request.OriginalEndTime,
                        OriginalVehicleNumber = request.OriginalVehicleNumber,

                        RequestedChargingStationId = request.RequestedChargingStationId,
                        RequestedBookingDate = request.RequestedBookingDate,
                        RequestedStartTime = request.RequestedStartTime,
                        RequestedEndTime = request.RequestedEndTime,
                        RequestedVehicleNumber = request.RequestedVehicleNumber,
                        RequestedVehicleType = request.RequestedVehicleType,
                        RequestedNotes = request.RequestedNotes,
                        RequestReason = request.RequestReason,

                        ReviewedBy = request.ReviewedBy,
                        ReviewedAt = request.ReviewedAt,
                        ReviewNotes = request.ReviewNotes,
                        RejectionReason = request.RejectionReason,

                        BookingNumber = booking?.BookingNumber ?? "",
                        ChangesSummary = request.ChangesSummary
                    };

                    if (user != null)
                    {
                        dto.Customer = new UserResponseDto
                        {
                            Id = user.Id ?? "",
                            NIC = user.NIC,
                            FullName = user.FullName,
                            Email = user.Email,
                            PhoneNumber = user.PhoneNumber
                        };
                    }

                    // Get station names
                    if (!string.IsNullOrEmpty(request.OriginalChargingStationId))
                    {
                        var originalStation = await _mongoDBService.ChargingStations
                            .Find(cs => cs.Id == request.OriginalChargingStationId)
                            .FirstOrDefaultAsync();
                        dto.OriginalStationName = originalStation?.StationName;
                    }

                    if (!string.IsNullOrEmpty(request.RequestedChargingStationId))
                    {
                        var requestedStation = await _mongoDBService.ChargingStations
                            .Find(cs => cs.Id == request.RequestedChargingStationId)
                            .FirstOrDefaultAsync();
                        dto.RequestedStationName = requestedStation?.StationName;
                    }

                    responseDtos.Add(dto);
                }

                return responseDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending modification requests");
                return new List<ModificationRequestResponseDto>();
            }
        }

        #endregion
    }
}