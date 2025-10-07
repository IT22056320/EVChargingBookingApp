package com.example.evchargingmobile.ui.screens.bookings

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
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.ExperimentalMaterialApi
import androidx.compose.material.pullrefresh.PullRefreshIndicator
import androidx.compose.material.pullrefresh.pullRefresh
import androidx.compose.material.pullrefresh.rememberPullRefreshState
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilterChip
import androidx.compose.material3.FilterChipDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.example.evchargingmobile.models.Booking
import com.example.evchargingmobile.viewmodels.BookingFilter
import com.example.evchargingmobile.viewmodels.BookingUi
import com.example.evchargingmobile.viewmodels.BookingsUiState
import java.time.format.DateTimeFormatter

@OptIn(ExperimentalMaterialApi::class, ExperimentalMaterial3Api::class)
@Composable
fun BookingsScreen(
    state: BookingsUiState,
    onBack: () -> Unit,
    onRefresh: () -> Unit,
    onFilterChanged: (BookingFilter) -> Unit,
    onBookingSelected: (String) -> Unit,
    onClearMessage: () -> Unit
) {
    val refreshState = rememberPullRefreshState(refreshing = state.isRefreshing, onRefresh = onRefresh)

    LaunchedEffect(state.successMessage, state.errorMessage) {
        if (!state.successMessage.isNullOrBlank() || !state.errorMessage.isNullOrBlank()) {
            // Auto clear after displaying once in UI
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(text = "My Bookings") },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(imageVector = Icons.Filled.ArrowBack, contentDescription = "Back")
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(containerColor = MaterialTheme.colorScheme.surface)
            )
        }
    ) { innerPadding ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
                .pullRefresh(refreshState)
                .background(Color(0xFFF4F6FA))
        ) {
            Column(
                modifier = Modifier.fillMaxSize()
            ) {
                FilterRow(selectedFilter = state.filter, onFilterChanged = onFilterChanged)

                if (!state.successMessage.isNullOrBlank()) {
                    MessageBanner(message = state.successMessage, isError = false, onDismiss = onClearMessage)
                }

                if (!state.errorMessage.isNullOrBlank()) {
                    MessageBanner(message = state.errorMessage, isError = true, onDismiss = onClearMessage)
                }

                if (state.displayedBookings.isEmpty() && !state.isLoading) {
                    EmptyState()
                } else {
                    LazyColumn(
                        modifier = Modifier.fillMaxSize(),
                        verticalArrangement = Arrangement.spacedBy(12.dp)
                    ) {
                        items(state.displayedBookings, key = { it.id }) { booking ->
                            BookingItemCard(booking = booking, onClick = { onBookingSelected(booking.id) })
                        }
                        item { Spacer(modifier = Modifier.height(16.dp)) }
                    }
                }
            }

            PullRefreshIndicator(
                refreshing = state.isRefreshing,
                state = refreshState,
                modifier = Modifier.align(Alignment.TopCenter)
            )
        }
    }
}

@Composable
private fun FilterRow(selectedFilter: BookingFilter, onFilterChanged: (BookingFilter) -> Unit) {
    val filters = remember { BookingFilter.values() }
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 12.dp),
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        filters.forEach { filter ->
            FilterChip(
                selected = selectedFilter == filter,
                onClick = { onFilterChanged(filter) },
                label = { Text(filter.name.lowercase().replaceFirstChar { it.uppercase() }) },
                colors = FilterChipDefaults.filterChipColors(
                    selectedContainerColor = MaterialTheme.colorScheme.primary,
                    selectedLabelColor = Color.White,
                    containerColor = Color.White
                )
            )
        }
    }
}

@Composable
private fun BookingItemCard(booking: BookingUi, onClick: () -> Unit) {
    Card(
        modifier = Modifier
            .padding(horizontal = 16.dp)
            .fillMaxWidth()
            .clickable { onClick() },
        colors = CardDefaults.cardColors(containerColor = Color.White),
        shape = RoundedCornerShape(16.dp),
        elevation = CardDefaults.cardElevation(defaultElevation = 4.dp)
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        text = booking.stationName,
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.SemiBold,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis
                    )
                    booking.stationAddress?.let {
                        Text(
                            text = it,
                            style = MaterialTheme.typography.bodySmall,
                            color = Color.Gray,
                            maxLines = 1,
                            overflow = TextOverflow.Ellipsis
                        )
                    }
                }
                StatusBadge(status = booking.statusLabel, statusEnum = booking.status)
            }

            Spacer(modifier = Modifier.height(12.dp))

            val formatter = DateTimeFormatter.ofPattern("EEE, dd MMM yyyy • hh:mm a")
            booking.startTime?.let { start ->
                Text(
                    text = "Start: ${start.format(formatter)}",
                    style = MaterialTheme.typography.bodyMedium
                )
            }
            booking.endTime?.let { end ->
                Text(
                    text = "End: ${end.format(formatter)}",
                    style = MaterialTheme.typography.bodyMedium,
                    color = Color.Gray
                )
            }

            Spacer(modifier = Modifier.height(12.dp))

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = "Vehicle: ${booking.vehicleNumber.ifBlank { "-" }}",
                    style = MaterialTheme.typography.bodySmall,
                    color = Color.Gray
                )
                booking.totalCost?.let { cost ->
                    Text(
                        text = "Rs. %.2f".format(cost),
                        style = MaterialTheme.typography.labelLarge,
                        color = MaterialTheme.colorScheme.primary
                    )
                }
            }
        }
    }
}

@Composable
private fun StatusBadge(status: String, statusEnum: Booking.BookingStatus) {
    val (background, content) = when (statusEnum) {
        Booking.BookingStatus.PENDING -> Color(0xFFFFF4E5) to Color(0xFFB76E00)
        Booking.BookingStatus.APPROVED, Booking.BookingStatus.CONFIRMED, Booking.BookingStatus.ACTIVE -> Color(0xFFE6F7ED) to Color(0xFF1B8E49)
        Booking.BookingStatus.COMPLETED -> Color(0xFFEDE7F6) to Color(0xFF5E35B1)
        Booking.BookingStatus.CANCELLED -> Color(0xFFFFE8E9) to Color(0xFFC62828)
    }

    Surface(
        shape = RoundedCornerShape(50),
        color = background
    ) {
        Text(
            text = status,
            modifier = Modifier.padding(horizontal = 12.dp, vertical = 6.dp),
            color = content,
            style = MaterialTheme.typography.labelMedium,
            fontWeight = FontWeight.SemiBold
        )
    }
}

@Composable
private fun EmptyState() {
    Box(
        modifier = Modifier.fillMaxSize(),
        contentAlignment = Alignment.Center
    ) {
        Text(
            text = "No bookings yet",
            style = MaterialTheme.typography.bodyLarge,
            color = Color.Gray
        )
    }
}

@Composable
private fun MessageBanner(message: String, isError: Boolean, onDismiss: () -> Unit) {
    val background = if (isError) Color(0xFFFFE8E9) else Color(0xFFE8FFF3)
    val textColor = if (isError) Color(0xFF8C1D1D) else Color(0xFF1A7345)

    Surface(
        modifier = Modifier
            .padding(horizontal = 16.dp, vertical = 4.dp)
            .fillMaxWidth(),
        color = background,
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




