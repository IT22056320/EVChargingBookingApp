/*
 * File: StationOperatorDashboard.kt
 * Description: Dashboard screen for Station Operators using Jetpack Compose
 * Author: EV Charging Team
 * Date: September 21, 2025
 */
package com.example.evchargingmobile.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.evchargingmobile.ui.theme.*

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun StationOperatorDashboard(
    onNavigateToQRScanner: () -> Unit = {},
    onNavigateToActiveBookings: () -> Unit = {},
    onNavigateToStationStatus: () -> Unit = {},
    onNavigateToProfile: () -> Unit = {},
    onLogout: () -> Unit = {},
    activeBookings: Int = 0,
    completedToday: Int = 0,
    stationStatus: String = "Online"
) {
    var showLogoutDialog by remember { mutableStateOf(false) }

    // Logout confirmation dialog
    if (showLogoutDialog) {
        AlertDialog(
            onDismissRequest = { showLogoutDialog = false },
            title = { Text("Logout") },
            text = { Text("Are you sure you want to logout from your station operator account?") },
            confirmButton = {
                TextButton(
                    onClick = {
                        showLogoutDialog = false
                        onLogout()
                    }
                ) {
                    Text("Logout", color = MaterialTheme.colorScheme.error)
                }
            },
            dismissButton = {
                TextButton(onClick = { showLogoutDialog = false }) {
                    Text("Cancel")
                }
            }
        )
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text(
                            text = "Station Operator ⚡",
                            fontSize = 20.sp,
                            fontWeight = FontWeight.Bold
                        )
                        Text(
                            text = "Manage charging operations",
                            fontSize = 14.sp,
                            color = Color.Gray
                        )
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = EVGreen,
                    titleContentColor = Color.White
                ),
                actions = {
                    // Profile button
                    IconButton(onClick = onNavigateToProfile) {
                        Icon(
                            Icons.Default.Person,
                            contentDescription = "Profile",
                            tint = Color.White
                        )
                    }
                    // Logout button
                    IconButton(onClick = { showLogoutDialog = true }) {
                        Icon(
                            Icons.Default.ExitToApp,
                            contentDescription = "Logout",
                            tint = Color.White
                        )
                    }
                }
            )
        }
    ) { paddingValues ->
        LazyColumn(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
                .background(Color(0xFFF8F9FA)),
            contentPadding = PaddingValues(16.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            // Station Status Overview
            item {
                Text(
                    text = "Station Overview",
                    fontSize = 20.sp,
                    fontWeight = FontWeight.Bold,
                    color = EVGreen,
                    modifier = Modifier.padding(bottom = 8.dp)
                )

                LazyRow(
                    horizontalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    item {
                        StatsCard(
                            title = "Active",
                            value = activeBookings.toString(),
                            subtitle = "Bookings",
                            icon = Icons.Default.Star, // Using Star instead of DirectionsCar
                            color = EVOrange
                        )
                    }
                    item {
                        StatsCard(
                            title = "Completed",
                            value = completedToday.toString(),
                            subtitle = "Today",
                            icon = Icons.Default.CheckCircle,
                            color = EVGreen
                        )
                    }
                    item {
                        StatsCard(
                            title = "Station",
                            value = stationStatus,
                            subtitle = "Status",
                            icon = Icons.Default.CheckCircle, // Using CheckCircle instead of Circle
                            color = if (stationStatus == "Online") EVGreen else EVRed
                        )
                    }
                }
            }

            // Quick Actions for Station Operators
            item {
                Text(
                    text = "Operator Actions",
                    fontSize = 20.sp,
                    fontWeight = FontWeight.Bold,
                    color = EVGreen,
                    modifier = Modifier.padding(top = 8.dp, bottom = 8.dp)
                )

                Column(
                    verticalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    // QR Scanner - Primary action
                    ActionCard(
                        title = "Scan QR Code",
                        subtitle = "Verify customer booking",
                        icon = Icons.Default.Add, // Using Add instead of Camera
                        color = EVGreen,
                        modifier = Modifier.fillMaxWidth(),
                        onClick = onNavigateToQRScanner
                    )

                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(12.dp)
                    ) {
                        ActionCard(
                            title = "Active Bookings",
                            subtitle = "Manage current sessions",
                            icon = Icons.Default.List,
                            color = EVBlue,
                            modifier = Modifier.weight(1f),
                            onClick = onNavigateToActiveBookings
                        )
                        ActionCard(
                            title = "Station Status",
                            subtitle = "Update availability",
                            icon = Icons.Default.Settings,
                            color = EVOrange,
                            modifier = Modifier.weight(1f),
                            onClick = onNavigateToStationStatus
                        )
                    }
                }
            }

            // Current Operations
            item {
                Text(
                    text = "Current Operations",
                    fontSize = 20.sp,
                    fontWeight = FontWeight.Bold,
                    color = EVGreen,
                    modifier = Modifier.padding(top = 8.dp, bottom = 8.dp)
                )

                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(containerColor = Color.White),
                    elevation = CardDefaults.cardElevation(defaultElevation = 4.dp)
                ) {
                    Column(
                        modifier = Modifier.padding(16.dp)
                    ) {
                        if (activeBookings > 0) {
                            RecentActivityItem(
                                title = "Charging in Progress",
                                subtitle = "Bay 1 - Tesla Model 3 - Started 10:30 AM",
                                icon = Icons.Default.Star, // Using Star instead of DirectionsCar
                                color = EVGreen
                            )
                            HorizontalDivider(modifier = Modifier.padding(vertical = 8.dp))
                            RecentActivityItem(
                                title = "Awaiting Customer",
                                subtitle = "Bay 2 - Booking confirmed for 11:00 AM",
                                icon = Icons.Default.Info, // Using Info instead of AccessTime
                                color = EVOrange
                            )
                        } else {
                            Box(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(32.dp),
                                contentAlignment = Alignment.Center
                            ) {
                                Column(
                                    horizontalAlignment = Alignment.CenterHorizontally
                                ) {
                                    Icon(
                                        Icons.Default.Star, // Using Star instead of DirectionsCar
                                        contentDescription = "No active bookings",
                                        modifier = Modifier.size(48.dp),
                                        tint = Color.Gray
                                    )
                                    Spacer(modifier = Modifier.height(8.dp))
                                    Text(
                                        text = "No Active Bookings",
                                        fontWeight = FontWeight.Medium,
                                        color = Color.Gray
                                    )
                                    Text(
                                        text = "Station is ready for customers",
                                        fontSize = 12.sp,
                                        color = Color.Gray
                                    )
                                }
                            }
                        }
                    }
                }
            }

            // Instructions for Station Operators
            item {
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(containerColor = EVBlue.copy(alpha = 0.1f)),
                    elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
                ) {
                    Column(
                        modifier = Modifier.padding(16.dp)
                    ) {
                        Row(
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(
                                Icons.Default.Info,
                                contentDescription = "Instructions",
                                tint = EVBlue,
                                modifier = Modifier.size(24.dp)
                            )
                            Spacer(modifier = Modifier.width(8.dp))
                            Text(
                                text = "Station Operator Instructions",
                                fontWeight = FontWeight.Medium,
                                color = EVBlue
                            )
                        }
                        Spacer(modifier = Modifier.height(8.dp))
                        Text(
                            text = "1. Scan customer QR codes to verify bookings\n" +
                                    "2. Confirm charging session start\n" +
                                    "3. Monitor charging progress\n" +
                                    "4. Finalize payment when complete",
                            fontSize = 14.sp,
                            color = Color.Gray
                        )
                    }
                }
            }
        }
    }
}
