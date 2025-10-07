package com.example.evchargingmobile.ui.navigation

import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.navigation.NavHostController
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.navArgument
import com.example.evchargingmobile.ui.screens.booking.CreateBookingScreen
import com.example.evchargingmobile.ui.screens.bookings.BookingDetailScreen
import com.example.evchargingmobile.ui.screens.bookings.BookingsScreen
import com.example.evchargingmobile.ui.screens.dashboard.DashboardHomeScreen
import com.example.evchargingmobile.ui.screens.profile.ProfileScreen
import com.example.evchargingmobile.ui.screens.profile.EditProfileScreen
import com.example.evchargingmobile.viewmodels.BookingsViewModel
import com.example.evchargingmobile.viewmodels.DashboardViewModel

sealed class DashboardRoute(val route: String) {
    object Home : DashboardRoute("dashboard/home")
    object Bookings : DashboardRoute("dashboard/bookings")
    object CreateBooking : DashboardRoute("dashboard/create-booking")
    object Profile : DashboardRoute("dashboard/profile")
    object EditProfile : DashboardRoute("dashboard/edit-profile")
    object BookingDetail : DashboardRoute("dashboard/booking/{bookingId}") {
        fun createRoute(id: String) = "dashboard/booking/$id"
    }
}

@Composable
fun DashboardNavHost(
    navController: NavHostController,
    dashboardViewModel: DashboardViewModel,
    bookingsViewModel: BookingsViewModel,
    onLogout: () -> Unit
) {
    val dashboardState by dashboardViewModel.uiState.collectAsState()
    val bookingsState by bookingsViewModel.uiState.collectAsState()

    NavHost(
        navController = navController,
        startDestination = DashboardRoute.Home.route
    ) {
        composable(DashboardRoute.Home.route) {
            // Refresh dashboard when screen appears
            LaunchedEffect(Unit) {
                dashboardViewModel.refreshDashboard()
                bookingsViewModel.refreshBookings()
            }
            
            DashboardHomeScreen(
                state = dashboardState,
                onRefresh = { dashboardViewModel.refreshDashboard() },
                onNavigateToBookings = { navController.navigate(DashboardRoute.Bookings.route) },
                onNavigateToCreateBooking = { navController.navigate(DashboardRoute.CreateBooking.route) },
                onNavigateToProfile = { navController.navigate(DashboardRoute.Profile.route) },
                onLogout = onLogout
            )
        }

        composable(DashboardRoute.Bookings.route) {
            // Refresh bookings when screen appears
            LaunchedEffect(Unit) {
                bookingsViewModel.refreshBookings()
            }
            
            BookingsScreen(
                state = bookingsState,
                onBack = { navController.popBackStack() },
                onRefresh = { bookingsViewModel.refreshBookings() },
                onFilterChanged = { filter -> bookingsViewModel.applyFilter(filter) },
                onBookingSelected = { bookingId ->
                    bookingsViewModel.selectBooking(bookingId)
                    navController.navigate(DashboardRoute.BookingDetail.createRoute(bookingId))
                },
                onClearMessage = { bookingsViewModel.clearMessages() }
            )
        }

        composable(
            route = DashboardRoute.BookingDetail.route,
            arguments = listOf(navArgument("bookingId") { type = NavType.StringType })
        ) { backStackEntry ->
            val bookingId = backStackEntry.arguments?.getString("bookingId").orEmpty()
            val booking = bookingsState.bookings.find { it.id == bookingId }
            BookingDetailScreen(
                booking = booking,
                successMessage = bookingsState.successMessage,
                errorMessage = bookingsState.errorMessage,
                isProcessing = bookingsState.isActionInProgress,
                onBack = { navController.popBackStack() },
                onCancel = { id, reason ->
                    bookingsViewModel.cancelBooking(id, reason) { success ->
                        if (success) {
                            navController.popBackStack()
                        }
                    }
                },
                onUpdate = { id, start, end, vehicleNumber, vehicleType, minutes, notes ->
                    bookingsViewModel.updateBooking(id, start, end, vehicleNumber, vehicleType, minutes, notes)
                },
                onClearMessage = { bookingsViewModel.clearMessages() }
            )
        }

        composable(DashboardRoute.CreateBooking.route) {
            CreateBookingScreen(
                stations = dashboardState.stations,
                isSubmitting = bookingsState.isActionInProgress,
                onBack = { navController.popBackStack() },
                onSubmit = { stationId, start, end, vehicleNumber, vehicleType, minutes, notes, onResult ->
                    bookingsViewModel.createBooking(stationId, start, end, vehicleNumber, vehicleType, minutes, notes, onResult)
                },
                onValidateTimeSlot = { stationId, start, end ->
                    bookingsViewModel.validateTimeSlot(stationId, start, end)
                }
            )
        }

        composable(DashboardRoute.Profile.route) {
            ProfileScreen(
                userNic = dashboardState.userNic,
                userFullName = dashboardState.userName,
                userEmail = dashboardState.userEmail,
                userPhone = dashboardState.userPhone,
                userAddress = dashboardState.userAddress,
                isApproved = dashboardState.isApproved,
                onBack = { navController.popBackStack() },
                onLogout = onLogout,
                onEditProfile = { navController.navigate(DashboardRoute.EditProfile.route) }
            )
        }

        composable(DashboardRoute.EditProfile.route) {
            EditProfileScreen(
                userNic = dashboardState.userNic,
                userName = dashboardState.userName,
                userEmail = dashboardState.userEmail,
                initialPhone = dashboardState.userPhone,
                initialAddress = dashboardState.userAddress,
                initialLatitude = dashboardState.userLatitude,
                initialLongitude = dashboardState.userLongitude,
                onBack = { navController.popBackStack() },
                onSave = { phone, address, latitude, longitude ->
                    dashboardViewModel.updateProfile(phone, address, latitude, longitude)
                }
            )
        }
    }
}

