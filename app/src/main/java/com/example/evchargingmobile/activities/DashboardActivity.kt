/*
 * File: DashboardActivity.kt
 * Description: Dashboard host activity using Jetpack Compose navigation
 */
package com.example.evchargingmobile.activities

import android.content.Intent
import android.content.SharedPreferences
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.navigation.compose.rememberNavController
import com.example.evchargingmobile.database.DatabaseHelper
import com.example.evchargingmobile.network.ApiService
import com.example.evchargingmobile.ui.navigation.DashboardNavHost
import com.example.evchargingmobile.ui.navigation.OperatorDashboardNavHost
import com.example.evchargingmobile.ui.theme.EVChargingTheme
import com.example.evchargingmobile.viewmodels.BookingsViewModel
import com.example.evchargingmobile.viewmodels.DashboardViewModel
import com.example.evchargingmobile.viewmodels.OperatorViewModel

class DashboardActivity : ComponentActivity() {

    private lateinit var sharedPreferences: SharedPreferences
    private lateinit var databaseHelper: DatabaseHelper
    private val apiService = ApiService.getInstance()

    companion object {
        private const val PREF_NAME = "EVChargingApp"
        private const val KEY_IS_LOGGED_IN = "isLoggedIn"
        private const val KEY_USER_TYPE = "userType"
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        sharedPreferences = getSharedPreferences(PREF_NAME, MODE_PRIVATE)
        databaseHelper = DatabaseHelper(this)

        if (!sharedPreferences.getBoolean(KEY_IS_LOGGED_IN, false)) {
            navigateToLogin()
            return
        }

        // Get user type to determine which dashboard to show
        val userType = sharedPreferences.getString(KEY_USER_TYPE, "EV_OWNER") ?: "EV_OWNER"
        val isOperator = userType == "STATION_OPERATOR"

        val dashboardViewModel = ViewModelProvider(this, dashboardFactory())[DashboardViewModel::class.java]
        val bookingsViewModel = ViewModelProvider(this, bookingsFactory())[BookingsViewModel::class.java]
        val operatorViewModel = ViewModelProvider(this, operatorFactory())[OperatorViewModel::class.java]

        setContent {
            EVChargingTheme {
                val navController = rememberNavController()
                
                if (isOperator) {
                    // Station Operator Dashboard
                    OperatorDashboardNavHost(
                        navController = navController,
                        operatorViewModel = operatorViewModel,
                        onLogout = { performLogout() }
                    )
                } else {
                    // EV Owner Dashboard
                    DashboardNavHost(
                        navController = navController,
                        dashboardViewModel = dashboardViewModel,
                        bookingsViewModel = bookingsViewModel,
                        onLogout = { performLogout() }
                    )
                }
            }
        }
    }

    private fun dashboardFactory(): ViewModelProvider.Factory = object : ViewModelProvider.Factory {
        @Suppress("UNCHECKED_CAST")
        override fun <T : ViewModel> create(modelClass: Class<T>): T {
            return DashboardViewModel(apiService, databaseHelper, sharedPreferences) as T
        }
    }

    private fun bookingsFactory(): ViewModelProvider.Factory = object : ViewModelProvider.Factory {
        @Suppress("UNCHECKED_CAST")
        override fun <T : ViewModel> create(modelClass: Class<T>): T {
            return BookingsViewModel(apiService, databaseHelper, sharedPreferences) as T
        }
    }

    private fun operatorFactory(): ViewModelProvider.Factory = object : ViewModelProvider.Factory {
        @Suppress("UNCHECKED_CAST")
        override fun <T : ViewModel> create(modelClass: Class<T>): T {
            return OperatorViewModel(apiService, databaseHelper, sharedPreferences) as T
        }
    }

    private fun performLogout() {
        sharedPreferences.edit().clear().apply()
        databaseHelper.close()
        navigateToLogin()
    }

    private fun navigateToLogin() {
        val intent = Intent(this, LoginActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        }
        startActivity(intent)
        finish()
    }
}
