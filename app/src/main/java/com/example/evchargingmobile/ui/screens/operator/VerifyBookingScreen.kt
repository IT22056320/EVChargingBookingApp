/*
 * File: VerifyBookingScreen.kt
 * Description: Booking verification screen after QR scan
 * Author: EAD Team
 * Date: 2025-10-06
 */
package com.example.evchargingmobile.ui.screens.operator

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.evchargingmobile.models.Booking
import com.example.evchargingmobile.viewmodels.BookingUi
import com.example.evchargingmobile.viewmodels.OperatorUiState
import java.time.OffsetDateTime
import java.time.format.DateTimeFormatter
import java.util.*

/**
 * Screen to verify booking details before starting charging session
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun VerifyBookingScreen(
    bookingId: String,
    state: OperatorUiState,
    onVerify: (String) -> Unit,
    onStartSession: (String) -> Unit,
    onCancel: () -> Unit,
    onBack: () -> Unit
) {
    val scannedBooking = state.scannedBooking

    LaunchedEffect(bookingId) {
        if (scannedBooking == null) {
            onVerify(bookingId)
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Verify Booking") },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.Default.ArrowBack, "Back")
                    }
                }
            )
        }
    ) { padding ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            when {
                state.isLoading -> {
                    CircularProgressIndicator(
                        modifier = Modifier.align(Alignment.Center)
                    )
                }
                state.errorMessage != null -> {
                    ErrorContent(
                        error = state.errorMessage,
                        onRetry = { onVerify(bookingId) },
                        onCancel = onBack
                    )
                }
                scannedBooking != null -> {
                    BookingDetailsContent(
                        booking = scannedBooking,
                        onStartSession = { onStartSession(bookingId) },
                        onCancel = onBack
                    )
                }
            }
        }
    }
}

/**
 * Display booking details
 */
@Composable
private fun BookingDetailsContent(
    booking: BookingUi,
    onStartSession: () -> Unit,
    onCancel: () -> Unit
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        // Status Card
        Card(
            modifier = Modifier.fillMaxWidth(),
            colors = CardDefaults.cardColors(
                containerColor = when (booking.status.name) {
                    "Approved" -> MaterialTheme.colorScheme.primaryContainer
                    "InProgress" -> MaterialTheme.colorScheme.secondaryContainer
                    else -> MaterialTheme.colorScheme.surfaceVariant
                }
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
                        text = "Booking Status",
                        style = MaterialTheme.typography.bodySmall
                    )
                    Text(
                        text = booking.status.name,
                        style = MaterialTheme.typography.titleLarge,
                        fontWeight = FontWeight.Bold
                    )
                }
                Icon(
                    imageVector = when (booking.status.name) {
                        "Approved" -> Icons.Default.CheckCircle
                        "InProgress" -> Icons.Default.Info
                        else -> Icons.Default.Close
                    },
                    contentDescription = null,
                    modifier = Modifier.size(48.dp)
                )
            }
        }

        // Booking ID
        DetailItem(
            icon = Icons.Default.Info,
            label = "Booking ID",
            value = booking.bookingNumber
        )

        Divider()

        // EV Owner Details
        Text(
            text = "EV Owner Details",
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.Bold
        )

        // Note: User details not available in BookingUi model
        // Would need to fetch from API if required

        Divider()

        // Vehicle Details
        Text(
            text = "Vehicle Details",
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.Bold
        )

        DetailItem(
            icon = Icons.Default.Star,
            label = "Vehicle Number",
            value = booking.vehicleNumber
        )

        DetailItem(
            icon = Icons.Default.Star,
            label = "Vehicle Type",
            value = booking.vehicleType
        )

        Divider()

        // Booking Time Details
        Text(
            text = "Booking Schedule",
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.Bold
        )

        booking.startTime?.let { start ->
            DetailItem(
                icon = Icons.Default.DateRange,
                label = "Start Time",
                value = formatDateTime(start)
            )
        }

        booking.endTime?.let { end ->
            DetailItem(
                icon = Icons.Default.DateRange,
                label = "End Time",
                value = formatDateTime(end)
            )
        }

        DetailItem(
            icon = Icons.Default.Info,
            label = "Estimated Duration",
            value = "${booking.estimatedMinutes} minutes"
        )

        // Station Details
        Divider()
        Text(
            text = "Charging Station",
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.Bold
        )
        DetailItem(
            icon = Icons.Default.LocationOn,
            label = "Station",
            value = booking.stationName
        )
        if (booking.stationAddress != null) {
            DetailItem(
                icon = Icons.Default.LocationOn,
                label = "Address",
                value = booking.stationAddress
            )
        }

        // Notes
        if (!booking.notes.isNullOrBlank()) {
            Divider()
            DetailItem(
                icon = Icons.Default.Info,
                label = "Notes",
                value = booking.notes
            )
        }

        Spacer(modifier = Modifier.height(16.dp))

        // Action Buttons
        if (booking.status == Booking.BookingStatus.APPROVED) {
            Button(
                onClick = onStartSession,
                modifier = Modifier.fillMaxWidth()
            ) {
                Icon(Icons.Default.CheckCircle, contentDescription = null)
                Spacer(modifier = Modifier.width(8.dp))
                Text("Start Charging Session")
            }
        }

        OutlinedButton(
            onClick = onCancel,
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Cancel")
        }
    }
}

/**
 * Detail item row
 */
@Composable
private fun DetailItem(
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    label: String,
    value: String
) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        Icon(
            imageVector = icon,
            contentDescription = null,
            tint = MaterialTheme.colorScheme.primary
        )
        Column(modifier = Modifier.weight(1f)) {
            Text(
                text = label,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Text(
                text = value,
                style = MaterialTheme.typography.bodyLarge
            )
        }
    }
}

/**
 * Error content
 */
@Composable
private fun ErrorContent(
    error: String,
    onRetry: () -> Unit,
    onCancel: () -> Unit
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Icon(
            imageVector = Icons.Default.Close,
            contentDescription = null,
            modifier = Modifier.size(64.dp),
            tint = MaterialTheme.colorScheme.error
        )

        Spacer(modifier = Modifier.height(16.dp))

        Text(
            text = "Verification Failed",
            style = MaterialTheme.typography.headlineSmall,
            fontWeight = FontWeight.Bold
        )

        Spacer(modifier = Modifier.height(8.dp))

        Text(
            text = error,
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.error
        )

        Spacer(modifier = Modifier.height(24.dp))

        Button(
            onClick = onRetry,
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Try Again")
        }

        TextButton(onClick = onCancel) {
            Text("Cancel")
        }
    }
}

/**
 * Format date/time string
 */
private fun formatDateTime(dateTime: OffsetDateTime): String {
    return try {
        val formatter = DateTimeFormatter.ofPattern("MMM dd, yyyy hh:mm a")
        dateTime.format(formatter)
    } catch (e: Exception) {
        dateTime.toString()
    }
}
