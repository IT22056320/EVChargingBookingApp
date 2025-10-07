/*
 * File: EditProfileScreen.kt
 * Description: Edit profile screen for EV Owners
 * Author: EV Charging Team
 * Date: December 2024
 */
package com.example.evchargingmobile.ui.screens.profile

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Check
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.evchargingmobile.ui.components.LocationPickerField
import com.example.evchargingmobile.ui.components.LocationData
import com.example.evchargingmobile.ui.theme.EVBlue
import com.example.evchargingmobile.ui.theme.EVGreen

/**
 * Edit Profile Screen
 * Allows users to update their phone number and address with location
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun EditProfileScreen(
    userNic: String,
    userName: String,
    userEmail: String,
    initialPhone: String?,
    initialAddress: String?,
    initialLatitude: Double?,
    initialLongitude: Double?,
    onBack: () -> Unit,
    onSave: (phone: String, address: String, latitude: Double?, longitude: Double?) -> Unit,
    modifier: Modifier = Modifier
) {
    var phoneNumber by remember { mutableStateOf(initialPhone ?: "") }
    var address by remember { mutableStateOf(initialAddress ?: "") }
    var latitude by remember { mutableStateOf(initialLatitude) }
    var longitude by remember { mutableStateOf(initialLongitude) }
    var isLoading by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf("") }
    var showSuccessDialog by remember { mutableStateOf(false) }

    // Success Dialog
    if (showSuccessDialog) {
        AlertDialog(
            onDismissRequest = { },
            title = { Text("Profile Updated") },
            text = {
                Text("Your profile has been updated successfully!")
            },
            confirmButton = {
                TextButton(
                    onClick = {
                        showSuccessDialog = false
                        onBack()
                    }
                ) {
                    Text("OK")
                }
            }
        )
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        "Edit Profile",
                        color = Color.White,
                        fontWeight = FontWeight.Bold
                    )
                },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(
                            Icons.Default.ArrowBack,
                            contentDescription = "Back",
                            tint = Color.White
                        )
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = EVBlue
                )
            )
        }
    ) { paddingValues ->
        Box(
            modifier = modifier
                .fillMaxSize()
                .padding(paddingValues)
                .background(
                    brush = androidx.compose.ui.graphics.Brush.verticalGradient(
                        colors = listOf(
                            EVBlue.copy(alpha = 0.05f),
                            Color.White
                        )
                    )
                )
        ) {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(16.dp)
                    .verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(16.dp)
            ) {
                // Info Card
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(containerColor = Color.White),
                    elevation = CardDefaults.cardElevation(defaultElevation = 4.dp)
                ) {
                    Column(
                        modifier = Modifier.padding(16.dp),
                        verticalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        Text(
                            text = "Account Information",
                            fontSize = 16.sp,
                            fontWeight = FontWeight.Bold,
                            color = EVBlue
                        )
                        
                        Divider(modifier = Modifier.padding(vertical = 8.dp))
                        
                        InfoRow(label = "NIC", value = userNic)
                        InfoRow(label = "Full Name", value = userName)
                        InfoRow(label = "Email", value = userEmail)
                        
                        Text(
                            text = "These fields cannot be changed",
                            fontSize = 12.sp,
                            color = Color.Gray,
                            modifier = Modifier.padding(top = 4.dp)
                        )
                    }
                }

                // Editable Fields Card
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(containerColor = Color.White),
                    elevation = CardDefaults.cardElevation(defaultElevation = 4.dp)
                ) {
                    Column(
                        modifier = Modifier.padding(16.dp),
                        verticalArrangement = Arrangement.spacedBy(16.dp)
                    ) {
                        Text(
                            text = "Editable Information",
                            fontSize = 16.sp,
                            fontWeight = FontWeight.Bold,
                            color = EVBlue
                        )

                        // Phone Number Input
                        OutlinedTextField(
                            value = phoneNumber,
                            onValueChange = { phoneNumber = it },
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

                        // Address with Location Picker
                        LocationPickerField(
                            label = "Address",
                            placeholder = "Select your location",
                            initialAddress = address,
                            onLocationSelected = { locationData ->
                                address = locationData.address
                                latitude = locationData.latitude
                                longitude = locationData.longitude
                            },
                            modifier = Modifier.fillMaxWidth()
                        )

                        // Error Message
                        if (errorMessage.isNotEmpty()) {
                            Text(
                                text = errorMessage,
                                color = MaterialTheme.colorScheme.error,
                                fontSize = 14.sp
                            )
                        }

                        // Save Button
                        Button(
                            onClick = {
                                when {
                                    phoneNumber.isEmpty() -> errorMessage = "Phone number is required"
                                    address.isEmpty() -> errorMessage = "Address is required"
                                    else -> {
                                        errorMessage = ""
                                        isLoading = true
                                        onSave(phoneNumber, address, latitude, longitude)
                                        // Show success dialog
                                        isLoading = false
                                        showSuccessDialog = true
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
                                    modifier = Modifier.size(24.dp)
                                )
                            } else {
                                Icon(
                                    imageVector = Icons.Default.Check,
                                    contentDescription = null,
                                    modifier = Modifier.padding(end = 8.dp)
                                )
                                Text("Save Changes", fontSize = 16.sp)
                            }
                        }
                    }
                }
            }
        }
    }
}

/**
 * Helper composable for displaying read-only info rows
 */
@Composable
private fun InfoRow(
    label: String,
    value: String
) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Text(
            text = label,
            fontSize = 14.sp,
            color = Color.Gray,
            modifier = Modifier.weight(0.4f)
        )
        Text(
            text = value,
            fontSize = 14.sp,
            fontWeight = FontWeight.Medium,
            modifier = Modifier.weight(0.6f)
        )
    }
}
