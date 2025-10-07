package com.example.evchargingmobile.viewmodels

import android.content.SharedPreferences
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.evchargingmobile.database.DatabaseHelper
import com.example.evchargingmobile.models.Booking
import com.example.evchargingmobile.network.ApiService
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.time.Duration
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.LocalTime
import java.time.OffsetDateTime
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.time.format.DateTimeParseException

data class BookingsUiState(
    val isLoading: Boolean = true,
    val isRefreshing: Boolean = false,
    val filter: BookingFilter = BookingFilter.ALL,
    val bookings: List<BookingUi> = emptyList(),
    val displayedBookings: List<BookingUi> = emptyList(),
    val upcomingBookings: List<BookingUi> = emptyList(),
    val pastBookings: List<BookingUi> = emptyList(),
    val selectedBookingId: String? = null,
    val errorMessage: String? = null,
    val successMessage: String? = null,
    val isActionInProgress: Boolean = false
)

data class BookingUi(
    val id: String,
    val bookingNumber: String,
    val stationName: String,
    val stationAddress: String?,
    val status: Booking.BookingStatus,
    val vehicleNumber: String,
    val vehicleType: String,
    val startTime: OffsetDateTime?,
    val endTime: OffsetDateTime?,
    val estimatedMinutes: Int,
    val totalCost: Double?,
    val qrCode: String?,
    val canModify: Boolean,
    val canCancel: Boolean,
    val notes: String?
) {
    val isUpcoming: Boolean = startTime?.isAfter(OffsetDateTime.now()) ?: false
    val statusLabel: String = status.name.replace("_", " ").lowercase().replaceFirstChar { it.uppercase() }
}

enum class BookingFilter {
    ALL,
    PENDING,
    APPROVED,
    COMPLETED,
    CANCELLED
}

class BookingsViewModel(
    private val apiService: ApiService,
    private val databaseHelper: DatabaseHelper,
    private val sharedPreferences: SharedPreferences
) : ViewModel() {

    companion object {
        private const val PREF_KEY_USER_NIC = "userNic"
    }

    private val _uiState = MutableStateFlow(BookingsUiState())
    val uiState: StateFlow<BookingsUiState> = _uiState.asStateFlow()

    init {
        refreshBookings()
    }

    fun refreshBookings() {
        val userNic = sharedPreferences.getString(PREF_KEY_USER_NIC, "").orEmpty()
        if (userNic.isBlank()) {
            _uiState.update {
                it.copy(
                    isLoading = false,
                    isRefreshing = false,
                    errorMessage = "Unable to load bookings. Please log in again."
                )
            }
            return
        }

        _uiState.update { it.copy(isRefreshing = true, errorMessage = null, successMessage = null) }

        viewModelScope.launch {
            val result = apiService.getBookingsByNic(userNic)
            if (result.isSuccess) {
                val bookings = result.getOrNull()?.bookings.orEmpty().map { response ->
                    val bookingUi = response.toBookingUi()
                    databaseHelper.saveBooking(
                        Booking(
                            id = response.id,
                            evOwnerNic = userNic,
                            chargingStationId = response.chargingStationId,
                            reservationDateTime = response.startTime,
                            bookingDate = response.bookingDate,
                            status = bookingUi.status,
                            qrCode = response.qrCode,
                            createdAt = response.createdAt,
                            updatedAt = response.modifiedAt,
                            cancelledAt = response.cancelledAt,
                            confirmedAt = response.approvedAt,
                            completedAt = response.completedAt,
                            stationName = response.chargingStation?.stationName,
                            stationLocation = response.chargingStation?.address,
                            stationType = response.chargingStation?.connectorType,
                            estimatedDuration = response.durationMinutes.toDouble(),
                            estimatedCost = response.totalCost?.toDouble() ?: 0.0
                        )
                    )
                    bookingUi
                }
                handleNewBookings(bookings)
            } else {
                val localBookings = databaseHelper.getBookingsByUser(userNic).map { it.toBookingUiFromLocal() }
                handleNewBookings(localBookings, errorMessage = "Showing offline data. ${result.exceptionOrNull()?.message ?: ""}".trim())
            }
        }
    }

    private fun handleNewBookings(bookings: List<BookingUi>, errorMessage: String? = null) {
        val upcoming = bookings.filter { it.isUpcoming }.sortedBy { it.startTime }
        val history = bookings.filterNot { it.isUpcoming }.sortedByDescending { it.startTime }
        val displayed = filterBookings(bookings, _uiState.value.filter)

        _uiState.update {
            it.copy(
                isLoading = false,
                isRefreshing = false,
                bookings = bookings,
                upcomingBookings = upcoming,
                pastBookings = history,
                displayedBookings = displayed,
                errorMessage = errorMessage,
                successMessage = null,
                isActionInProgress = false
            )
        }
    }

    fun applyFilter(filter: BookingFilter) {
        _uiState.update {
            val filtered = filterBookings(it.bookings, filter)
            it.copy(filter = filter, displayedBookings = filtered)
        }
    }

    fun selectBooking(bookingId: String) {
        _uiState.update { it.copy(selectedBookingId = bookingId) }
    }

    fun clearMessages() {
        _uiState.update { it.copy(errorMessage = null, successMessage = null) }
    }

    fun cancelBooking(bookingId: String, reason: String, onComplete: (Boolean) -> Unit = {}) {
        val booking = _uiState.value.bookings.find { it.id == bookingId }
        if (booking == null) {
            _uiState.update { it.copy(errorMessage = "Booking not found") }
            onComplete(false)
            return
        }

        if (!booking.canCancel) {
            _uiState.update { it.copy(errorMessage = "This booking can no longer be cancelled") }
            onComplete(false)
            return
        }

        if (!hasTwelveHoursRemaining(booking.startTime)) {
            _uiState.update { it.copy(errorMessage = "Cancellations must be made at least 12 hours before the reservation") }
            onComplete(false)
            return
        }

        val userNic = sharedPreferences.getString(PREF_KEY_USER_NIC, "").orEmpty()
        if (userNic.isBlank()) {
            _uiState.update { it.copy(errorMessage = "Missing user information") }
            onComplete(false)
            return
        }

        _uiState.update { it.copy(isActionInProgress = true) }
        viewModelScope.launch {
            val cancelResult = apiService.cancelBooking(
                bookingId,
                ApiService.CancelBookingRequest(
                    CancelledBy = userNic,
                    CancellationReason = reason
                )
            )

            if (cancelResult.isSuccess) {
                refreshBookings()
                _uiState.update { it.copy(successMessage = cancelResult.getOrNull()?.message) }
                onComplete(true)
            } else {
                _uiState.update {
                    it.copy(
                        isActionInProgress = false,
                        errorMessage = cancelResult.exceptionOrNull()?.message ?: "Failed to cancel booking"
                    )
                }
                onComplete(false)
            }
        }
    }

    fun updateBooking(
        bookingId: String,
        newStartTime: OffsetDateTime,
        newEndTime: OffsetDateTime,
        vehicleNumber: String,
        vehicleType: String,
        estimatedMinutes: Int,
        notes: String? = null,
        onComplete: (Boolean) -> Unit = {}
    ) {
        val booking = _uiState.value.bookings.find { it.id == bookingId }
        if (booking == null) {
            _uiState.update { it.copy(errorMessage = "Booking not found") }
            onComplete(false)
            return
        }

        if (!booking.canModify) {
            _uiState.update { it.copy(errorMessage = "This booking can no longer be modified") }
            onComplete(false)
            return
        }

        if (!hasTwelveHoursRemaining(booking.startTime)) {
            _uiState.update { it.copy(errorMessage = "Updates must be done at least 12 hours before the reservation") }
            onComplete(false)
            return
        }

        if (!isWithinSevenDays(newStartTime)) {
            _uiState.update { it.copy(errorMessage = "Reservation must be within 7 days from today") }
            onComplete(false)
            return
        }

        _uiState.update { it.copy(isActionInProgress = true) }
        viewModelScope.launch {
            val updateResult = apiService.updateBooking(
                bookingId,
                ApiService.UpdateBookingRequest(
                    BookingDate = newStartTime.toLocalDate().atStartOfDay().toString(),
                    StartTime = newStartTime.toString(),
                    EndTime = newEndTime.toString(),
                    VehicleNumber = vehicleNumber,
                    VehicleType = vehicleType,
                    EstimatedChargingTimeMinutes = estimatedMinutes,
                    Notes = notes
                )
            )

            if (updateResult.isSuccess) {
                refreshBookings()
                _uiState.update { it.copy(successMessage = "Booking updated successfully") }
                onComplete(true)
            } else {
                _uiState.update {
                    it.copy(
                        isActionInProgress = false,
                        errorMessage = updateResult.exceptionOrNull()?.message ?: "Failed to update booking"
                    )
                }
                onComplete(false)
            }
        }
    }

    fun createBooking(
        stationId: String,
        startTime: OffsetDateTime,
        endTime: OffsetDateTime,
        vehicleNumber: String,
        vehicleType: String,
        estimatedMinutes: Int,
        notes: String?,
        onResult: (Boolean, String) -> Unit
    ) {
        val userNic = sharedPreferences.getString(PREF_KEY_USER_NIC, "").orEmpty()
        if (userNic.isBlank()) {
            onResult(false, "Missing user information")
            return
        }

        if (!isWithinSevenDays(startTime)) {
            onResult(false, "Reservation must be within 7 days from today")
            return
        }

        if (!hasTwelveHoursRemaining(startTime)) {
            onResult(false, "Reservations must be created at least 12 hours in advance")
            return
        }

        _uiState.update { it.copy(isActionInProgress = true) }
        viewModelScope.launch {
            // Format dates in ISO 8601 format for the backend
            val bookingDate = startTime.toLocalDate().toString() // "2025-10-06"
            val startTimeStr = startTime.toString() // "2025-10-06T08:00+05:30"
            val endTimeStr = endTime.toString() // "2025-10-06T09:00+05:30"
            
            val request = ApiService.CreateBookingRequest(
                UserId = userNic,
                ChargingStationId = stationId,
                BookingDate = bookingDate,
                StartTime = startTimeStr,
                EndTime = endTimeStr,
                VehicleNumber = vehicleNumber,
                VehicleType = vehicleType,
                EstimatedChargingTimeMinutes = estimatedMinutes,
                Notes = notes
            )

            val result = apiService.createBooking(request)
            if (result.isSuccess) {
                refreshBookings()
                _uiState.update { it.copy(successMessage = "Booking request submitted successfully") }
                onResult(true, "Booking request submitted successfully")
            } else {
                _uiState.update { it.copy(isActionInProgress = false) }
                onResult(false, result.exceptionOrNull()?.message ?: "Failed to create booking")
            }
        }
    }

    suspend fun validateTimeSlot(
        stationId: String,
        startTime: OffsetDateTime,
        endTime: OffsetDateTime,
        excludeBookingId: String? = null
    ): Result<ApiService.TimeSlotAvailabilityResponse> {
        return withContext(Dispatchers.IO) {
            apiService.checkTimeSlotAvailability(
                ApiService.TimeSlotCheckRequest(
                    ChargingStationId = stationId,
                    Date = startTime.toLocalDate().toString(),
                    StartTime = startTime.toString(),
                    EndTime = endTime.toString(),
                    ExcludeBookingId = excludeBookingId
                )
            )
        }
    }

    private fun filterBookings(bookings: List<BookingUi>, filter: BookingFilter): List<BookingUi> {
        return when (filter) {
            BookingFilter.ALL -> bookings.sortedByDescending { it.startTime }
            BookingFilter.PENDING -> bookings.filter { it.status == Booking.BookingStatus.PENDING }.sortedBy { it.startTime }
            BookingFilter.APPROVED -> bookings.filter {
                it.status == Booking.BookingStatus.APPROVED ||
                    it.status == Booking.BookingStatus.CONFIRMED ||
                    it.status == Booking.BookingStatus.ACTIVE
            }.sortedBy { it.startTime }
            BookingFilter.COMPLETED -> bookings.filter { it.status == Booking.BookingStatus.COMPLETED }.sortedByDescending { it.startTime }
            BookingFilter.CANCELLED -> bookings.filter { it.status == Booking.BookingStatus.CANCELLED }.sortedByDescending { it.startTime }
        }
    }

    private fun ApiService.BookingResponse.toBookingUi(): BookingUi {
        val start = parseOffsetDateTime(startTime)
        val end = parseOffsetDateTime(endTime)
        return BookingUi(
            id = id,
            bookingNumber = bookingNumber,
            stationName = chargingStation?.stationName ?: "Unknown Station",
            stationAddress = chargingStation?.address,
            status = mapStatus(status),
            vehicleNumber = vehicleNumber,
            vehicleType = vehicleType,
            startTime = start,
            endTime = end,
            estimatedMinutes = estimatedChargingTimeMinutes,
            totalCost = totalCost,
            qrCode = qrCode,
            canModify = canBeModified,
            canCancel = canBeCancelled,
            notes = notes
        )
    }

    private fun Booking.toBookingUiFromLocal(): BookingUi {
        val start = parseOffsetDateTime(reservationDateTime)
        return BookingUi(
            id = id.orEmpty(),
            bookingNumber = "",
            stationName = stationName ?: "Unknown Station",
            stationAddress = stationLocation,
            status = status,
            vehicleNumber = "",
            vehicleType = stationType ?: "",
            startTime = start,
            endTime = start?.plusMinutes(estimatedDuration.toLong()),
            estimatedMinutes = estimatedDuration.toInt(),
            totalCost = estimatedCost,
            qrCode = qrCode,
            canModify = canBeModified(),
            canCancel = canBeModified(),
            notes = null
        )
    }

    private fun parseOffsetDateTime(value: String?): OffsetDateTime? {
        if (value.isNullOrBlank()) return null
        return try {
            OffsetDateTime.parse(value)
        } catch (_: DateTimeParseException) {
            try {
                val local = LocalDateTime.parse(value)
                local.atZone(ZoneId.systemDefault()).toOffsetDateTime()
            } catch (_: Exception) {
                null
            }
        }
    }

    private fun mapStatus(status: String?): Booking.BookingStatus {
        return when (status?.uppercase()) {
            "APPROVED" -> Booking.BookingStatus.APPROVED
            "CONFIRMED" -> Booking.BookingStatus.CONFIRMED
            "ACTIVE" -> Booking.BookingStatus.ACTIVE
            "COMPLETED" -> Booking.BookingStatus.COMPLETED
            "CANCELLED" -> Booking.BookingStatus.CANCELLED
            else -> Booking.BookingStatus.PENDING
        }
    }

    private fun hasTwelveHoursRemaining(startTime: OffsetDateTime?): Boolean {
        if (startTime == null) return false
        val hours = Duration.between(OffsetDateTime.now(), startTime).toHours()
        return hours >= 12
    }

    private fun isWithinSevenDays(startTime: OffsetDateTime): Boolean {
        val now = OffsetDateTime.now()
        val days = Duration.between(now, startTime).toDays()
        return days <= 7 && days >= 0
    }

    fun calculateEndTime(startDate: LocalDate, startTime: LocalTime, estimatedMinutes: Int): OffsetDateTime {
        val localDateTime = LocalDateTime.of(startDate, startTime)
        return localDateTime.atZone(ZoneId.systemDefault()).toOffsetDateTime().plusMinutes(estimatedMinutes.toLong())
    }
}
