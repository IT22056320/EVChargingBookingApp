package com.example.evchargingmobile.ui.screens.dashboard

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.wrapContentHeight
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.ExperimentalMaterialApi
import androidx.compose.material.pullrefresh.PullRefreshIndicator
import androidx.compose.material.pullrefresh.pullRefresh
import androidx.compose.material.pullrefresh.rememberPullRefreshState
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.evchargingmobile.ui.theme.EVBlue
import com.example.evchargingmobile.ui.theme.EVGreen
import com.example.evchargingmobile.ui.theme.EVLightBlue
import com.example.evchargingmobile.ui.theme.EVOrange
import com.example.evchargingmobile.viewmodels.DashboardUiState
import com.example.evchargingmobile.viewmodels.StationUiModel
import com.google.android.gms.maps.model.CameraPosition
import com.google.android.gms.maps.model.LatLng
import com.google.maps.android.compose.GoogleMap
import com.google.maps.android.compose.Marker
import com.google.maps.android.compose.MarkerState
import com.google.maps.android.compose.rememberCameraPositionState

@OptIn(ExperimentalMaterialApi::class)
@Composable
fun DashboardHomeScreen(
    state: DashboardUiState,
    onRefresh: () -> Unit,
    onNavigateToBookings: () -> Unit,
    onNavigateToCreateBooking: () -> Unit,
    onNavigateToProfile: () -> Unit,
    onLogout: () -> Unit
) {
    val refreshState = rememberPullRefreshState(refreshing = state.isLoading, onRefresh = onRefresh)

    Box(
        modifier = Modifier
            .fillMaxSize()
            .pullRefresh(refreshState)
            .background(Color(0xFFF2F5F9))
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            GreetingSection(state = state, onLogout = onLogout)
            StatsSection(state = state)
            StationsMapSection(stations = state.stations)
            QuickActionsSection(
                onNavigateToBookings = onNavigateToBookings,
                onNavigateToCreateBooking = onNavigateToCreateBooking,
                onNavigateToProfile = onNavigateToProfile
            )
            if (!state.errorMessage.isNullOrBlank()) {
                ErrorBanner(message = state.errorMessage)
            }
        }

        PullRefreshIndicator(
            refreshing = state.isLoading,
            state = refreshState,
            modifier = Modifier.align(Alignment.TopCenter)
        )
    }
}

@Composable
private fun GreetingSection(state: DashboardUiState, onLogout: () -> Unit) {
    Surface(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(16.dp),
        color = EVBlue,
        tonalElevation = 4.dp
    ) {
        Column(
            modifier = Modifier.padding(20.dp)
        ) {
            Text(
                text = "Welcome back,",
                style = MaterialTheme.typography.bodyMedium,
                color = Color.White.copy(alpha = 0.8f)
            )
            Spacer(Modifier.height(4.dp))
            Text(
                text = state.userName.ifBlank { "EV Owner" },
                style = MaterialTheme.typography.headlineSmall.copy(fontWeight = FontWeight.Bold),
                color = Color.White
            )
            Spacer(Modifier.height(12.dp))
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                Text(
                    text = "Pending ${state.pendingCount}",
                    style = MaterialTheme.typography.bodyMedium,
                    color = Color.White
                )
                Text(
                    text = "Approved ${state.approvedCount}",
                    style = MaterialTheme.typography.bodyMedium,
                    color = Color.White
                )
                Spacer(Modifier.weight(1f))
                TextButton(onClick = onLogout) {
                    Text(text = "Logout", color = Color.White)
                }
            }
        }
    }
}

@Composable
private fun StatsSection(state: DashboardUiState) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        StatCard(
            title = "Pending",
            value = state.pendingCount.toString(),
            subtitle = "Awaiting approval",
            background = EVOrange
        )
        StatCard(
            title = "Approved",
            value = state.approvedCount.toString(),
            subtitle = "Upcoming",
            background = EVGreen
        )
        StatCard(
            title = "Stations",
            value = state.stations.size.toString(),
            subtitle = "Nearby",
            background = EVLightBlue
        )
    }
}

@Composable
private fun RowScope.StatCard(title: String, value: String, subtitle: String, background: Color) {
    Card(
        modifier = Modifier
            .weight(1f)
            .height(120.dp),
        colors = CardDefaults.cardColors(containerColor = background),
        elevation = CardDefaults.cardElevation(defaultElevation = 6.dp),
        shape = RoundedCornerShape(16.dp)
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(12.dp),
            verticalArrangement = Arrangement.SpaceBetween
        ) {
            Text(text = title, color = Color.White.copy(alpha = 0.9f), style = MaterialTheme.typography.bodyMedium)
            Text(text = value, color = Color.White, style = MaterialTheme.typography.headlineMedium, fontWeight = FontWeight.Bold)
            Text(text = subtitle, color = Color.White.copy(alpha = 0.8f), style = MaterialTheme.typography.labelMedium)
        }
    }
}

@Composable
private fun StationsMapSection(stations: List<StationUiModel>) {
    Column(
        modifier = Modifier.fillMaxWidth()
    ) {
        Text(
            text = "Nearby Stations",
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.SemiBold
        )
        Spacer(Modifier.height(12.dp))

        if (stations.isEmpty()) {
            Card(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(220.dp),
                colors = CardDefaults.cardColors(containerColor = Color.White),
                shape = RoundedCornerShape(16.dp)
            ) {
                Box(contentAlignment = Alignment.Center, modifier = Modifier.fillMaxSize()) {
                    Text(text = "No stations available nearby", color = Color.Gray)
                }
            }
        } else {
            val initialPosition = LatLng(stations.first().latitude, stations.first().longitude)
            val cameraPositionState = rememberCameraPositionState {
                position = CameraPosition.fromLatLngZoom(initialPosition, 12f)
            }

            Card(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(260.dp),
                shape = RoundedCornerShape(16.dp),
                elevation = CardDefaults.cardElevation(defaultElevation = 6.dp)
            ) {
                GoogleMap(
                    modifier = Modifier.fillMaxSize(),
                    cameraPositionState = cameraPositionState
                ) {
                    stations.forEach { station ->
                        Marker(
                            state = MarkerState(position = LatLng(station.latitude, station.longitude)),
                            title = station.name,
                            snippet = "Slots: ${station.availableSlots}/${station.totalSlots}"
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun QuickActionsSection(
    onNavigateToBookings: () -> Unit,
    onNavigateToCreateBooking: () -> Unit,
    onNavigateToProfile: () -> Unit
) {
    Column(
        modifier = Modifier.fillMaxWidth(),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        Text(
            text = "Quick Actions",
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.SemiBold
        )
        Button(
            onClick = onNavigateToCreateBooking,
            modifier = Modifier.fillMaxWidth(),
            shape = RoundedCornerShape(12.dp)
        ) {
            Text(text = "New Booking")
        }
        Button(
            onClick = onNavigateToBookings,
            modifier = Modifier.fillMaxWidth(),
            shape = RoundedCornerShape(12.dp)
        ) {
            Text(text = "My Bookings")
        }
        TextButton(
            onClick = onNavigateToProfile,
            modifier = Modifier.fillMaxWidth()
        ) {
            Text(text = "Profile", color = EVBlue)
        }
    }
}

@Composable
private fun ErrorBanner(message: String) {
    Surface(
        modifier = Modifier.fillMaxWidth(),
        color = Color(0xFFFFE8E9),
        shape = RoundedCornerShape(12.dp)
    ) {
        Text(
            text = message,
            modifier = Modifier.padding(12.dp),
            color = Color(0xFF862E2E)
        )
    }
}
