/*
 * File: OperatorNavGraph.kt
 * Description: Navigation graph for Station Operator screens
 * Author: EAD Team
 * Date: 2025-10-06
 */
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
import com.example.evchargingmobile.ui.screens.operator.CompleteSessionScreen
import com.example.evchargingmobile.ui.screens.operator.OperatorDashboardScreen
import com.example.evchargingmobile.ui.screens.operator.QRScannerScreen
import com.example.evchargingmobile.ui.screens.operator.VerifyBookingScreen
import com.example.evchargingmobile.viewmodels.OperatorViewModel

sealed class OperatorRoute(val route: String) {
    object Dashboard : OperatorRoute("operator/dashboard")
    object ScanQR : OperatorRoute("operator/scan")
    object VerifyBooking : OperatorRoute("operator/verify/{bookingId}") {
        fun createRoute(bookingId: String) = "operator/verify/$bookingId"
    }
    object CompleteSession : OperatorRoute("operator/complete/{bookingId}") {
        fun createRoute(bookingId: String) = "operator/complete/$bookingId"
    }
}

@Composable
fun OperatorDashboardNavHost(
    navController: NavHostController,
    operatorViewModel: OperatorViewModel,
    onLogout: () -> Unit
) {
    val operatorState by operatorViewModel.uiState.collectAsState()
    
    NavHost(
        navController = navController,
        startDestination = OperatorRoute.Dashboard.route
    ) {
        composable(OperatorRoute.Dashboard.route) {
            LaunchedEffect(Unit) {
                operatorViewModel.refreshDashboard()
            }

            OperatorDashboardScreen(
                state = operatorState,
                onRefresh = { operatorViewModel.refreshDashboard() },
                onLogout = onLogout,
                onScanQR = {
                    navController.navigate(OperatorRoute.ScanQR.route)
                }
            )
        }

        composable(OperatorRoute.ScanQR.route) {
            QRScannerScreen(
                onScanResult = { bookingId ->
                    navController.navigate(OperatorRoute.VerifyBooking.createRoute(bookingId))
                },
                onBack = {
                    navController.popBackStack()
                }
            )
        }

        composable(
            route = OperatorRoute.VerifyBooking.route,
            arguments = listOf(navArgument("bookingId") { type = NavType.StringType })
        ) { backStackEntry ->
            val bookingId = backStackEntry.arguments?.getString("bookingId").orEmpty()
            
            VerifyBookingScreen(
                bookingId = bookingId,
                state = operatorState,
                onVerify = { id ->
                    operatorViewModel.verifyBooking(id)
                },
                onStartSession = { id ->
                    navController.navigate(OperatorRoute.CompleteSession.createRoute(id))
                },
                onCancel = {
                    operatorViewModel.clearScannedBooking()
                    navController.popBackStack()
                },
                onBack = {
                    operatorViewModel.clearScannedBooking()
                    navController.popBackStack()
                }
            )
        }

        composable(
            route = OperatorRoute.CompleteSession.route,
            arguments = listOf(navArgument("bookingId") { type = NavType.StringType })
        ) { backStackEntry ->
            val bookingId = backStackEntry.arguments?.getString("bookingId").orEmpty()
            
            CompleteSessionScreen(
                bookingId = bookingId,
                state = operatorState,
                onComplete = { id, energy, notes ->
                    operatorViewModel.completeSession(id, energy, notes)
                },
                onBack = {
                    operatorViewModel.clearScannedBooking()
                    navController.popBackStack(OperatorRoute.Dashboard.route, false)
                }
            )
        }
    }
}
