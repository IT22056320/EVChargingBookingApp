/*
 * File: OperatorViewModel.kt
 * Description: ViewModel for Station Operator dashboard and operations
 * Author: EAD Team
 * Date: 2025-10-06
 */
package com.example.evchargingmobile.viewmodels

import android.content.SharedPreferences
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.evchargingmobile.database.DatabaseHelper
import com.example.evchargingmobile.models.Booking
import com.example.evchargingmobile.network.ApiService
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

/**
 * UI state for Station Operator dashboard
 */
data class OperatorUiState(
    val isLoading: Boolean = false,
    val operatorName: String = "",
    val stationName: String = "",
    val activeBookingsCount: Int = 0,
    val todayCompletedCount: Int = 0,
    val recentBookings: List<BookingUi> = emptyList(),
    val scannedBooking: BookingUi? = null,
    val errorMessage: String? = null,
    val successMessage: String? = null
)

/**
 * ViewModel for Station Operator operations
 */
class OperatorViewModel(
    private val apiService: ApiService,
    private val databaseHelper: DatabaseHelper,
    private val sharedPreferences: SharedPreferences
) : ViewModel() {

    private val _uiState = MutableStateFlow(OperatorUiState())
    val uiState: StateFlow<OperatorUiState> = _uiState.asStateFlow()

    companion object {
        private const val PREF_KEY_USER_NAME = "userName"
        private const val PREF_KEY_STATION_ID = "stationId"
    }

    init {
        loadOperatorInfo()
        refreshDashboard()
    }

    /**
     * Load operator information from shared preferences
     */
    private fun loadOperatorInfo() {
        val operatorName = sharedPreferences.getString(PREF_KEY_USER_NAME, "Operator") ?: "Operator"
        _uiState.update { it.copy(operatorName = operatorName) }
    }

    /**
     * Refresh dashboard data
     * @param silentMode If true, won't update error messages in UI (used after completion)
     */
    fun refreshDashboard(silentMode: Boolean = false) {
        if (!silentMode) {
            _uiState.update { it.copy(isLoading = true, errorMessage = null) }
        }
        
        viewModelScope.launch {
            try {
                // Get station ID from shared preferences (stored in user's address field)
                val stationId = sharedPreferences.getString("userAddress", null)
                
                // Debug logging
                android.util.Log.d("OperatorViewModel", "=== Dashboard Refresh ===")
                android.util.Log.d("OperatorViewModel", "StationId from SharedPrefs: $stationId")
                android.util.Log.d("OperatorViewModel", "All SharedPrefs keys: ${sharedPreferences.all.keys}")
                android.util.Log.d("OperatorViewModel", "Silent mode: $silentMode")
                
                if (stationId.isNullOrEmpty()) {
                    android.util.Log.e("OperatorViewModel", "StationId is null or empty!")
                    
                    // Only show error if not in silent mode
                    if (!silentMode) {
                        _uiState.update { 
                            it.copy(
                                isLoading = false,
                                errorMessage = "Station ID not found. Please logout and login again to refresh data."
                            )
                        }
                    } else {
                        // Silent mode: just update loading state
                        _uiState.update { it.copy(isLoading = false) }
                    }
                    return@launch
                }
                
                // Get today's date for filtering completed bookings
                val today = java.time.LocalDate.now().toString()
                
                // Fetch approved bookings (active sessions)
                val approvedResult = apiService.getStationBookings(
                    stationId = stationId,
                    status = "Approved"
                )
                
                // Fetch today's completed bookings
                val completedResult = apiService.getStationBookings(
                    stationId = stationId,
                    status = "Completed",
                    startDate = today
                )
                
                // Fetch recent bookings (all statuses, last 10)
                val recentResult = apiService.getStationBookings(
                    stationId = stationId
                )
                
                val approvedBookings = approvedResult.getOrNull() ?: emptyList()
                val completedBookings = completedResult.getOrNull() ?: emptyList()
                val allBookings = recentResult.getOrNull() ?: emptyList()
                
                // Convert to BookingUi
                val recentBookingsUi = allBookings.take(10).map { booking ->
                    BookingUi(
                        id = booking.id,
                        bookingNumber = booking.bookingNumber,
                        stationName = booking.chargingStation?.stationName ?: "Unknown",
                        stationAddress = booking.chargingStation?.address,
                        status = when (booking.status) {
                            "Pending" -> Booking.BookingStatus.PENDING
                            "Approved" -> Booking.BookingStatus.APPROVED
                            "Completed" -> Booking.BookingStatus.COMPLETED
                            "Cancelled" -> Booking.BookingStatus.CANCELLED
                            else -> Booking.BookingStatus.PENDING
                        },
                        vehicleNumber = booking.vehicleNumber,
                        vehicleType = booking.vehicleType,
                        startTime = null, // TODO: Parse dates properly
                        endTime = null,
                        estimatedMinutes = booking.estimatedChargingTimeMinutes,
                        totalCost = booking.totalCost,
                        qrCode = booking.qrCode,
                        canModify = false,
                        canCancel = false,
                        notes = booking.notes
                    )
                }
                
                _uiState.update { 
                    it.copy(
                        isLoading = false,
                        activeBookingsCount = approvedBookings.size,
                        todayCompletedCount = completedBookings.size,
                        recentBookings = recentBookingsUi,
                        stationName = allBookings.firstOrNull()?.chargingStation?.stationName ?: "Station",
                        errorMessage = null // Clear any errors on successful refresh
                    )
                }
            } catch (e: Exception) {
                android.util.Log.e("OperatorViewModel", "Dashboard refresh error: ${e.message}")
                
                // Only show error if not in silent mode
                if (!silentMode) {
                    _uiState.update { 
                        it.copy(
                            isLoading = false,
                            errorMessage = "Failed to load dashboard data: ${e.message}"
                        )
                    }
                } else {
                    _uiState.update { it.copy(isLoading = false) }
                }
            }
        }
    }

    /**
     * Verify booking from scanned QR code
     */
    fun verifyBooking(bookingId: String) {
        // Don't re-verify if we already have this booking loaded
        if (_uiState.value.scannedBooking?.id == bookingId) {
            return
        }
        
        _uiState.update { it.copy(isLoading = true, errorMessage = null, scannedBooking = null) }
        
        viewModelScope.launch {
            // Get operator's assigned station ID for security validation
            val operatorStationId = sharedPreferences.getString("userAddress", null)
            
            android.util.Log.d("OperatorViewModel", "Verifying booking $bookingId with operatorStationId: $operatorStationId")
            
            val result = apiService.verifyBooking(bookingId, operatorStationId)
            
            if (result.isSuccess) {
                val bookingResponse = result.getOrNull()
                if (bookingResponse != null) {
                    // IMPORTANT: Extract and save stationId immediately for dashboard use
                    if (!bookingResponse.chargingStationId.isNullOrEmpty()) {
                        android.util.Log.d("OperatorViewModel", "Saving stationId from verification: ${bookingResponse.chargingStationId}")
                        sharedPreferences.edit()
                            .putString("userAddress", bookingResponse.chargingStationId)
                            .apply()
                    }
                    
                    // Convert BookingResponse to BookingUi (simplified for now)
                    val bookingUi = BookingUi(
                        id = bookingResponse.id,
                        bookingNumber = bookingResponse.bookingNumber,
                        stationName = bookingResponse.chargingStation?.stationName ?: "Unknown",
                        stationAddress = bookingResponse.chargingStation?.address,
                        status = Booking.BookingStatus.APPROVED, // Simplified
                        vehicleNumber = bookingResponse.vehicleNumber,
                        vehicleType = bookingResponse.vehicleType,
                        startTime = null, // TODO: Parse dates
                        endTime = null,
                        estimatedMinutes = bookingResponse.estimatedChargingTimeMinutes,
                        totalCost = bookingResponse.totalCost,
                        qrCode = bookingResponse.qrCode,
                        canModify = false,
                        canCancel = false,
                        notes = bookingResponse.notes
                    )
                    _uiState.update { 
                        it.copy(
                            isLoading = false,
                            scannedBooking = bookingUi,
                            successMessage = null  // Don't show success message for verification
                        )
                    }
                    
                    // Trigger a silent dashboard refresh to update with saved stationId
                    try {
                        android.util.Log.d("OperatorViewModel", "Triggering dashboard refresh after stationId save...")
                        refreshDashboard(silentMode = true)
                    } catch (e: Exception) {
                        android.util.Log.w("OperatorViewModel", "Dashboard refresh after verification failed: ${e.message}")
                    }
                } else {
                    _uiState.update { 
                        it.copy(
                            isLoading = false,
                            errorMessage = "Booking not found"
                        )
                    }
                }
            } else {
                _uiState.update { 
                    it.copy(
                        isLoading = false,
                        errorMessage = result.exceptionOrNull()?.message ?: "Failed to verify booking"
                    )
                }
            }
        }
    }

    /**
     * Complete a charging session
     */
    fun completeSession(bookingId: String, energyConsumed: Double, notes: String) {
        _uiState.update { it.copy(isLoading = true, errorMessage = null) }
        
        viewModelScope.launch {
            val result = apiService.completeBooking(bookingId, energyConsumed, notes)
            
            if (result.isSuccess) {
                android.util.Log.d("OperatorViewModel", "Session completed successfully")
                
                // FALLBACK: Extract and save stationId from the completed booking
                val completedBooking = result.getOrNull()
                if (completedBooking != null && !completedBooking.chargingStationId.isNullOrEmpty()) {
                    android.util.Log.d("OperatorViewModel", "Saving stationId from completed booking: ${completedBooking.chargingStationId}")
                    sharedPreferences.edit()
                        .putString("userAddress", completedBooking.chargingStationId)
                        .apply()
                }
                
                _uiState.update { 
                    it.copy(
                        isLoading = false,
                        scannedBooking = null,
                        successMessage = "Session completed successfully",
                        errorMessage = null // Clear any previous errors
                    )
                }
                
                // Try to refresh dashboard in silent mode (won't show errors)
                try {
                    android.util.Log.d("OperatorViewModel", "Attempting to refresh dashboard...")
                    refreshDashboard(silentMode = true)
                } catch (e: Exception) {
                    android.util.Log.w("OperatorViewModel", "Dashboard refresh failed (non-critical): ${e.message}")
                    // Don't show error - session was successful even if refresh fails
                }
            } else {
                _uiState.update { 
                    it.copy(
                        isLoading = false,
                        errorMessage = result.exceptionOrNull()?.message ?: "Failed to complete session"
                    )
                }
            }
        }
    }

    /**
     * Clear scanned booking
     */
    fun clearScannedBooking() {
        _uiState.update { it.copy(scannedBooking = null) }
    }

    /**
     * Clear messages
     */
    fun clearMessages() {
        _uiState.update { it.copy(errorMessage = null, successMessage = null) }
    }
}
