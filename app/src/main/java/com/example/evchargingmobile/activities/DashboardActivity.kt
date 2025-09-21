/*
 * File: DashboardActivity.kt
 * Description: Dashboard home screen for EV owners and station operators with Jetpack Compose
 * Author: EV Charging Team
 * Date: September 21, 2025
 */
package com.example.evchargingmobile.activities

import android.content.Intent
import android.content.SharedPreferences
import android.os.Bundle
import android.widget.Toast
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.runtime.*
import androidx.lifecycle.lifecycleScope
import com.example.evchargingmobile.database.DatabaseHelper
import com.example.evchargingmobile.models.Booking
import com.example.evchargingmobile.models.User
import com.example.evchargingmobile.network.ApiService
import com.example.evchargingmobile.ui.screens.DashboardScreen
import com.example.evchargingmobile.ui.screens.StationOperatorDashboard
import com.example.evchargingmobile.ui.theme.EVChargingTheme
import kotlinx.coroutines.launch

class DashboardActivity : ComponentActivity() {

    private lateinit var sharedPreferences: SharedPreferences
    private lateinit var databaseHelper: DatabaseHelper
    private lateinit var apiService: ApiService
    private lateinit var currentUserNic: String
    private lateinit var userType: User.UserType

    companion object {
        private const val PREF_NAME = "EVChargingApp"
        private const val KEY_USER_NIC = "userNic"
        private const val KEY_USER_TYPE = "userType"
        private const val KEY_IS_LOGGED_IN = "isLoggedIn"
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        initializeServices()
        loadUserInfo()

        setContent {
            EVChargingTheme {
                DashboardContent()
            }
        }
    }

    override fun onResume() {
        super.onResume()
        // Data will be refreshed automatically through the Composable
    }

    @Composable
    private fun DashboardContent() {
        var pendingCount by remember { mutableStateOf(0) }
        var approvedCount by remember { mutableStateOf(0) }
        var stationsCount by remember { mutableStateOf(0) }

        // Station Operator specific state
        var activeBookings by remember { mutableStateOf(0) }
        var completedToday by remember { mutableStateOf(0) }
        var stationStatus by remember { mutableStateOf("Online") }

        LaunchedEffect(Unit) {
            if (userType == User.UserType.EV_OWNER) {
                loadDashboardData { pending, approved, stations ->
                    pendingCount = pending
                    approvedCount = approved
                    stationsCount = stations
                }
            } else {
                loadStationOperatorData { active, completed, status ->
                    activeBookings = active
                    completedToday = completed
                    stationStatus = status
                }
            }
        }

        // Show different dashboard based on user type
        when (userType) {
            User.UserType.EV_OWNER -> {
                DashboardScreen(
                    onNavigateToBookings = ::openMyBookings,
                    onNavigateToStations = ::openNearbyStations,
                    onNavigateToProfile = ::openProfile,
                    onNavigateToHistory = ::openBookingHistory,
                    onLogout = ::performLogout,
                    pendingReservations = pendingCount,
                    approvedReservations = approvedCount,
                    nearbyStations = stationsCount
                )
            }
            User.UserType.STATION_OPERATOR -> {
                StationOperatorDashboard(
                    onNavigateToQRScanner = ::openQRScanner,
                    onNavigateToActiveBookings = ::openActiveBookings,
                    onNavigateToStationStatus = ::openStationStatus,
                    onNavigateToProfile = ::openProfile,
                    onLogout = ::performLogout,
                    activeBookings = activeBookings,
                    completedToday = completedToday,
                    stationStatus = stationStatus
                )
            }
        }
    }

    /**
     * Initialize services
     */
    private fun initializeServices() {
        sharedPreferences = getSharedPreferences(PREF_NAME, MODE_PRIVATE)
        databaseHelper = DatabaseHelper(this)
        apiService = ApiService.getInstance()
    }

    /**
     * Load current user information
     */
    private fun loadUserInfo() {
        currentUserNic = sharedPreferences.getString(KEY_USER_NIC, "") ?: ""
        val userTypeStr = sharedPreferences.getString(KEY_USER_TYPE, "EV_OWNER") ?: "EV_OWNER"
        userType = User.UserType.valueOf(userTypeStr)

        // Get user details from local database
        val user = databaseHelper.getUserByNIC(currentUserNic)
        user?.let {
            showToast("Welcome, ${it.fullName}")
        }
    }

    /**
     * Load real dashboard data from database and API
     */
    private fun loadDashboardData(onDataLoaded: (Int, Int, Int) -> Unit) {
        lifecycleScope.launch {
            try {
                // Get bookings from local database
                val localBookings = databaseHelper.getBookingsByUser(currentUserNic)

                // Count pending and approved bookings
                var pendingCount = 0
                var approvedCount = 0

                localBookings.forEach { booking ->
                    when (booking.status) {
                        Booking.BookingStatus.PENDING -> pendingCount++
                        Booking.BookingStatus.APPROVED, Booking.BookingStatus.CONFIRMED -> approvedCount++
                        else -> {} // Handle other statuses
                    }
                }

                // Get nearby stations count from local database
                val localStations = databaseHelper.getAllActiveStations()
                val stationsCount = localStations.size

                // Update UI with real data
                onDataLoaded(pendingCount, approvedCount, stationsCount)

            } catch (e: Exception) {
                showToast("Error loading dashboard data: ${e.message}")
                // Fallback to default values
                onDataLoaded(0, 0, 0)
            }
        }
    }

    /**
     * Load data for Station Operator dashboard
     */
    private fun loadStationOperatorData(onDataLoaded: (Int, Int, String) -> Unit) {
        lifecycleScope.launch {
            try {
                // Get bookings from local database using existing method
                val localBookings = databaseHelper.getBookingsByUser(currentUserNic)

                // Count active and completed bookings
                var activeCount = 0
                var completedCount = 0

                localBookings.forEach { booking: Booking ->
                    when (booking.status) {
                        Booking.BookingStatus.ACTIVE -> activeCount++
                        Booking.BookingStatus.COMPLETED -> completedCount++
                        else -> {} // Handle other statuses
                    }
                }

                // For now, assume station is online - this can be enhanced later
                val status = "Online"

                // Update UI with real data
                onDataLoaded(activeCount, completedCount, status)

            } catch (e: Exception) {
                showToast("Error loading station operator data: ${e.message}")
                // Fallback to default values
                onDataLoaded(0, 0, "Offline")
            }
        }
    }

    /**
     * Perform logout with proper cleanup
     */
    private fun performLogout() {
        try {
            // Clear shared preferences
            sharedPreferences.edit().apply {
                remove(KEY_IS_LOGGED_IN)
                remove(KEY_USER_NIC)
                remove(KEY_USER_TYPE)
                apply()
            }

            // Clear sensitive data from database if needed
            // databaseHelper.clearUserSession()

            showToast("Logged out successfully")

            // Navigate to login screen
            val intent = Intent(this@DashboardActivity, LoginActivity::class.java).apply {
                flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
            }
            startActivity(intent)
            finish()

        } catch (e: Exception) {
            showToast("Logout error: ${e.message}")
        }
    }

    /**
     * Show toast message
     */
    private fun showToast(message: String) {
        Toast.makeText(this, message, Toast.LENGTH_SHORT).show()
    }

    /**
     * Navigation methods
     */
    private fun openNewBooking() {
        showToast("New Booking - Coming soon")
        // TODO: Navigate to booking screen
    }

    private fun openMyBookings() {
        showToast("My Bookings - Coming soon")
        // TODO: Navigate to bookings list screen
    }

    private fun openProfile() {
        showToast("Profile - Coming soon")
        // TODO: Navigate to profile screen
    }

    private fun openQRScanner() {
        showToast("QR Scanner - Coming soon")
        // TODO: Navigate to QR scanner screen
    }

    private fun openNearbyStations() {
        showToast("Nearby Stations - Coming soon")
        // TODO: Navigate to stations map screen
    }

    private fun openBookingHistory() {
        showToast("Booking History - Coming soon")
        // TODO: Navigate to history screen
    }

    private fun openActiveBookings() {
        showToast("Active Bookings - Coming soon")
        // TODO: Navigate to active bookings screen
    }

    private fun openStationStatus() {
        showToast("Station Status - Coming soon")
        // TODO: Navigate to station status screen
    }
}
