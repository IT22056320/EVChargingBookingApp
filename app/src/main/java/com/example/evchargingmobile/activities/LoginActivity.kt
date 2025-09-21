/*
 * File: LoginActivity.kt
 * Description: Login activity for EV owners and station operators with Jetpack Compose
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
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.outlined.Lock
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.lifecycleScope
import com.example.evchargingmobile.database.DatabaseHelper
import com.example.evchargingmobile.models.User
import com.example.evchargingmobile.network.ApiService
import com.example.evchargingmobile.ui.theme.EVChargingTheme
import com.example.evchargingmobile.ui.theme.EVBlue
import com.example.evchargingmobile.ui.theme.EVLightBlue
import kotlinx.coroutines.launch

class LoginActivity : ComponentActivity() {

    private lateinit var apiService: ApiService
    private lateinit var databaseHelper: DatabaseHelper
    private lateinit var sharedPreferences: SharedPreferences

    companion object {
        private const val PREF_NAME = "EVChargingApp"
        private const val KEY_IS_LOGGED_IN = "isLoggedIn"
        private const val KEY_USER_EMAIL = "userEmail"
        private const val KEY_USER_TYPE = "userType"
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        initializeServices()

        setContent {
            EVChargingTheme {
                LoginScreen(
                    onLogin = ::performLogin,
                    onRegister = ::openRegistration
                )
            }
        }
    }

    /**
     * Initialize services and helpers
     */
    private fun initializeServices() {
        apiService = ApiService.getInstance()
        databaseHelper = DatabaseHelper(this)
        sharedPreferences = getSharedPreferences(PREF_NAME, MODE_PRIVATE)
    }

    /**
     * Perform unified user login for both EV owners and station operators
     */
    private fun performLogin(email: String, password: String, onResult: (Boolean, String) -> Unit) {
        lifecycleScope.launch {
            try {
                val result = apiService.loginUser(email, password)

                result.fold(
                    onSuccess = { loginResponse ->
                        handleSuccessfulLogin(email, loginResponse)
                        onResult(true, "Login successful!")
                    },
                    onFailure = { exception ->
                        onResult(false, exception.message ?: "Login failed")
                    }
                )
            } catch (e: Exception) {
                onResult(false, "Login error: ${e.message}")
            }
        }
    }

    /**
     * Handle successful login response - store user data and navigate based on user type
     */
    private fun handleSuccessfulLogin(email: String, result: ApiService.UnifiedLoginResponse) {
        try {
            sharedPreferences.edit().apply {
                putBoolean(KEY_IS_LOGGED_IN, true)
                putString(KEY_USER_EMAIL, email)
                putString(KEY_USER_TYPE, result.userType)
                apply()
            }

            val intent = Intent(this@LoginActivity, DashboardActivity::class.java).apply {
                flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
            }
            startActivity(intent)
            finish()

        } catch (e: Exception) {
            Toast.makeText(this, "Failed to process login: ${e.message}", Toast.LENGTH_LONG).show()
        }
    }

    /**
     * Open registration activity
     */
    private fun openRegistration() {
        val intent = Intent(this, RegisterActivity::class.java)
        startActivity(intent)
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun LoginScreen(
    onLogin: (String, String, (Boolean, String) -> Unit) -> Unit,
    onRegister: () -> Unit
) {
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var passwordVisible by remember { mutableStateOf(false) }
    var isLoading by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf("") }

    val context = LocalContext.current

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(
                brush = androidx.compose.ui.graphics.Brush.verticalGradient(
                    colors = listOf(
                        EVBlue.copy(alpha = 0.1f),
                        Color.White
                    )
                )
            )
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(24.dp)
                .verticalScroll(rememberScrollState()),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center
        ) {
            // Header Section
            Card(
                modifier = Modifier.size(80.dp),
                colors = CardDefaults.cardColors(containerColor = EVBlue),
                elevation = CardDefaults.cardElevation(defaultElevation = 8.dp)
            ) {
                Box(
                    modifier = Modifier.fillMaxSize(),
                    contentAlignment = Alignment.Center
                ) {
                    Text(
                        text = "⚡",
                        fontSize = 32.sp,
                        color = Color.White
                    )
                }
            }

            Spacer(modifier = Modifier.height(24.dp))

            Text(
                text = "Welcome Back",
                fontSize = 28.sp,
                fontWeight = FontWeight.Bold,
                color = EVBlue
            )

            Text(
                text = "Sign in to your EV Charging account",
                fontSize = 16.sp,
                color = Color.Gray,
                textAlign = TextAlign.Center
            )

            Spacer(modifier = Modifier.height(32.dp))

            // Login Form
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(containerColor = Color.White),
                elevation = CardDefaults.cardElevation(defaultElevation = 8.dp)
            ) {
                Column(
                    modifier = Modifier.padding(24.dp),
                    verticalArrangement = Arrangement.spacedBy(16.dp)
                ) {
                    // Email Input
                    OutlinedTextField(
                        value = email,
                        onValueChange = { email = it },
                        label = { Text("Email Address") },
                        placeholder = { Text("Enter your email") },
                        modifier = Modifier.fillMaxWidth(),
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Email),
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedBorderColor = EVBlue,
                            focusedLabelColor = EVBlue
                        ),
                        singleLine = true
                    )

                    // Password Input
                    OutlinedTextField(
                        value = password,
                        onValueChange = { password = it },
                        label = { Text("Password") },
                        placeholder = { Text("Enter your password") },
                        modifier = Modifier.fillMaxWidth(),
                        visualTransformation = if (passwordVisible) VisualTransformation.None else PasswordVisualTransformation(),
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password),
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedBorderColor = EVBlue,
                            focusedLabelColor = EVBlue
                        ),
                        trailingIcon = {
                            IconButton(onClick = { passwordVisible = !passwordVisible }) {
                                Icon(
                                    imageVector = if (passwordVisible) Icons.Default.Lock else Icons.Outlined.Lock,
                                    contentDescription = if (passwordVisible) "Hide password" else "Show password"
                                )
                            }
                        },
                        singleLine = true
                    )

                    // Error Message
                    if (errorMessage.isNotEmpty()) {
                        Text(
                            text = errorMessage,
                            color = MaterialTheme.colorScheme.error,
                            fontSize = 14.sp
                        )
                    }

                    Spacer(modifier = Modifier.height(8.dp))

                    // Login Button
                    Button(
                        onClick = {
                            when {
                                email.isEmpty() -> errorMessage = "Email is required"
                                password.isEmpty() -> errorMessage = "Password is required"
                                else -> {
                                    errorMessage = ""
                                    isLoading = true
                                    onLogin(email, password) { success, message ->
                                        isLoading = false
                                        if (!success) {
                                            errorMessage = message
                                        } else {
                                            Toast.makeText(context, message, Toast.LENGTH_SHORT).show()
                                        }
                                    }
                                }
                            }
                        },
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(56.dp),
                        colors = ButtonDefaults.buttonColors(containerColor = EVBlue),
                        enabled = !isLoading
                    ) {
                        if (isLoading) {
                            CircularProgressIndicator(
                                color = Color.White,
                                strokeWidth = 2.dp,
                                modifier = Modifier.size(20.dp)
                            )
                        } else {
                            Text(
                                text = "Sign In",
                                fontSize = 16.sp,
                                fontWeight = FontWeight.Medium
                            )
                        }
                    }
                }
            }

            Spacer(modifier = Modifier.height(24.dp))

            // Register Link
            Row(
                horizontalArrangement = Arrangement.Center,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = "Don't have an account? ",
                    color = Color.Gray
                )
                TextButton(onClick = onRegister) {
                    Text(
                        text = "Register",
                        color = EVBlue,
                        fontWeight = FontWeight.Medium
                    )
                }
            }
        }
    }
}
