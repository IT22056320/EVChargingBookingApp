/*
 * File: QRScannerScreen.kt
 * Description: QR Code Scanner screen for Station Operators
 * Author: EAD Team
 * Date: 2025-10-06
 */
package com.example.evchargingmobile.ui.screens.operator

import android.Manifest
import android.content.pm.PackageManager
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Close
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.core.content.ContextCompat
import com.google.zxing.integration.android.IntentIntegrator
import com.journeyapps.barcodescanner.CaptureActivity
import com.journeyapps.barcodescanner.ScanContract
import com.journeyapps.barcodescanner.ScanOptions

/**
 * QR Scanner Screen for scanning booking QR codes
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun QRScannerScreen(
    onScanResult: (String) -> Unit,
    onBack: () -> Unit
) {
    val context = LocalContext.current
    var showPermissionRationale by remember { mutableStateOf(false) }
    var scanError by remember { mutableStateOf<String?>(null) }

    // Camera permission launcher
    val permissionLauncher = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.RequestPermission(),
        onResult = { isGranted ->
            if (isGranted) {
                showPermissionRationale = false
                // Permission granted, scanning will start
            } else {
                showPermissionRationale = true
            }
        }
    )

    // QR Code scanner launcher
    val scanLauncher = rememberLauncherForActivityResult(
        contract = ScanContract(),
        onResult = { result ->
            if (result.contents != null) {
                // Extract booking ID from QR code content
                val bookingId = extractBookingId(result.contents)
                if (bookingId != null) {
                    onScanResult(bookingId)
                } else {
                    scanError = "Invalid QR code format"
                }
            } else {
                // User cancelled scanning
                onBack()
            }
        }
    )

    // Check camera permission on launch
    LaunchedEffect(Unit) {
        when (PackageManager.PERMISSION_GRANTED) {
            ContextCompat.checkSelfPermission(context, Manifest.permission.CAMERA) -> {
                // Permission already granted, start scanner
                val options = ScanOptions().apply {
                    setDesiredBarcodeFormats(ScanOptions.QR_CODE)
                    setPrompt("Scan Booking QR Code")
                    setBeepEnabled(true)
                    setOrientationLocked(false)
                    setBarcodeImageEnabled(false)
                    setCaptureActivity(CaptureActivity::class.java)
                }
                scanLauncher.launch(options)
            }
            else -> {
                // Request permission
                permissionLauncher.launch(Manifest.permission.CAMERA)
            }
        }
    }

    // UI for permission rationale or error
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Scan QR Code") },
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
                .padding(padding),
            contentAlignment = Alignment.Center
        ) {
            when {
                showPermissionRationale -> {
                    PermissionRationaleContent(
                        onRequestPermission = {
                            permissionLauncher.launch(Manifest.permission.CAMERA)
                        },
                        onCancel = onBack
                    )
                }
                scanError != null -> {
                    ErrorContent(
                        error = scanError!!,
                        onRetry = {
                            scanError = null
                            val options = ScanOptions().apply {
                                setDesiredBarcodeFormats(ScanOptions.QR_CODE)
                                setPrompt("Scan Booking QR Code")
                                setBeepEnabled(true)
                                setOrientationLocked(false)
                                setBarcodeImageEnabled(false)
                                setCaptureActivity(CaptureActivity::class.java)
                            }
                            scanLauncher.launch(options)
                        },
                        onCancel = onBack
                    )
                }
                else -> {
                    CircularProgressIndicator()
                }
            }
        }
    }
}

/**
 * Permission rationale UI
 */
@Composable
private fun PermissionRationaleContent(
    onRequestPermission: () -> Unit,
    onCancel: () -> Unit
) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Icon(
            imageVector = Icons.Default.Close,
            contentDescription = null,
            modifier = Modifier.size(64.dp),
            tint = MaterialTheme.colorScheme.error
        )
        
        Text(
            text = "Camera Permission Required",
            style = MaterialTheme.typography.headlineSmall,
            textAlign = TextAlign.Center
        )
        
        Text(
            text = "This app needs camera access to scan QR codes. Please grant camera permission to continue.",
            style = MaterialTheme.typography.bodyMedium,
            textAlign = TextAlign.Center,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        
        Spacer(modifier = Modifier.height(8.dp))
        
        Button(
            onClick = onRequestPermission,
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Grant Permission")
        }
        
        TextButton(onClick = onCancel) {
            Text("Cancel")
        }
    }
}

/**
 * Error content UI
 */
@Composable
private fun ErrorContent(
    error: String,
    onRetry: () -> Unit,
    onCancel: () -> Unit
) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Icon(
            imageVector = Icons.Default.Close,
            contentDescription = null,
            modifier = Modifier.size(64.dp),
            tint = MaterialTheme.colorScheme.error
        )
        
        Text(
            text = "Scan Error",
            style = MaterialTheme.typography.headlineSmall,
            textAlign = TextAlign.Center
        )
        
        Text(
            text = error,
            style = MaterialTheme.typography.bodyMedium,
            textAlign = TextAlign.Center,
            color = MaterialTheme.colorScheme.error
        )
        
        Spacer(modifier = Modifier.height(8.dp))
        
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
 * Extract booking ID from QR code content
 * Expected formats:
 * 1. "BOOKING:{bookingId}"
 * 2. JSON: {"BookingId":"...", ...} (case-insensitive)
 * 3. Plain booking ID
 */
private fun extractBookingId(qrContent: String): String? {
    return try {
        when {
            qrContent.startsWith("BOOKING:", ignoreCase = true) -> {
                qrContent.substring(8).trim()
            }
            qrContent.startsWith("{") && qrContent.endsWith("}") -> {
                // JSON format: {"BookingId":"..."} or {"bookingId":"..."}
                // Case-insensitive regex to handle both
                val regex = "\"[Bb]ooking[Ii]d\"\\s*:\\s*\"([^\"]+)\"".toRegex()
                regex.find(qrContent)?.groupValues?.get(1)
            }
            else -> {
                // Assume the entire content is the booking ID
                qrContent.trim()
            }
        }
    } catch (e: Exception) {
        null
    }
}
