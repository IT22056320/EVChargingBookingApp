/*
 * File: Booki    [AuthorizeAttribute]
    public class BookingManagementController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ISignalRService _signalRService;
        private readonly ILogger<BookingManagementController> _logger;

        public BookingManagementController(
            ApiService apiService, 
            ISignalRService signalRService,
            ILogger<BookingManagementController> logger)
        {
            _apiService = apiService;
            _signalRService = signalRService;
            _logger = logger;
        }tController.cs
 * Description: MVC Controller for booking management interface (Frontend Web Application)
 * Author: EV Charging Team
 * Date: September 24, 2025
 */

using Microsoft.AspNetCore.Mvc;
using WebApplicationFrontend.Services;
using WebApplicationFrontend.Filters;
using WebApplicationFrontend.Models;
using System.Text.Json;

namespace WebApplicationFrontend.Controllers
{
    /// <summary>
    /// MVC Controller for booking management operations
    /// </summary>
    [AuthorizeAttribute]
    public class BookingManagementController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ILogger<BookingManagementController> _logger;

        public BookingManagementController(ApiService apiService, ILogger<BookingManagementController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        /// <summary>
        /// Main booking management dashboard
        /// </summary>
        public async Task<IActionResult> Index(
            string? status = null,
            string? searchTerm = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var queryParams = new Dictionary<string, string?>
                {
                    ["page"] = page.ToString(),
                    ["pageSize"] = pageSize.ToString(),
                    ["sortBy"] = "CreatedAt",
                    ["sortDescending"] = "true"
                };

                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    queryParams["status"] = status;
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    queryParams["vehicleNumber"] = searchTerm;
                    queryParams["userNIC"] = searchTerm;
                    queryParams["userName"] = searchTerm;
                }

                if (fromDate.HasValue)
                {
                    queryParams["createdFrom"] = fromDate.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
                }

                if (toDate.HasValue)
                {
                    queryParams["createdTo"] = toDate.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
                }

                var bookingsResponse = await _apiService.GetAsync<BookingPagedResponse>("api/bookings", queryParams);

                var viewModel = new BookingManagementViewModel
                {
                    Bookings = bookingsResponse?.Bookings ?? new List<BookingResponse>(),
                    TotalCount = bookingsResponse?.TotalCount ?? 0,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = bookingsResponse?.TotalPages ?? 1,
                    HasNextPage = bookingsResponse?.HasNextPage ?? false,
                    HasPreviousPage = bookingsResponse?.HasPreviousPage ?? false,
                    CurrentStatus = status ?? "all",
                    SearchTerm = searchTerm ?? "",
                    FromDate = fromDate,
                    ToDate = toDate
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading booking management dashboard");
                TempData["ErrorMessage"] = "Failed to load bookings. Please try again.";
                return View(new BookingManagementViewModel());
            }
        }

        /// <summary>
        /// View booking details
        /// </summary>
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest("Booking ID is required");
                }

                var booking = await _apiService.GetAsync<BookingResponse>($"api/bookings/{id}");

                if (booking == null)
                {
                    TempData["ErrorMessage"] = "Booking not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading booking details for ID: {id}");
                TempData["ErrorMessage"] = "Failed to load booking details.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Approve booking action
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Approve(string id, string approvedBy, string? reason = null)
        {
            try
            {
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(approvedBy))
                {
                    return Json(new { success = false, message = "Invalid parameters" });
                }

                var approvalData = new
                {
                    isApproved = true,
                    approvedBy = approvedBy,
                    reason = reason
                };

                var response = await _apiService.PostAsync<object>($"api/bookings/{id}/approve", approvalData);

                if (response != null)
                {
                    return Json(new { success = true, message = "Booking approved successfully" });
                }

                return Json(new { success = false, message = "Failed to approve booking" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving booking {id}");
                return Json(new { success = false, message = "An error occurred while approving the booking" });
            }
        }

        /// <summary>
        /// Reject booking action
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Reject(string id, string rejectedBy, string reason)
        {
            try
            {
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(rejectedBy) || string.IsNullOrEmpty(reason))
                {
                    return Json(new { success = false, message = "Invalid parameters. Reason is required for rejection." });
                }

                var rejectionData = new
                {
                    isApproved = false,
                    approvedBy = rejectedBy,
                    reason = reason
                };

                var response = await _apiService.PostAsync<object>($"api/bookings/{id}/approve", rejectionData);

                if (response != null)
                {
                    return Json(new { success = true, message = "Booking rejected successfully" });
                }

                return Json(new { success = false, message = "Failed to reject booking" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting booking {id}");
                return Json(new { success = false, message = "An error occurred while rejecting the booking" });
            }
        }

        /// <summary>
        /// Cancel booking action
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Cancel(string id, string cancelledBy, string reason)
        {
            try
            {
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(cancelledBy) || string.IsNullOrEmpty(reason))
                {
                    return Json(new { success = false, message = "Invalid parameters. Reason is required for cancellation." });
                }

                var cancellationData = new
                {
                    cancelledBy = cancelledBy,
                    cancellationReason = reason
                };

                var response = await _apiService.PostAsync<object>($"api/bookings/{id}/cancel", cancellationData);

                if (response != null)
                {
                    return Json(new { success = true, message = "Booking cancelled successfully" });
                }

                return Json(new { success = false, message = "Failed to cancel booking" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling booking {id}");
                return Json(new { success = false, message = "An error occurred while cancelling the booking" });
            }
        }

        /// <summary>
        /// Complete booking action
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Complete(string id, decimal? totalCost = null, decimal? energyConsumed = null)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return Json(new { success = false, message = "Invalid booking ID" });
                }

                var completionData = new
                {
                    status = 2, // BookingStatus.Completed
                    updatedBy = HttpContext.Session.GetString("UserName") ?? "System",
                    actualEndTime = DateTime.UtcNow,
                    totalCost = totalCost,
                    energyConsumedKWh = energyConsumed
                };

                var response = await _apiService.PatchAsync<object>($"api/bookings/{id}/status", completionData);

                if (response != null)
                {
                    return Json(new { success = true, message = "Booking completed successfully" });
                }

                return Json(new { success = false, message = "Failed to complete booking" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error completing booking {id}");
                return Json(new { success = false, message = "An error occurred while completing the booking" });
            }
        }

        /// <summary>
        /// Get booking statistics
        /// </summary>
        public async Task<IActionResult> Statistics(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var queryParams = new Dictionary<string, string?>();

                if (fromDate.HasValue)
                {
                    queryParams["fromDate"] = fromDate.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
                }

                if (toDate.HasValue)
                {
                    queryParams["toDate"] = toDate.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
                }

                var stats = await _apiService.GetAsync<BookingStatsResponse>("api/bookings/statistics", queryParams);

                var viewModel = new BookingStatisticsViewModel
                {
                    Statistics = stats ?? new BookingStatsResponse(),
                    FromDate = fromDate,
                    ToDate = toDate
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading booking statistics");
                TempData["ErrorMessage"] = "Failed to load statistics.";
                return View(new BookingStatisticsViewModel());
            }
        }

        /// <summary>
        /// Get QR code for booking
        /// </summary>
        public async Task<IActionResult> QRCode(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest("Booking ID is required");
                }

                var qrCodeData = await _apiService.GetFileAsync($"api/bookings/{id}/qrcode");

                if (qrCodeData != null)
                {
                    return File(qrCodeData, "image/png", $"booking-{id}-qrcode.png");
                }

                TempData["ErrorMessage"] = "QR code not found for this booking.";
                return RedirectToAction(nameof(Details), new { id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving QR code for booking {id}");
                TempData["ErrorMessage"] = "Failed to retrieve QR code.";
                return RedirectToAction(nameof(Details), new { id = id });
            }
        }

        /// <summary>
        /// Bulk approval action
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> BulkApprove(string[] bookingIds, string approvedBy)
        {
            try
            {
                if (bookingIds == null || bookingIds.Length == 0)
                {
                    return Json(new { success = false, message = "No bookings selected" });
                }

                var results = new List<object>();
                var successCount = 0;

                foreach (var bookingId in bookingIds)
                {
                    try
                    {
                        var approvalData = new
                        {
                            isApproved = true,
                            approvedBy = approvedBy
                        };

                        var response = await _apiService.PostAsync<object>($"api/bookings/{bookingId}/approve", approvalData);

                        if (response != null)
                        {
                            successCount++;
                            results.Add(new { bookingId = bookingId, success = true, message = "Approved" });
                        }
                        else
                        {
                            results.Add(new { bookingId = bookingId, success = false, message = "Failed to approve" });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error in bulk approval for booking {bookingId}");
                        results.Add(new { bookingId = bookingId, success = false, message = "Error occurred" });
                    }
                }

                return Json(new
                {
                    success = successCount > 0,
                    message = $"Approved {successCount} out of {bookingIds.Length} bookings",
                    results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk approval");
                return Json(new { success = false, message = "An error occurred during bulk approval" });
            }
        }

        /// <summary>
        /// Export bookings to CSV
        /// </summary>
        public async Task<IActionResult> Export(
            string? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                var queryParams = new Dictionary<string, string?>
                {
                    ["pageSize"] = "10000", // Get all records for export
                    ["sortBy"] = "CreatedAt",
                    ["sortDescending"] = "true"
                };

                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    queryParams["status"] = status;
                }

                if (fromDate.HasValue)
                {
                    queryParams["createdFrom"] = fromDate.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
                }

                if (toDate.HasValue)
                {
                    queryParams["createdTo"] = toDate.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
                }

                var bookingsResponse = await _apiService.GetAsync<BookingPagedResponse>("api/bookings", queryParams);

                if (bookingsResponse?.Bookings == null || !bookingsResponse.Bookings.Any())
                {
                    TempData["ErrorMessage"] = "No bookings found for export.";
                    return RedirectToAction(nameof(Index));
                }

                var csv = GenerateBookingsCsv(bookingsResponse.Bookings);
                var fileName = $"bookings_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

                return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting bookings");
                TempData["ErrorMessage"] = "Failed to export bookings.";
                return RedirectToAction(nameof(Index));
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Generate CSV content from bookings list
        /// </summary>
        private string GenerateBookingsCsv(List<BookingResponse> bookings)
        {
            var csv = new System.Text.StringBuilder();
            
            // Headers
            csv.AppendLine("Booking ID,User Name,User NIC,Station Name,Vehicle Number,Booking Date,Start Time,End Time,Status,Created At,Total Cost,Energy Consumed");

            // Data
            foreach (var booking in bookings)
            {
                csv.AppendLine($"{booking.Id}," +
                              $"\"{booking.User?.FullName ?? "N/A"}\"," +
                              $"\"{booking.User?.NIC ?? "N/A"}\"," +
                              $"\"{booking.ChargingStation?.StationName ?? "N/A"}\"," +
                              $"\"{booking.VehicleNumber}\"," +
                              $"{booking.BookingDate:yyyy-MM-dd}," +
                              $"{booking.StartTime:yyyy-MM-dd HH:mm}," +
                              $"{booking.EndTime:yyyy-MM-dd HH:mm}," +
                              $"{booking.Status}," +
                              $"{booking.CreatedAt:yyyy-MM-dd HH:mm}," +
                              $"{booking.TotalCost ?? 0:F2}," +
                              $"{booking.EnergyConsumedKWh ?? 0:F2}");
            }

            return csv.ToString();
        }

        #endregion
    }
}