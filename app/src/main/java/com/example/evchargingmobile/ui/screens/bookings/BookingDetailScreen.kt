package com.example.evchargingmobile.ui.screens.bookings

import android.graphics.BitmapFactory
import android.util.Base64
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.MutableState
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.composed
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.example.evchargingmobile.models.Booking
import com.example.evchargingmobile.viewmodels.BookingUi
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.LocalTime
import java.time.OffsetDateTime
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.Locale

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun BookingDetailScreen(
    booking: BookingUi?,
    successMessage: String?,
    errorMessage: String?,
    isProcessing: Boolean,
    onBack: () -> Unit,
    onCancel: (String, String) -> Unit,
    onUpdate: (String, OffsetDateTime, OffsetDateTime, String, String, Int, String?) -> Unit,
    onClearMessage: () -> Unit
) {
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(text = "Booking Details") },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(
                            imageVector = androidx.compose.material.icons.Icons.Filled.ArrowBack,
                            contentDescription = "Back"
                        )
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(containerColor = MaterialTheme.colorScheme.surface)
            )
        }
    ) { innerPadding ->
        if (booking == null) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(innerPadding),
                contentAlignment = Alignment.Center
            ) {
                Text(text = "Booking not found", style = MaterialTheme.typography.bodyLarge)
            }
            return@Scaffold
        }

        val scrollState = rememberScrollState()
        val showCancelDialog = remember { mutableStateOf(false) }
        val showRescheduleDialog = remember { mutableStateOf(false) }

        LaunchedEffect(successMessage, errorMessage) {
            if (!successMessage.isNullOrBlank() || !errorMessage.isNullOrBlank()) {
                // Auto clear messages after a short delay if needed
            }
        }

        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
                .background(Color(0xFFF4F6FA))
                .verticalScroll(scrollState)
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            if (!successMessage.isNullOrBlank()) {
                MessageBanner(message = successMessage, isError = false, onDismiss = onClearMessage)
            }
            if (!errorMessage.isNullOrBlank()) {
                MessageBanner(message = errorMessage, isError = true, onDismiss = onClearMessage)
            }

            BookingSummaryCard(booking)

            // Only show QR code for APPROVED bookings (not for Completed, Cancelled, etc.)
            if (booking.status == Booking.BookingStatus.APPROVED) {
                booking.qrCode?.takeIf { it.isNotBlank() }?.let { code ->
                    QRCodeSection(qrCode = code)
                }
            }

            ActionButtons(
                booking = booking,
                isProcessing = isProcessing,
                onReschedule = { showRescheduleDialog.value = true },
                onCancel = { showCancelDialog.value = true }
            )
        }

        if (showCancelDialog.value) {
            CancelBookingDialog(
                onDismiss = { showCancelDialog.value = false },
                onConfirm = { reason ->
                    showCancelDialog.value = false
                    onCancel(booking.id, reason)
                }
            )
        }

        if (showRescheduleDialog.value) {
            RescheduleDialog(
                booking = booking,
                onDismiss = { showRescheduleDialog.value = false },
                onConfirm = { start, end, vehicleNumber, vehicleType, minutes, notes ->
                    showRescheduleDialog.value = false
                    onUpdate(booking.id, start, end, vehicleNumber, vehicleType, minutes, notes)
                }
            )
        }
    }
}

@Composable
private fun BookingSummaryCard(booking: BookingUi) {
    val formatter = DateTimeFormatter.ofPattern("EEE, dd MMM yyyy • hh:mm a")

    Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        elevation = CardDefaults.cardElevation(defaultElevation = 4.dp)
    ) {
        Column(modifier = Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
            Text(text = booking.stationName, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
            booking.stationAddress?.let {
                Text(text = it, style = MaterialTheme.typography.bodySmall, color = Color.Gray)
            }

            DetailRow(label = "Status", value = booking.statusLabel)
            booking.startTime?.let {
                DetailRow(label = "Start", value = it.format(formatter))
            }
            booking.endTime?.let {
                DetailRow(label = "End", value = it.format(formatter))
            }
            DetailRow(label = "Duration", value = "${booking.estimatedMinutes} mins")
            DetailRow(label = "Vehicle", value = booking.vehicleNumber.ifBlank { "-" })
            DetailRow(label = "Type", value = booking.vehicleType.ifBlank { "-" })
            booking.totalCost?.let {
                DetailRow(label = "Estimated Cost", value = "Rs. %.2f".format(it))
            }
            booking.notes?.takeIf { it.isNotBlank() }?.let {
                DetailRow(label = "Notes", value = it)
            }
        }
    }
}

@Composable
private fun DetailRow(label: String, value: String) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Text(text = label, style = MaterialTheme.typography.bodySmall, color = Color.Gray)
        Text(text = value, style = MaterialTheme.typography.bodyMedium, fontWeight = FontWeight.Medium, textAlign = TextAlign.End)
    }
}

@Composable
private fun QRCodeSection(qrCode: String) {
    val bitmap = remember(qrCode) {
        runCatching {
            val decoded = Base64.decode(qrCode, Base64.DEFAULT)
            BitmapFactory.decodeByteArray(decoded, 0, decoded.size)?.asImageBitmap()
        }.getOrNull()
    }

    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(top = 8.dp),
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        elevation = CardDefaults.cardElevation(defaultElevation = 4.dp)
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Text(text = "Booking QR Code", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
            if (bitmap != null) {
                Image(bitmap = bitmap, contentDescription = "Booking QR code")
            } else {
                Text(text = "QR code unavailable", color = Color.Gray)
            }
        }
    }
}

@Composable
private fun ActionButtons(
    booking: BookingUi,
    isProcessing: Boolean,
    onReschedule: () -> Unit,
    onCancel: () -> Unit
) {
    Column(
        modifier = Modifier.fillMaxWidth(),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        Button(
            onClick = onReschedule,
            enabled = booking.canModify && !isProcessing,
            shape = RoundedCornerShape(12.dp),
            modifier = Modifier.fillMaxWidth()
        ) {
            Text(text = "Reschedule")
        }
        Button(
            onClick = onCancel,
            enabled = booking.canCancel && !isProcessing,
            shape = RoundedCornerShape(12.dp),
            modifier = Modifier.fillMaxWidth(),
            colors = ButtonDefaults.buttonColors(containerColor = Color(0xFFB71C1C), contentColor = Color.White)
        ) {
            Text(text = "Cancel Booking")
        }
    }
}

@Composable
private fun CancelBookingDialog(onDismiss: () -> Unit, onConfirm: (String) -> Unit) {
    val reasonState = rememberSaveable { mutableStateOf("") }
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(text = "Cancel Booking") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                Text(text = "Please provide a reason for cancellation")
                OutlinedTextField(
                    value = reasonState.value,
                    onValueChange = { reasonState.value = it },
                    placeholder = { Text(text = "Reason") },
                    modifier = Modifier.fillMaxWidth()
                )
            }
        },
        confirmButton = {
            TextButton(onClick = { onConfirm(reasonState.value) }, enabled = reasonState.value.length >= 5) {
                Text(text = "Confirm")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) {
                Text(text = "Dismiss")
            }
        }
    )
}

@Composable
private fun RescheduleDialog(
    booking: BookingUi,
    onDismiss: () -> Unit,
    onConfirm: (OffsetDateTime, OffsetDateTime, String, String, Int, String?) -> Unit
) {
    val currentStart = booking.startTime ?: OffsetDateTime.now().plusHours(12)
    val selectedDateEpoch = rememberSaveable { mutableStateOf(currentStart.toLocalDate().toEpochDay()) }
    val selectedTimeMinutes = rememberSaveable { mutableStateOf(currentStart.toLocalTime().toSecondOfDay() / 60) }
    val durationMinutes = rememberSaveable { mutableStateOf(booking.estimatedMinutes.coerceIn(30, 480)) }
    val vehicleNumber = rememberSaveable { mutableStateOf(booking.vehicleNumber) }
    val vehicleType = rememberSaveable { mutableStateOf(booking.vehicleType) }
    val notesState = rememberSaveable { mutableStateOf(booking.notes ?: "") }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(text = "Reschedule Booking") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                DatePickerField(
                    label = "Reservation Date",
                    epochDayState = selectedDateEpoch
                )
                TimePickerField(
                    label = "Start Time",
                    minutesState = selectedTimeMinutes
                )
                DurationPickerField(durationState = durationMinutes)
                OutlinedTextField(
                    value = vehicleNumber.value,
                    onValueChange = { vehicleNumber.value = it },
                    label = { Text("Vehicle Number") },
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = vehicleType.value,
                    onValueChange = { vehicleType.value = it },
                    label = { Text("Vehicle Type") },
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = notesState.value,
                    onValueChange = { notesState.value = it },
                    label = { Text("Notes (optional)") },
                    modifier = Modifier.fillMaxWidth()
                )
            }
        },
        confirmButton = {
            TextButton(onClick = {
                val date = LocalDate.ofEpochDay(selectedDateEpoch.value)
                val time = LocalTime.ofSecondOfDay((selectedTimeMinutes.value * 60).toLong())
                val start = OffsetDateTime.of(date, time, ZoneId.systemDefault().rules.getOffset(LocalDateTime.now()))
                val end = start.plusMinutes(durationMinutes.value.toLong())
                onConfirm(start, end, vehicleNumber.value, vehicleType.value, durationMinutes.value, notesState.value)
            }) {
                Text(text = "Confirm")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) {
                Text(text = "Dismiss")
            }
        }
    )
}

@Composable
private fun DatePickerField(label: String, epochDayState: MutableState<Long>) {
    val date = LocalDate.ofEpochDay(epochDayState.value)
    val formatted = date.format(DateTimeFormatter.ofPattern("EEE, dd MMM yyyy", Locale.getDefault()))
    val showPicker = remember { mutableStateOf(false) }

    OutlinedTextField(
        value = formatted,
        onValueChange = {},
        label = { Text(label) },
        modifier = Modifier
            .fillMaxWidth()
            .clickableWithoutRipple { showPicker.value = true },
        enabled = false
    )

    if (showPicker.value) {
        android.app.DatePickerDialog(
            androidx.compose.ui.platform.LocalContext.current,
            { _, year, month, day ->
                epochDayState.value = LocalDate.of(year, month + 1, day).toEpochDay()
            },
            date.year,
            date.monthValue - 1,
            date.dayOfMonth
        ).apply {
            setOnDismissListener { showPicker.value = false }
            show()
        }
    }
}

@Composable
private fun TimePickerField(label: String, minutesState: MutableState<Int>) {
    val minutes = minutesState.value
    val current = LocalTime.ofSecondOfDay((minutes * 60).toLong())
    val formatted = current.format(DateTimeFormatter.ofPattern("hh:mm a", Locale.getDefault()))
    val expanded = remember { mutableStateOf(false) }

    Column {
        OutlinedTextField(
            value = formatted,
            onValueChange = {},
            label = { Text(label) },
            modifier = Modifier
                .fillMaxWidth()
                .clickableWithoutRipple { expanded.value = true },
            enabled = false
        )
        DropdownMenu(expanded = expanded.value, onDismissRequest = { expanded.value = false }) {
            generateTimeSlots().forEach { time ->
                DropdownMenuItem(
                    text = { Text(time.first) },
                    onClick = {
                        minutesState.value = time.second
                        expanded.value = false
                    }
                )
            }
        }
    }
}

@Composable
private fun DurationPickerField(durationState: MutableState<Int>) {
    val durations = listOf(30, 45, 60, 90, 120, 180)
    val expanded = remember { mutableStateOf(false) }
    Column {
        OutlinedTextField(
            value = "${durationState.value} minutes",
            onValueChange = {},
            label = { Text("Estimated Duration") },
            modifier = Modifier
                .fillMaxWidth()
                .clickableWithoutRipple { expanded.value = true },
            enabled = false
        )
        DropdownMenu(expanded = expanded.value, onDismissRequest = { expanded.value = false }) {
            durations.forEach { minutes ->
                DropdownMenuItem(
                    text = { Text("$minutes minutes") },
                    onClick = {
                        durationState.value = minutes
                        expanded.value = false
                    }
                )
            }
        }
    }
}

private fun generateTimeSlots(): List<Pair<String, Int>> {
    val formatter = DateTimeFormatter.ofPattern("hh:mm a", Locale.getDefault())
    val slots = mutableListOf<Pair<String, Int>>()
    var time = LocalTime.MIDNIGHT
    while (time != LocalTime.MIDNIGHT || slots.isEmpty()) {
        slots.add(formatter.format(time) to (time.toSecondOfDay() / 60))
        time = time.plusMinutes(30)
        if (time == LocalTime.MIDNIGHT) break
    }
    return slots
}

@Composable
private fun MessageBanner(message: String, isError: Boolean, onDismiss: () -> Unit) {
    val background = if (isError) Color(0xFFFFE8E9) else Color(0xFFE8FFF3)
    val textColor = if (isError) Color(0xFF8C1D1D) else Color(0xFF1A7345)

    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(containerColor = background),
        shape = RoundedCornerShape(12.dp)
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(12.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Text(text = message, color = textColor)
            TextButton(onClick = onDismiss) {
                Text(text = "Dismiss", color = textColor)
            }
        }
    }
}

private fun Modifier.clickableWithoutRipple(onClick: () -> Unit): Modifier = composed {
    this.then(Modifier.background(Color.Transparent)).clickable(onClick = onClick)
}




