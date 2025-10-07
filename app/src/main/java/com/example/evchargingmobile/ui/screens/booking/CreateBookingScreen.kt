package com.example.evchargingmobile.ui.screens.booking

import android.app.DatePickerDialog
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Divider
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.ui.graphics.Color
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.evchargingmobile.network.ApiService
import com.example.evchargingmobile.viewmodels.StationUiModel
import kotlinx.coroutines.launch
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.LocalTime
import java.time.OffsetDateTime
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.Locale

@OptIn(ExperimentalMaterial3Api::class)

@Composable
fun CreateBookingScreen(
    stations: List<StationUiModel>,
    isSubmitting: Boolean,
    onBack: () -> Unit,
    onSubmit: (String, OffsetDateTime, OffsetDateTime, String, String, Int, String?, (Boolean, String) -> Unit) -> Unit,
    onValidateTimeSlot: suspend (String, OffsetDateTime, OffsetDateTime) -> Result<ApiService.TimeSlotAvailabilityResponse>
) {
    val scope = rememberCoroutineScope()
    var currentStep by rememberSaveable { mutableStateOf(0) }
    var formError by rememberSaveable { mutableStateOf<String?>(null) }

    var selectedStationId by rememberSaveable { mutableStateOf<String?>(null) }
    var selectedDateEpochDay by rememberSaveable { mutableStateOf<Long?>(null) }
    var selectedTimeMinutes by rememberSaveable { mutableStateOf<Int?>(null) }
    var durationMinutes by rememberSaveable { mutableStateOf(60) }
    var vehicleNumber by rememberSaveable { mutableStateOf("") }
    var vehicleType by rememberSaveable { mutableStateOf("") }
    var notes by rememberSaveable { mutableStateOf("") }
    var submitMessage by rememberSaveable { mutableStateOf<String?>(null) }

    LaunchedEffect(stations) {
        if (selectedStationId == null && stations.isNotEmpty()) {
            selectedStationId = stations.first().id
        }
        if (selectedDateEpochDay == null) {
            val defaultDate = OffsetDateTime.now().plusHours(12).toLocalDate()
            selectedDateEpochDay = defaultDate.toEpochDay()
        }
        if (selectedTimeMinutes == null) {
            val defaultTime = OffsetDateTime.now().plusHours(12).toLocalTime()
            selectedTimeMinutes = defaultTime.toSecondOfDay() / 60
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(text = "New Booking") },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(imageVector = Icons.Filled.ArrowBack, contentDescription = "Back")
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(containerColor = MaterialTheme.colorScheme.surface)
            )
        }
    ) { innerPadding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
                .background(Color(0xFFF4F6FA))
        ) {
            // Fixed header section
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(16.dp)
            ) {
                submitMessage?.let { message ->
                    MessageBanner(message = message, isError = false) { submitMessage = null }
                }
                formError?.let { error ->
                    MessageBanner(message = error, isError = true) { formError = null }
                }

                StepIndicator(currentStep = currentStep)
            }

            // Scrollable content area
            Box(
                modifier = Modifier
                    .weight(1f)
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp)
            ) {
                when (currentStep) {
                    0 -> StationSelectionStep(
                        stations = stations,
                        selectedStationId = selectedStationId,
                        onStationSelected = { selectedStationId = it }
                    )
                    1 -> DateTimeSelectionStep(
                        selectedDateEpochDay = selectedDateEpochDay,
                        selectedTimeMinutes = selectedTimeMinutes,
                        durationMinutes = durationMinutes,
                        onDateSelected = { selectedDateEpochDay = it },
                        onTimeSelected = { selectedTimeMinutes = it },
                        onDurationSelected = { durationMinutes = it }
                    )
                    2 -> VehicleDetailsStep(
                        vehicleNumber = vehicleNumber,
                        vehicleType = vehicleType,
                        notes = notes,
                        onVehicleNumberChanged = { vehicleNumber = it },
                        onVehicleTypeChanged = { vehicleType = it },
                        onNotesChanged = { notes = it }
                    )
                    3 -> SummaryStep(
                        station = stations.find { it.id == selectedStationId },
                        selectedDateEpochDay = selectedDateEpochDay,
                        selectedTimeMinutes = selectedTimeMinutes,
                        durationMinutes = durationMinutes,
                        vehicleNumber = vehicleNumber,
                        vehicleType = vehicleType,
                        notes = notes
                    )
                }
            }

            // Fixed footer with buttons
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(16.dp),
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                TextButton(onClick = {
                    if (currentStep == 0) {
                        onBack()
                    } else {
                        currentStep -= 1
                    }
                }) {
                    Text(text = if (currentStep == 0) "Cancel" else "Back")
                }

                Button(
                    onClick = {
                        when (currentStep) {
                            0 -> if (selectedStationId != null) currentStep++ else formError = "Please select a station"
                            1 -> if (selectedDateEpochDay != null && selectedTimeMinutes != null) currentStep++ else formError = "Please choose date and time"
                            2 -> if (vehicleNumber.isNotBlank()) currentStep++ else formError = "Enter vehicle number"
                            3 -> {
                                val station = stations.find { it.id == selectedStationId }
                                val dateEpoch = selectedDateEpochDay
                                val timeMinutes = selectedTimeMinutes
                                if (station == null || dateEpoch == null || timeMinutes == null) {
                                    formError = "Incomplete booking information"
                                    return@Button
                                }
                                val date = LocalDate.ofEpochDay(dateEpoch)
                                val time = LocalTime.ofSecondOfDay((timeMinutes * 60).toLong())
                                val start = OffsetDateTime.of(date, time, ZoneId.systemDefault().rules.getOffset(LocalDateTime.now()))
                                val end = start.plusMinutes(durationMinutes.toLong())

                                scope.launch {
                                    val availability = onValidateTimeSlot(station.id, start, end)
                                    if (availability.isSuccess) {
                                        val response = availability.getOrNull()
                                        if (response?.isAvailable == false) {
                                            formError = response.message.ifBlank { "Selected time slot is not available." }
                                            return@launch
                                        }
                                        onSubmit(
                                            station.id,
                                            start,
                                            end,
                                            vehicleNumber,
                                            vehicleType,
                                            durationMinutes,
                                            notes.takeIf { it.isNotBlank() }
                                        ) { success, message ->
                                            if (success) {
                                                submitMessage = message
                                                onBack()
                                            } else {
                                                formError = message
                                            }
                                        }
                                    } else {
                                        formError = availability.exceptionOrNull()?.message ?: "Unable to validate time slot"
                                    }
                                }
                            }
                        }
                    },
                    enabled = !isSubmitting,
                    shape = RoundedCornerShape(12.dp)
                ) {
                    Text(text = if (currentStep == 3) "Submit" else "Next")
                }
            }
        }
    }
}

@Composable
private fun StepIndicator(currentStep: Int) {
    val titles = listOf("Station", "Schedule", "Vehicle", "Summary")
    Column {
        Text(text = "Booking Steps", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
        Spacer(modifier = Modifier.height(12.dp))
        Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
            titles.forEachIndexed { index, title ->
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    val color = if (index <= currentStep) MaterialTheme.colorScheme.primary else Color.LightGray
                    Box(
                        modifier = Modifier
                            .height(12.dp)
                            .width(12.dp)
                            .background(color, shape = RoundedCornerShape(50))
                    )
                    Spacer(modifier = Modifier.height(6.dp))
                    Text(text = title, style = MaterialTheme.typography.labelSmall, color = color)
                }
            }
        }
    }
}

@Composable
private fun StationSelectionStep(
    stations: List<StationUiModel>,
    selectedStationId: String?,
    onStationSelected: (String) -> Unit
) {
    Column(modifier = Modifier.fillMaxWidth()) {
        Text(
            text = "Select Charging Station",
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.SemiBold,
            modifier = Modifier.padding(bottom = 12.dp)
        )
        
        if (stations.isEmpty()) {
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(200.dp),
                contentAlignment = Alignment.Center
            ) {
                Text(text = "No stations available", color = Color.Gray)
            }
            return
        }

        LazyColumn(
            modifier = Modifier.fillMaxWidth(),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            items(stations, key = { it.id }) { station ->
                val isSelected = station.id == selectedStationId
                Card(
                    modifier = Modifier
                        .fillMaxWidth()
                        .clickable { onStationSelected(station.id) },
                    colors = CardDefaults.cardColors(
                        containerColor = if (isSelected) MaterialTheme.colorScheme.primary.copy(alpha = 0.15f) else Color.White
                    ),
                    shape = RoundedCornerShape(16.dp),
                    elevation = CardDefaults.cardElevation(defaultElevation = if (isSelected) 6.dp else 2.dp),
                    border = if (isSelected) androidx.compose.foundation.BorderStroke(2.dp, MaterialTheme.colorScheme.primary) else null
                ) {
                    Column(modifier = Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Text(
                                text = station.name,
                                style = MaterialTheme.typography.titleMedium,
                                fontWeight = FontWeight.Bold,
                                color = if (isSelected) MaterialTheme.colorScheme.primary else Color.Black
                            )
                            if (isSelected) {
                                Text(
                                    text = "✓ Selected",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.primary,
                                    fontWeight = FontWeight.Bold
                                )
                            }
                        }
                        Text(text = station.address, style = MaterialTheme.typography.bodySmall, color = Color.Gray)
                        Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                            Text(text = "⚡ ${station.connectorType}", style = MaterialTheme.typography.bodySmall)
                            Text(text = "🔌 ${station.availableSlots}/${station.totalSlots} slots", style = MaterialTheme.typography.bodySmall)
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun DateTimeSelectionStep(
    selectedDateEpochDay: Long?,
    selectedTimeMinutes: Int?,
    durationMinutes: Int,
    onDateSelected: (Long) -> Unit,
    onTimeSelected: (Int) -> Unit,
    onDurationSelected: (Int) -> Unit
) {
    val currentDate = selectedDateEpochDay ?: LocalDate.now().toEpochDay()
    val currentTimeMinutes = selectedTimeMinutes ?: (LocalTime.now().toSecondOfDay() / 60)
    Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
        Text(text = "Schedule", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
        DatePickerField(currentEpochDay = currentDate, onDateSelected = onDateSelected)
        TimePickerField(currentMinutes = currentTimeMinutes, onTimeSelected = onTimeSelected)
        DurationPickerField(currentDuration = durationMinutes, onDurationSelected = onDurationSelected)
    }
}

@Composable
private fun VehicleDetailsStep(
    vehicleNumber: String,
    vehicleType: String,
    notes: String,
    onVehicleNumberChanged: (String) -> Unit,
    onVehicleTypeChanged: (String) -> Unit,
    onNotesChanged: (String) -> Unit
) {
    Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
        Text(text = "Vehicle Details", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
        OutlinedTextField(
            value = vehicleNumber,
            onValueChange = onVehicleNumberChanged,
            label = { Text("Vehicle Number") },
            modifier = Modifier.fillMaxWidth()
        )
        OutlinedTextField(
            value = vehicleType,
            onValueChange = onVehicleTypeChanged,
            label = { Text("Vehicle Type") },
            modifier = Modifier.fillMaxWidth()
        )
        OutlinedTextField(
            value = notes,
            onValueChange = onNotesChanged,
            label = { Text("Notes (optional)") },
            modifier = Modifier.fillMaxWidth()
        )
    }
}

@Composable
private fun SummaryStep(
    station: StationUiModel?,
    selectedDateEpochDay: Long?,
    selectedTimeMinutes: Int?,
    durationMinutes: Int,
    vehicleNumber: String,
    vehicleType: String,
    notes: String
) {
    Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
        Text(text = "Summary", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
        Card(
            modifier = Modifier.fillMaxWidth(),
            colors = CardDefaults.cardColors(containerColor = Color.White),
            shape = RoundedCornerShape(16.dp),
            elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
        ) {
            Column(modifier = Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
                station?.let {
                    Text(text = it.name, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
                    Text(text = it.address, style = MaterialTheme.typography.bodySmall, color = Color.Gray)
                }
                Divider()
                val date = selectedDateEpochDay?.let { LocalDate.ofEpochDay(it) }
                val time = selectedTimeMinutes?.let { LocalTime.ofSecondOfDay((it * 60).toLong()) }
                val dateFormatter = DateTimeFormatter.ofPattern("EEE, dd MMM yyyy", Locale.getDefault())
                val timeFormatter = DateTimeFormatter.ofPattern("hh:mm a", Locale.getDefault())
                date?.let { DetailRow(label = "Date", value = it.format(dateFormatter)) }
                time?.let { DetailRow(label = "Start", value = it.format(timeFormatter)) }
                DetailRow(label = "Duration", value = "$durationMinutes minutes")
                DetailRow(label = "Vehicle", value = vehicleNumber.ifBlank { "-" })
                DetailRow(label = "Type", value = vehicleType.ifBlank { "-" })
                if (notes.isNotBlank()) {
                    DetailRow(label = "Notes", value = notes)
                }
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
        Text(text = value, style = MaterialTheme.typography.bodyMedium, fontWeight = FontWeight.Medium)
    }
}

@Composable
private fun DatePickerField(currentEpochDay: Long, onDateSelected: (Long) -> Unit) {
    val context = LocalContext.current
    val date = LocalDate.ofEpochDay(currentEpochDay)
    val formatted = date.format(DateTimeFormatter.ofPattern("EEE, dd MMM yyyy", Locale.getDefault()))

    OutlinedTextField(
        value = formatted,
        onValueChange = {},
        label = { Text("Reservation Date") },
        modifier = Modifier
            .fillMaxWidth()
            .clickableWithoutRipple {
                DatePickerDialog(
                    context,
                    { _, year, month, day ->
                        val newDate = LocalDate.of(year, month + 1, day)
                        onDateSelected(newDate.toEpochDay())
                    },
                    date.year,
                    date.monthValue - 1,
                    date.dayOfMonth
                ).show()
            },
        enabled = false
    )
}

@Composable
private fun TimePickerField(currentMinutes: Int, onTimeSelected: (Int) -> Unit) {
    val formatted = LocalTime.ofSecondOfDay((currentMinutes * 60).toLong()).format(DateTimeFormatter.ofPattern("hh:mm a", Locale.getDefault()))
    val expanded = remember { mutableStateOf(false) }

    Column {
        OutlinedTextField(
            value = formatted,
            onValueChange = {},
            label = { Text("Start Time") },
            modifier = Modifier
                .fillMaxWidth()
                .clickableWithoutRipple { expanded.value = true },
            enabled = false
        )
        DropdownMenu(expanded = expanded.value, onDismissRequest = { expanded.value = false }) {
            generateTimeSlots().forEach { slot ->
                DropdownMenuItem(
                    text = { Text(slot.first) },
                    onClick = {
                        onTimeSelected(slot.second)
                        expanded.value = false
                    }
                )
            }
        }
    }
}

@Composable
private fun DurationPickerField(currentDuration: Int, onDurationSelected: (Int) -> Unit) {
    val durations = listOf(30, 45, 60, 90, 120, 180)
    val expanded = remember { mutableStateOf(false) }
    Column {
        OutlinedTextField(
            value = "${currentDuration} minutes",
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
                        onDurationSelected(minutes)
                        expanded.value = false
                    }
                )
            }
        }
    }
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
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(text = message, color = textColor)
            TextButton(onClick = onDismiss) {
                Text(text = "Dismiss", color = textColor)
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
private fun Modifier.clickableWithoutRipple(onClick: () -> Unit): Modifier {
    val interactionSource = remember { MutableInteractionSource() }
    return this.then(
        Modifier.clickable(
            indication = null,
            interactionSource = interactionSource,
            onClick = onClick
        )
    )
}


