/*
 * File: DashboardCompose.kt
 * Description: Modern Dashboard screen using Jetpack Compose for EV Charging app
 * Author: EV Charging Team
 * Date: September 21, 2025
 */
package com.example.evchargingmobile.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
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
fun DashboardScreen(
    onNavigateToBookings: () -> Unit = {},
    onNavigateToStations: () -> Unit = {},
    onNavigateToProfile: () -> Unit = {},
    onNavigateToHistory: () -> Unit = {},
    onLogout: () -> Unit = {},
    pendingReservations: Int = 0,
    approvedReservations: Int = 0,
    nearbyStations: Int = 0
) {
    var showLogoutDialog by remember { mutableStateOf(false) }

    // Logout confirmation dialog
    if (showLogoutDialog) {
        AlertDialog(
            onDismissRequest = { showLogoutDialog = false },
            title = { Text("Logout") },
            text = { Text("Are you sure you want to logout from your account?") },
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
                            text = "Good Morning! ⚡",
                            fontSize = 20.sp,
                            fontWeight = FontWeight.Bold
                        )
                        Text(
                            text = "Ready to charge your EV?",
                            fontSize = 14.sp,
                            color = Color.Gray
                        )
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = EVBlue,
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
            // Quick Stats Cards
            item {
                Text(
                    text = "Quick Overview",
                    fontSize = 20.sp,
                    fontWeight = FontWeight.Bold,
                    color = EVBlue,
                    modifier = Modifier.padding(bottom = 8.dp)
                )

                LazyRow(
                    horizontalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    item {
                        StatsCard(
                            title = "Pending",
                            value = pendingReservations.toString(),
                            subtitle = "Reservations",
                            icon = Icons.Default.Info,
                            color = EVOrange
                        )
                    }
                    item {
                        StatsCard(
                            title = "Approved",
                            value = approvedReservations.toString(),
                            subtitle = "Bookings",
                            icon = Icons.Default.CheckCircle,
                            color = EVGreen
                        )
                    }
                    item {
                        StatsCard(
                            title = "Nearby",
                            value = nearbyStations.toString(),
                            subtitle = "Stations",
                            icon = Icons.Default.LocationOn,
                            color = EVBlue
                        )
                    }
                }
            }

            // Quick Actions
            item {
                Text(
                    text = "Quick Actions",
                    fontSize = 20.sp,
                    fontWeight = FontWeight.Bold,
                    color = EVBlue,
                    modifier = Modifier.padding(top = 8.dp, bottom = 8.dp)
                )

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    ActionCard(
                        title = "Book Charging",
                        subtitle = "Reserve a slot",
                        icon = Icons.Default.Add,
                        color = EVBlue,
                        modifier = Modifier.weight(1f),
                        onClick = onNavigateToStations
                    )
                    ActionCard(
                        title = "My Bookings",
                        subtitle = "View reservations",
                        icon = Icons.Default.List,
                        color = EVGreen,
                        modifier = Modifier.weight(1f),
                        onClick = onNavigateToBookings
                    )
                }
            }

            // Recent Activity
            item {
                Text(
                    text = "Recent Activity",
                    fontSize = 20.sp,
                    fontWeight = FontWeight.Bold,
                    color = EVBlue,
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
                        RecentActivityItem(
                            title = "Booking Confirmed",
                            subtitle = "Station Alpha - Today 2:00 PM",
                            icon = Icons.Default.CheckCircle,
                            color = EVGreen
                        )
                        Divider(modifier = Modifier.padding(vertical = 8.dp))
                        RecentActivityItem(
                            title = "Payment Successful",
                            subtitle = "₹250 - Yesterday",
                            icon = Icons.Default.CheckCircle, // Using CheckCircle as it's already working
                            color = EVBlue
                        )
                        Divider(modifier = Modifier.padding(vertical = 8.dp))
                        RecentActivityItem(
                            title = "Charging Completed",
                            subtitle = "Station Beta - 2 days ago",
                            icon = Icons.Default.Check, // Using Check which is most basic
                            color = EVGreen
                        )
                    }
                }
            }

            // Map Section Placeholder
            item {
                Text(
                    text = "Nearby Charging Stations",
                    fontSize = 20.sp,
                    fontWeight = FontWeight.Bold,
                    color = EVBlue,
                    modifier = Modifier.padding(top = 8.dp, bottom = 8.dp)
                )

                Card(
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(200.dp),
                    colors = CardDefaults.cardColors(containerColor = Color.White),
                    elevation = CardDefaults.cardElevation(defaultElevation = 4.dp)
                ) {
                    Box(
                        modifier = Modifier.fillMaxSize(),
                        contentAlignment = Alignment.Center
                    ) {
                        Column(
                            horizontalAlignment = Alignment.CenterHorizontally
                        ) {
                            Icon(
                                Icons.Default.Place, // Changed from Map
                                contentDescription = "Map",
                                modifier = Modifier.size(48.dp),
                                tint = EVBlue
                            )
                            Spacer(modifier = Modifier.height(8.dp))
                            Text(
                                text = "Google Maps Integration",
                                fontWeight = FontWeight.Medium,
                                color = EVBlue
                            )
                            Text(
                                text = "Shows nearby charging stations",
                                fontSize = 12.sp,
                                color = Color.Gray
                            )
                        }
                    }
                }
            }
        }
    }
}

@Composable
fun StatsCard(
    title: String,
    value: String,
    subtitle: String,
    icon: ImageVector,
    color: Color,
    modifier: Modifier = Modifier
) {
    Card(
        modifier = modifier.width(120.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        elevation = CardDefaults.cardElevation(defaultElevation = 4.dp)
    ) {
        Column(
            modifier = Modifier.padding(16.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Icon(
                imageVector = icon,
                contentDescription = title,
                tint = color,
                modifier = Modifier.size(24.dp)
            )
            Spacer(modifier = Modifier.height(8.dp))
            Text(
                text = value,
                fontSize = 24.sp,
                fontWeight = FontWeight.Bold,
                color = color
            )
            Text(
                text = title,
                fontSize = 12.sp,
                fontWeight = FontWeight.Medium,
                color = Color.Gray
            )
            Text(
                text = subtitle,
                fontSize = 10.sp,
                color = Color.Gray
            )
        }
    }
}

@Composable
fun ActionCard(
    title: String,
    subtitle: String,
    icon: ImageVector,
    color: Color,
    modifier: Modifier = Modifier,
    onClick: () -> Unit = {}
) {
    Card(
        modifier = modifier,
        colors = CardDefaults.cardColors(containerColor = color),
        elevation = CardDefaults.cardElevation(defaultElevation = 4.dp),
        onClick = onClick
    ) {
        Column(
            modifier = Modifier.padding(16.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Icon(
                imageVector = icon,
                contentDescription = title,
                tint = Color.White,
                modifier = Modifier.size(32.dp)
            )
            Spacer(modifier = Modifier.height(8.dp))
            Text(
                text = title,
                fontSize = 16.sp,
                fontWeight = FontWeight.Bold,
                color = Color.White
            )
            Text(
                text = subtitle,
                fontSize = 12.sp,
                color = Color.White.copy(alpha = 0.8f)
            )
        }
    }
}

@Composable
fun RecentActivityItem(
    title: String,
    subtitle: String,
    icon: ImageVector,
    color: Color
) {
    Row(
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(
            imageVector = icon,
            contentDescription = title,
            tint = color,
            modifier = Modifier.size(24.dp)
        )
        Spacer(modifier = Modifier.width(12.dp))
        Column(
            modifier = Modifier.weight(1f)
        ) {
            Text(
                text = title,
                fontWeight = FontWeight.Medium,
                fontSize = 14.sp
            )
            Text(
                text = subtitle,
                fontSize = 12.sp,
                color = Color.Gray
            )
        }
    }
}
