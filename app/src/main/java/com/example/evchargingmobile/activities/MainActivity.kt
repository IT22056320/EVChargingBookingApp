/*
 * File: MainActivity.kt
 * Description: Main entry point activity for the EV Charging mobile app with Jetpack Compose
 * Author: EV Charging Team
 * Date: September 21, 2025
 */
package com.example.evchargingmobile.activities

import android.content.Intent
import android.content.SharedPreferences
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.animation.core.*
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.scale
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import kotlinx.coroutines.delay
import com.example.evchargingmobile.ui.theme.EVChargingTheme
import com.example.evchargingmobile.ui.theme.EVBlue
import com.example.evchargingmobile.ui.theme.EVLightBlue

class MainActivity : ComponentActivity() {

    private lateinit var sharedPreferences: SharedPreferences

    companion object {
        private const val SPLASH_DELAY = 2000L // 2 seconds
        private const val PREF_NAME = "EVChargingApp"
        private const val KEY_IS_LOGGED_IN = "isLoggedIn"
        private const val KEY_USER_NIC = "userNic"
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        // Initialize shared preferences for session management
        sharedPreferences = getSharedPreferences(PREF_NAME, MODE_PRIVATE)

        setContent {
            EVChargingTheme {
                SplashScreen(
                    onNavigate = ::navigateToNextScreen
                )
            }
        }
    }

    /**
     * Navigate to appropriate screen based on login status
     */
    private fun navigateToNextScreen() {
        val isLoggedIn = sharedPreferences.getBoolean(KEY_IS_LOGGED_IN, false)

        val intent = if (isLoggedIn) {
            // User is already logged in, go to dashboard
            Intent(this, DashboardActivity::class.java)
        } else {
            // User not logged in, go to login screen
            Intent(this, LoginActivity::class.java)
        }

        startActivity(intent)
        finish() // Close MainActivity
    }
}

@Composable
fun SplashScreen(onNavigate: () -> Unit) {
    // Animation states for a more engaging splash screen
    val infiniteTransition = rememberInfiniteTransition(label = "splash_animation")
    val scale by infiniteTransition.animateFloat(
        initialValue = 0.8f,
        targetValue = 1.2f,
        animationSpec = infiniteRepeatable(
            animation = tween(1000, easing = FastOutSlowInEasing),
            repeatMode = RepeatMode.Reverse
        ),
        label = "scale_animation"
    )

    LaunchedEffect(Unit) {
        delay(2000L) // Use constant instead of SPLASH_DELAY reference
        onNavigate()
    }

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(
                brush = androidx.compose.ui.graphics.Brush.verticalGradient(
                    colors = listOf(
                        EVBlue,
                        EVLightBlue
                    )
                )
            ),
        contentAlignment = Alignment.Center
    ) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center
        ) {
            // Animated logo container
            Card(
                modifier = Modifier
                    .size(140.dp)
                    .scale(scale),
                colors = CardDefaults.cardColors(containerColor = Color.White),
                elevation = CardDefaults.cardElevation(defaultElevation = 12.dp),
                shape = CircleShape
            ) {
                Box(
                    modifier = Modifier.fillMaxSize(),
                    contentAlignment = Alignment.Center
                ) {
                    Text(
                        text = "⚡",
                        fontSize = 56.sp,
                        color = EVBlue
                    )
                }
            }

            Spacer(modifier = Modifier.height(32.dp))

            Text(
                text = "EV Charging",
                fontSize = 36.sp,
                fontWeight = FontWeight.Bold,
                color = Color.White
            )

            Text(
                text = "Book Your Charge",
                fontSize = 18.sp,
                color = Color.White.copy(alpha = 0.9f),
                textAlign = TextAlign.Center,
                fontWeight = FontWeight.Light
            )

            Spacer(modifier = Modifier.height(48.dp))

            // Modern loading indicator
            CircularProgressIndicator(
                color = Color.White,
                strokeWidth = 4.dp,
                modifier = Modifier.size(32.dp)
            )

            Spacer(modifier = Modifier.height(16.dp))

            Text(
                text = "Initializing...",
                fontSize = 14.sp,
                color = Color.White.copy(alpha = 0.7f)
            )
        }
    }
}
