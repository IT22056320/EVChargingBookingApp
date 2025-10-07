/*
 * File: CompleteSessionScreen.kt
 * Description: Screen to complete charging session with energy and notes
 * Author: EAD Team
 * Date: 2025-10-06
 */
package com.example.evchargingmobile.ui.screens.operator

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import com.example.evchargingmobile.viewmodels.OperatorUiState

/**
 * Screen to complete a charging session
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CompleteSessionScreen(
    bookingId: String,
    state: OperatorUiState,
    onComplete: (String, Double, String) -> Unit,
    onBack: () -> Unit
) {
    var energyConsumed by remember { mutableStateOf("") }
    var notes by remember { mutableStateOf("") }
    var energyError by remember { mutableStateOf<String?>(null) }

    val booking = state.scannedBooking

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Complete Session") },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.Default.ArrowBack, "Back")
                    }
                }
            )
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .verticalScroll(rememberScrollState())
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            // Session Summary Card
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(
                    containerColor = MaterialTheme.colorScheme.primaryContainer
                )
            ) {
                Column(
                    modifier = Modifier.padding(16.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    Text(
                        text = "Session Summary",
                        style = MaterialTheme.typography.titleLarge,
                        fontWeight = FontWeight.Bold
                    )
                    
                    if (booking != null) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Text(
                                text = "Booking Number",
                                style = MaterialTheme.typography.bodyMedium
                            )
                            Text(
                                text = booking.bookingNumber,
                                style = MaterialTheme.typography.bodyMedium,
                                fontWeight = FontWeight.Bold
                            )
                        }
                        
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Text(
                                text = "Vehicle",
                                style = MaterialTheme.typography.bodyMedium
                            )
                            Text(
                                text = booking.vehicleNumber,
                                style = MaterialTheme.typography.bodyMedium,
                                fontWeight = FontWeight.Bold
                            )
                        }
                        
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Text(
                                text = "Duration",
                                style = MaterialTheme.typography.bodyMedium
                            )
                            Text(
                                text = "${booking.estimatedMinutes} min",
                                style = MaterialTheme.typography.bodyMedium,
                                fontWeight = FontWeight.Bold
                            )
                        }
                    }
                }
            }

            // Energy Consumed Input
            OutlinedTextField(
                value = energyConsumed,
                onValueChange = {
                    energyConsumed = it
                    energyError = validateEnergy(it)
                },
                label = { Text("Energy Consumed (kWh)") },
                leadingIcon = {
                    Icon(Icons.Default.Star, contentDescription = null)
                },
                placeholder = { Text("Enter kWh consumed") },
                keyboardOptions = KeyboardOptions(
                    keyboardType = KeyboardType.Decimal
                ),
                isError = energyError != null,
                supportingText = energyError?.let { { Text(it) } },
                modifier = Modifier.fillMaxWidth(),
                singleLine = true
            )

            // Estimated Cost Display
            if (energyConsumed.toDoubleOrNull() != null && energyError == null) {
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.secondaryContainer
                    )
                ) {
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(16.dp),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column {
                            Text(
                                text = "Estimated Cost",
                                style = MaterialTheme.typography.bodyMedium
                            )
                            Text(
                                text = "LKR ${calculateCost(energyConsumed.toDouble())}",
                                style = MaterialTheme.typography.headlineMedium,
                                fontWeight = FontWeight.Bold
                            )
                        }
                        Icon(
                            imageVector = Icons.Default.Info,
                            contentDescription = null,
                            modifier = Modifier.size(48.dp),
                            tint = MaterialTheme.colorScheme.onSecondaryContainer
                        )
                    }
                }
            }

            // Notes Input
            OutlinedTextField(
                value = notes,
                onValueChange = { notes = it },
                label = { Text("Notes (Optional)") },
                leadingIcon = {
                    Icon(Icons.Default.Info, contentDescription = null)
                },
                placeholder = { Text("Add any additional notes") },
                modifier = Modifier
                    .fillMaxWidth()
                    .height(120.dp),
                maxLines = 4
            )

            Spacer(modifier = Modifier.height(8.dp))

            // Complete Button
            Button(
                onClick = {
                    val energy = energyConsumed.toDoubleOrNull()
                    if (energy != null && energy > 0) {
                        onComplete(bookingId, energy, notes)
                    }
                },
                modifier = Modifier.fillMaxWidth(),
                enabled = !state.isLoading && energyConsumed.toDoubleOrNull() != null && energyError == null
            ) {
                if (state.isLoading) {
                    CircularProgressIndicator(
                        modifier = Modifier.size(24.dp),
                        color = MaterialTheme.colorScheme.onPrimary
                    )
                } else {
                    Icon(Icons.Default.CheckCircle, contentDescription = null)
                    Spacer(modifier = Modifier.width(8.dp))
                    Text("Complete Session")
                }
            }

            // Cancel Button
            OutlinedButton(
                onClick = onBack,
                modifier = Modifier.fillMaxWidth(),
                enabled = !state.isLoading
            ) {
                Text("Cancel")
            }

            // Error Message
            if (state.errorMessage != null) {
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.errorContainer
                    )
                ) {
                    Row(
                        modifier = Modifier.padding(16.dp),
                        horizontalArrangement = Arrangement.spacedBy(12.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Icon(
                            imageVector = Icons.Default.Close,
                            contentDescription = null,
                            tint = MaterialTheme.colorScheme.onErrorContainer
                        )
                        Text(
                            text = state.errorMessage,
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onErrorContainer
                        )
                    }
                }
            }

            // Success Message
            if (state.successMessage != null && state.successMessage.contains("completed", ignoreCase = true)) {
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.primaryContainer
                    )
                ) {
                    Row(
                        modifier = Modifier.padding(16.dp),
                        horizontalArrangement = Arrangement.spacedBy(12.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Icon(
                            imageVector = Icons.Default.CheckCircle,
                            contentDescription = null,
                            tint = MaterialTheme.colorScheme.onPrimaryContainer
                        )
                        Text(
                            text = state.successMessage,
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onPrimaryContainer
                        )
                    }
                }

                // Auto-navigate back on success ONLY for completion
                LaunchedEffect(state.successMessage) {
                    kotlinx.coroutines.delay(2000)
                    onBack()
                }
            }
        }
    }
}

/**
 * Validate energy input
 */
private fun validateEnergy(value: String): String? {
    return when {
        value.isBlank() -> "Energy consumed is required"
        value.toDoubleOrNull() == null -> "Please enter a valid number"
        value.toDouble() <= 0 -> "Energy must be greater than 0"
        value.toDouble() > 1000 -> "Energy seems unreasonably high"
        else -> null
    }
}

/**
 * Calculate estimated cost
 * Rate: LKR 50 per kWh (example rate)
 */
private fun calculateCost(energy: Double): String {
    val ratePerKWh = 50.0
    val cost = energy * ratePerKWh
    return String.format("%.2f", cost)
}
