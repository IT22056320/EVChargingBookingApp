/*
 * File: RegisterActivity.kt
 * Description: EV Owner self-registration activity with Jetpack Compose
 * Author: EV Charging Team
 * Date: September 21, 2025
 */
package com.example.evchargingmobile.activities

import android.os.Bundle
import android.util.Patterns
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
import com.example.evchargingmobile.ui.theme.EVGreen
import kotlinx.coroutines.launch

class RegisterActivity : ComponentActivity() {

    private lateinit var apiService: ApiService
    private lateinit var databaseHelper: DatabaseHelper

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        initializeServices()

        setContent {
            EVChargingTheme {
                RegisterScreen(
                    onRegister = ::performRegistration,
                    onBackToLogin = { finish() }
                )
            }
        }
    }

    /**
     * Initialize services
     */
    private fun initializeServices() {
        apiService = ApiService.getInstance()
        databaseHelper = DatabaseHelper(this)
    }

    /**
     * Perform user registration using Kotlin coroutines
     */
    private fun performRegistration(
        nic: String,
        fullName: String,
        email: String,
        password: String,
        phone: String,
        address: String,
        onResult: (Boolean, String) -> Unit
    ) {
        val user = User(
            nic = nic,
            fullName = fullName,
            email = email,
            password = password,
            phoneNumber = phone,
            address = address,
            userType = User.UserType.EV_OWNER
        )

        lifecycleScope.launch {
            try {
                val result = apiService.registerEVOwner(user)

                result.fold(
                    onSuccess = { response ->
                        response.user?.let { databaseHelper.saveUser(it) }
                        onResult(true, "Registration successful! Your account is pending approval.")
                    },
                    onFailure = { exception ->
                        onResult(false, "Registration failed: ${exception.message}")
                    }
                )
            } catch (e: Exception) {
                onResult(false, "Registration error: ${e.message}")
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun RegisterScreen(
    onRegister: (String, String, String, String, String, String, (Boolean, String) -> Unit) -> Unit,
    onBackToLogin: () -> Unit
) {
    var nic by remember { mutableStateOf("") }
    var fullName by remember { mutableStateOf("") }
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var confirmPassword by remember { mutableStateOf("") }
    var phone by remember { mutableStateOf("") }
    var address by remember { mutableStateOf("") }
    var passwordVisible by remember { mutableStateOf(false) }
    var confirmPasswordVisible by remember { mutableStateOf(false) }
    var isLoading by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf("") }
    var showSuccessDialog by remember { mutableStateOf(false) }

    val context = LocalContext.current

    // Success Dialog
    if (showSuccessDialog) {
        AlertDialog(
            onDismissRequest = { },
            title = { Text("Registration Successful") },
            text = {
                Text("Your account has been created successfully! Your account is pending approval from our back-office team. You will be able to login once approved.")
            },
            confirmButton = {
                TextButton(
                    onClick = {
                        showSuccessDialog = false
                        onBackToLogin()
                    }
                ) {
                    Text("OK")
                }
            }
        )
    }

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
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            // Header Section
            Card(
                modifier = Modifier.size(80.dp),
                colors = CardDefaults.cardColors(containerColor = EVGreen),
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
                text = "Create Account",
                fontSize = 28.sp,
                fontWeight = FontWeight.Bold,
                color = EVBlue
            )

            Text(
                text = "Join the EV Charging community",
                fontSize = 16.sp,
                color = Color.Gray,
                textAlign = TextAlign.Center
            )

            Spacer(modifier = Modifier.height(32.dp))

            // Registration Form
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(containerColor = Color.White),
                elevation = CardDefaults.cardElevation(defaultElevation = 8.dp)
            ) {
                Column(
                    modifier = Modifier.padding(24.dp),
                    verticalArrangement = Arrangement.spacedBy(16.dp)
                ) {
                    // NIC Input
                    OutlinedTextField(
                        value = nic,
                        onValueChange = { nic = it },
                        label = { Text("NIC Number") },
                        placeholder = { Text("Enter your NIC") },
                        modifier = Modifier.fillMaxWidth(),
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Text),
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedBorderColor = EVBlue,
                            focusedLabelColor = EVBlue
                        ),
                        singleLine = true
                    )

                    // Full Name Input
                    OutlinedTextField(
                        value = fullName,
                        onValueChange = { fullName = it },
                        label = { Text("Full Name") },
                        placeholder = { Text("Enter your full name") },
                        modifier = Modifier.fillMaxWidth(),
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Text),
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedBorderColor = EVBlue,
                            focusedLabelColor = EVBlue
                        ),
                        singleLine = true
                    )

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

                    // Phone Input
                    OutlinedTextField(
                        value = phone,
                        onValueChange = { phone = it },
                        label = { Text("Phone Number") },
                        placeholder = { Text("Enter your phone number") },
                        modifier = Modifier.fillMaxWidth(),
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Phone),
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedBorderColor = EVBlue,
                            focusedLabelColor = EVBlue
                        ),
                        singleLine = true
                    )

                    // Address Input
                    OutlinedTextField(
                        value = address,
                        onValueChange = { address = it },
                        label = { Text("Address") },
                        placeholder = { Text("Enter your address") },
                        modifier = Modifier.fillMaxWidth(),
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Text),
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedBorderColor = EVBlue,
                            focusedLabelColor = EVBlue
                        ),
                        minLines = 2,
                        maxLines = 3
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
                                    imageVector = if (passwordVisible) Icons.Outlined.Lock else Icons.Default.Lock,
                                    contentDescription = if (passwordVisible) "Hide password" else "Show password"
                                )
                            }
                        },
                        singleLine = true
                    )

                    // Confirm Password Input
                    OutlinedTextField(
                        value = confirmPassword,
                        onValueChange = { confirmPassword = it },
                        label = { Text("Confirm Password") },
                        placeholder = { Text("Re-enter your password") },
                        modifier = Modifier.fillMaxWidth(),
                        visualTransformation = if (confirmPasswordVisible) VisualTransformation.None else PasswordVisualTransformation(),
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password),
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedBorderColor = EVBlue,
                            focusedLabelColor = EVBlue
                        ),
                        trailingIcon = {
                            IconButton(onClick = { confirmPasswordVisible = !confirmPasswordVisible }) {
                                Icon(
                                    imageVector = if (confirmPasswordVisible) Icons.Outlined.Lock else Icons.Default.Lock,
                                    contentDescription = if (confirmPasswordVisible) "Hide password" else "Show password"
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

                    // Register Button
                    Button(
                        onClick = {
                            // Validation
                            when {
                                nic.isEmpty() -> errorMessage = "NIC is required"
                                nic.length < 10 -> errorMessage = "Invalid NIC format"
                                fullName.isEmpty() -> errorMessage = "Full name is required"
                                email.isEmpty() -> errorMessage = "Email is required"
                                !Patterns.EMAIL_ADDRESS.matcher(email).matches() -> errorMessage = "Invalid email format"
                                password.isEmpty() -> errorMessage = "Password is required"
                                password.length < 6 -> errorMessage = "Password must be at least 6 characters"
                                password != confirmPassword -> errorMessage = "Passwords do not match"
                                phone.isEmpty() -> errorMessage = "Phone number is required"
                                address.isEmpty() -> errorMessage = "Address is required"
                                else -> {
                                    errorMessage = ""
                                    isLoading = true
                                    onRegister(nic, fullName, email, password, phone, address) { success, message ->
                                        isLoading = false
                                        if (success) {
                                            showSuccessDialog = true
                                        } else {
                                            errorMessage = message
                                        }
                                    }
                                }
                            }
                        },
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(56.dp),
                        colors = ButtonDefaults.buttonColors(containerColor = EVGreen),
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
                                text = "Create Account",
                                fontSize = 16.sp,
                                fontWeight = FontWeight.Medium
                            )
                        }
                    }

                    // Back to Login Button
                    OutlinedButton(
                        onClick = onBackToLogin,
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(56.dp),
                        colors = ButtonDefaults.outlinedButtonColors(
                            contentColor = EVBlue
                        )
                    ) {
                        Text(
                            text = "Back to Login",
                            fontSize = 16.sp,
                            fontWeight = FontWeight.Medium
                        )
                    }
                }
            }

            Spacer(modifier = Modifier.height(24.dp))
        }
    }
}
