package com.example.evchargingmobile.viewmodels

import android.content.SharedPreferences
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.evchargingmobile.database.DatabaseHelper
import com.example.evchargingmobile.models.Booking
import com.example.evchargingmobile.models.ChargingStation
import com.example.evchargingmobile.models.User
import com.example.evchargingmobile.network.ApiService
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import java.time.Duration
import java.time.LocalDateTime
import java.time.OffsetDateTime
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.time.format.DateTimeParseException

data class DashboardUiState(
    val isLoading: Boolean = true,
    val userName: String = "",
    val userNic: String = "",
    val userEmail: String = "",
    val userPhone: String? = null,
    val userAddress: String? = null,
    val userLatitude: Double? = null,
    val userLongitude: Double? = null,
    val isApproved: Boolean = false,
    val userType: User.UserType = User.UserType.EV_OWNER,
    val pendingCount: Int = 0,
    val approvedCount: Int = 0,
    val stations: List<StationUiModel> = emptyList(),
    val errorMessage: String? = null,
    val lastUpdated: LocalDateTime? = null
)

data class StationUiModel(
    val id: String,
    val name: String,
    val address: String,
    val latitude: Double,
    val longitude: Double,
    val connectorType: String,
    val availableSlots: Int,
    val totalSlots: Int,
    val isAvailable: Boolean,
    val distanceKm: Double? = null  // Distance from user in kilometers
)

class DashboardViewModel(
    private val apiService: ApiService,
    private val databaseHelper: DatabaseHelper,
    private val sharedPreferences: SharedPreferences
) : ViewModel() {

    companion object {
        private const val PREF_KEY_USER_NIC = "userNic"
        private const val PREF_KEY_USER_TYPE = "userType"
    }

    private val _uiState = MutableStateFlow(DashboardUiState())
    val uiState: StateFlow<DashboardUiState> = _uiState.asStateFlow()

    init {
        refreshDashboard()
    }

    fun refreshDashboard() {
        viewModelScope.launch {
            loadDashboard()
        }
    }

    private suspend fun loadDashboard() {
        val userNic = sharedPreferences.getString(PREF_KEY_USER_NIC, "").orEmpty()
        val userTypeStr = sharedPreferences.getString(PREF_KEY_USER_TYPE, User.UserType.EV_OWNER.name).orEmpty()
        val userType = runCatching { User.UserType.valueOf(userTypeStr) }.getOrDefault(User.UserType.EV_OWNER)

        _uiState.update {
            it.copy(
                isLoading = true,
                errorMessage = null,
                userType = userType
            )
        }

        if (userNic.isBlank()) {
            _uiState.update {
                it.copy(
                    isLoading = false,
                    errorMessage = "Missing user identifier. Please log in again."
                )
            }
            return
        }

        val bookingsResult = apiService.getBookingsByNic(userNic)
        val stationsResult = apiService.fetchChargingStations()

        val bookings = if (bookingsResult.isSuccess) {
            val remoteBookings = bookingsResult.getOrNull()?.bookings.orEmpty()
            // Persist latest bookings locally
            remoteBookings.forEach { response ->
                databaseHelper.saveBooking(
                    Booking(
                        id = response.id,
                        evOwnerNic = userNic,
                        chargingStationId = response.chargingStationId,
                        reservationDateTime = response.startTime,
                        bookingDate = response.bookingDate,
                        status = mapStatus(response.status),
                        qrCode = response.qrCode,
                        createdAt = response.createdAt,
                        stationName = response.chargingStation?.stationName,
                        stationLocation = response.chargingStation?.address,
                        stationType = response.chargingStation?.connectorType,
                        estimatedDuration = response.durationMinutes.toDouble(),
                        estimatedCost = response.totalCost?.toDouble() ?: 0.0
                    )
                )
            }
            remoteBookings
        } else {
            databaseHelper.getBookingsByUser(userNic).map { booking ->
                ApiService.BookingResponse(
                    id = booking.id.orEmpty(),
                    bookingNumber = "",
                    userId = booking.evOwnerNic,
                    chargingStationId = booking.chargingStationId,
                    bookingDate = booking.bookingDate,
                    startTime = booking.reservationDateTime,
                    endTime = booking.reservationDateTime,
                    status = booking.status.name,
                    vehicleNumber = "",
                    vehicleType = "",
                    estimatedChargingTimeMinutes = booking.estimatedDuration.toInt(),
                    notes = null,
                    qrCode = booking.qrCode,
                    qrCodeGeneratedAt = null,
                    createdAt = booking.createdAt,
                    modifiedAt = booking.updatedAt,
                    approvedAt = null,
                    approvedBy = null,
                    completedAt = booking.completedAt,
                    cancelledAt = booking.cancelledAt,
                    cancelledBy = null,
                    cancellationReason = null,
                    rejectedAt = null,
                    rejectedBy = null,
                    rejectionReason = null,
                    actualStartTime = null,
                    actualEndTime = null,
                    totalCost = booking.estimatedCost,
                    energyConsumedKWh = null,
                    user = null,
                    chargingStation = null,
                    isWithinBookingWindow = booking.canBeModified(),
                    canBeModified = booking.canBeModified(),
                    canBeCancelled = booking.canBeModified(),
                    isActive = booking.status == Booking.BookingStatus.ACTIVE,
                    durationMinutes = booking.estimatedDuration.toInt()
                )
            }
        }

        val now = OffsetDateTime.now()
        val upcoming = bookings.filter { parseOffsetDateTime(it.startTime)?.isAfter(now) == true }
        val pendingCount = upcoming.count { it.status.equals("PENDING", ignoreCase = true) }
        val approvedCount = upcoming.count {
            val status = it.status.uppercase()
            status == "APPROVED" || status == "CONFIRMED" || status == "ACTIVE"
        }

        // Get user location first for proximity calculations
        val localUser = databaseHelper.getUserByNIC(userNic)
        val userLatitude = localUser?.latitude
        val userLongitude = localUser?.longitude

        val stations = if (stationsResult.isSuccess) {
            val remoteStations = stationsResult.getOrNull().orEmpty()
            remoteStations.forEach { station ->
                databaseHelper.saveChargingStation(
                    ChargingStation(
                        id = station.id,
                        name = station.stationName,
                        location = station.location,
                        address = station.address,
                        latitude = station.latitude ?: 0.0,
                        longitude = station.longitude ?: 0.0,
                        type = when (station.connectorType.uppercase()) {
                            "DC" -> ChargingStation.StationType.DC
                            "BOTH" -> ChargingStation.StationType.BOTH
                            else -> ChargingStation.StationType.AC
                        },
                        totalSlots = station.totalSlots,
                        availableSlots = station.availableSlots,
                        isActive = station.isAvailable
                    )
                )
            }
            remoteStations.mapNotNull { it.toUiModel(userLat = userLatitude, userLng = userLongitude) }
                .sortedBy { it.distanceKm ?: Double.MAX_VALUE }  // Sort by distance, nulls last
        } else {
            databaseHelper.getAllActiveStations().map { station ->
                StationUiModel(
                    id = station.id.orEmpty(),
                    name = station.name,
                    address = station.address,
                    latitude = station.latitude,
                    longitude = station.longitude,
                    connectorType = station.type.name,
                    availableSlots = station.availableSlots,
                    totalSlots = station.totalSlots,
                    isAvailable = station.isActive,
                    distanceKm = if (userLatitude != null && userLongitude != null) {
                        calculateDistance(userLatitude, userLongitude, station.latitude, station.longitude)
                    } else null
                )
            }.sortedBy { it.distanceKm ?: Double.MAX_VALUE }
        }

        val displayName = localUser?.fullName ?: sharedPreferences.getString("userName", "") ?: "User"
        val email = localUser?.email ?: sharedPreferences.getString("userEmail", "") ?: ""
        val phone = localUser?.phoneNumber ?: sharedPreferences.getString("userPhone", null)
        val address = localUser?.address ?: sharedPreferences.getString("userAddress", null)
        val isApproved = localUser?.isApproved ?: sharedPreferences.getBoolean("isApproved", false)

        _uiState.update {
            it.copy(
                isLoading = false,
                userName = displayName,
                userNic = userNic,
                userEmail = email,
                userPhone = phone,
                userAddress = address,
                userLatitude = userLatitude,
                userLongitude = userLongitude,
                isApproved = isApproved,
                pendingCount = pendingCount,
                approvedCount = approvedCount,
                stations = stations,
                errorMessage = if (bookingsResult.isSuccess && stationsResult.isSuccess) null else buildErrorMessage(bookingsResult, stationsResult),
                lastUpdated = LocalDateTime.now()
            )
        }
    }

    private fun buildErrorMessage(
        bookingsResult: Result<ApiService.BookingPagedResponse>,
        stationsResult: Result<List<ApiService.ChargingStationResponse>>
    ): String? {
        val errors = mutableListOf<String>()
        if (bookingsResult.isFailure) {
            errors.add("Failed to sync bookings")
        }
        if (stationsResult.isFailure) {
            errors.add("Failed to sync charging stations")
        }
        return if (errors.isEmpty()) null else errors.joinToString(". ")
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

    private fun parseOffsetDateTime(value: String?): OffsetDateTime? {
        if (value.isNullOrBlank()) return null
        return try {
            OffsetDateTime.parse(value)
        } catch (ex: DateTimeParseException) {
            try {
                val local = LocalDateTime.parse(value)
                local.atZone(ZoneId.systemDefault()).toOffsetDateTime()
            } catch (_: Exception) {
                null
            }
        }
    }

    private fun ApiService.ChargingStationResponse.toUiModel(userLat: Double? = null, userLng: Double? = null): StationUiModel? {
        val lat = latitude ?: return null
        val lng = longitude ?: return null
        val idValue = id ?: stationName
        val distance = if (userLat != null && userLng != null) {
            calculateDistance(userLat, userLng, lat, lng)
        } else null
        
        return StationUiModel(
            id = idValue,
            name = stationName,
            address = address,
            latitude = lat,
            longitude = lng,
            connectorType = connectorType,
            availableSlots = availableSlots,
            totalSlots = totalSlots,
            isAvailable = isAvailable,
            distanceKm = distance
        )
    }

    /**
     * Calculate distance between two coordinates using Haversine formula
     * @return distance in kilometers
     */
    private fun calculateDistance(lat1: Double, lon1: Double, lat2: Double, lon2: Double): Double {
        val R = 6371.0 // Radius of Earth in kilometers
        val dLat = Math.toRadians(lat2 - lat1)
        val dLon = Math.toRadians(lon2 - lon1)
        val a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
                Math.cos(Math.toRadians(lat1)) * Math.cos(Math.toRadians(lat2)) *
                Math.sin(dLon / 2) * Math.sin(dLon / 2)
        val c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a))
        return R * c
    }

    /**
     * Update user profile (phone and address with coordinates)
     */
    fun updateProfile(phone: String, address: String, latitude: Double?, longitude: Double?) {
        viewModelScope.launch {
            try {
                val userNic = sharedPreferences.getString(PREF_KEY_USER_NIC, null) ?: return@launch
                
                // Update via API
                val result = apiService.updateEVOwner(userNic, phone, address, latitude, longitude)
                
                result.fold(
                    onSuccess = { updatedUser ->
                        // Update local database
                        databaseHelper.updateUserProfile(userNic, phone, address, latitude, longitude)
                        
                        // Update UI state
                        _uiState.update {
                            it.copy(
                                userPhone = phone,
                                userAddress = address,
                                userLatitude = latitude,
                                userLongitude = longitude
                            )
                        }
                    },
                    onFailure = { exception ->
                        _uiState.update {
                            it.copy(errorMessage = "Failed to update profile: ${exception.message}")
                        }
                    }
                )
            } catch (e: Exception) {
                _uiState.update {
                    it.copy(errorMessage = "Error updating profile: ${e.message}")
                }
            }
        }
    }
}
