/*
 * File: LocationPickerField.kt
 * Description: Reusable location picker with Google Maps and Places Autocomplete
 * Author: EV Charging Team
 * Date: December 2024
 */
package com.example.evchargingmobile.ui.components

import android.app.Activity
import android.content.Context
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.LocationOn
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties
import com.google.android.gms.maps.model.CameraPosition
import com.google.android.gms.maps.model.LatLng
import com.google.maps.android.compose.*
import com.google.android.libraries.places.api.Places
import com.google.android.libraries.places.api.model.Place
import com.google.android.libraries.places.widget.Autocomplete
import com.google.android.libraries.places.widget.AutocompleteActivity
import com.google.android.libraries.places.widget.model.AutocompleteActivityMode
import android.content.Intent
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import com.example.evchargingmobile.ui.theme.EVBlue
import com.google.android.gms.location.LocationServices
import com.google.android.gms.location.FusedLocationProviderClient
import android.Manifest
import android.content.pm.PackageManager
import androidx.core.content.ContextCompat
import com.google.android.gms.tasks.Task
import android.location.Location as AndroidLocation
import android.location.Geocoder
import java.util.Locale
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException

/**
 * Data class to hold location information
 */
data class LocationData(
    val address: String,
    val latitude: Double,
    val longitude: Double
)

/**
 * Location Picker Field with Interactive Map and Search
 *
 * @param label Label for the text field
 * @param placeholder Placeholder text
 * @param initialAddress Initial address value
 * @param initialLatitude Initial latitude
 * @param initialLongitude Initial longitude
 * @param onLocationSelected Callback when location is selected with address, lat, lng
 * @param modifier Optional modifier
 * @param isError Whether the field is in error state
 * @param supportingText Supporting text shown below the field
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun LocationPickerField(
    label: String = "Address",
    placeholder: String = "Select your location",
    initialAddress: String = "",
    initialLatitude: Double? = null,
    initialLongitude: Double? = null,
    onLocationSelected: (LocationData) -> Unit,
    modifier: Modifier = Modifier,
    isError: Boolean = false,
    supportingText: String? = null
) {
    val context = LocalContext.current
    var selectedAddress by remember { mutableStateOf(initialAddress) }
    var showMapPicker by remember { mutableStateOf(false) }
    
    // Initialize Places API if not already initialized
    LaunchedEffect(Unit) {
        if (!Places.isInitialized()) {
            try {
                Places.initialize(context.applicationContext, getPlacesApiKey(context))
            } catch (e: Exception) {
                // API key not configured
            }
        }
    }

    // Activity launcher for Places Autocomplete (search mode)
    val searchLauncher = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.StartActivityForResult()
    ) { result ->
        when (result.resultCode) {
            Activity.RESULT_OK -> {
                result.data?.let { data ->
                    val place = Autocomplete.getPlaceFromIntent(data)
                    selectedAddress = place.address ?: ""
                    
                    val lat = place.latLng?.latitude ?: 0.0
                    val lng = place.latLng?.longitude ?: 0.0
                    
                    onLocationSelected(LocationData(
                        address = selectedAddress,
                        latitude = lat,
                        longitude = lng
                    ))
                }
            }
        }
    }

    // Map picker dialog
    if (showMapPicker) {
        MapLocationPickerDialog(
            initialLocation = if (initialLatitude != null && initialLongitude != null) {
                LatLng(initialLatitude, initialLongitude)
            } else null,
            onLocationSelected = { latLng, address ->
                selectedAddress = address
                onLocationSelected(LocationData(
                    address = address,
                    latitude = latLng.latitude,
                    longitude = latLng.longitude
                ))
                showMapPicker = false
            },
            onDismiss = { showMapPicker = false }
        )
    }

    Column(modifier = modifier) {
        OutlinedTextField(
            value = selectedAddress,
            onValueChange = { },
            label = { Text(label) },
            placeholder = { Text(placeholder) },
            modifier = Modifier
                .fillMaxWidth()
                .clickable {
                    // Show options: Search or Pick on Map
                    showMapPicker = true
                },
            leadingIcon = {
                Icon(
                    imageVector = Icons.Default.LocationOn,
                    contentDescription = "Location",
                    tint = EVBlue
                )
            },
            trailingIcon = {
                IconButton(onClick = { 
                    launchPlacesAutocomplete(context, searchLauncher)
                }) {
                    Icon(
                        imageVector = Icons.Default.Search,
                        contentDescription = "Search location",
                        tint = EVBlue
                    )
                }
            },
            colors = OutlinedTextFieldDefaults.colors(
                focusedBorderColor = EVBlue,
                focusedLabelColor = EVBlue
            ),
            readOnly = true,
            enabled = false,
            isError = isError,
            supportingText = if (supportingText != null) {
                { Text(supportingText) }
            } else null,
            singleLine = false,
            maxLines = 3
        )
        
        Text(
            text = "Tap field to pick on map • Tap 📍 to search",
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            modifier = Modifier.padding(start = 16.dp, top = 4.dp)
        )
    }
}

/**
 * Interactive Map Location Picker Dialog
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MapLocationPickerDialog(
    initialLocation: LatLng?,
    onLocationSelected: (LatLng, String) -> Unit,
    onDismiss: () -> Unit
) {
    val context = LocalContext.current
    
    // Default to Colombo, Sri Lanka if no initial location
    var selectedLocation by remember { 
        mutableStateOf(initialLocation ?: LatLng(6.9271, 79.8612))
    }
    var selectedAddress by remember { mutableStateOf("") }
    var isLoading by remember { mutableStateOf(false) }
    
    // Get address from coordinates when location changes
    LaunchedEffect(selectedLocation) {
        isLoading = true
        try {
            val geocoder = Geocoder(context, Locale.getDefault())
            val addresses = geocoder.getFromLocation(
                selectedLocation.latitude,
                selectedLocation.longitude,
                1
            )
            if (!addresses.isNullOrEmpty()) {
                selectedAddress = addresses[0].getAddressLine(0) ?: "Unknown location"
            }
        } catch (e: Exception) {
            selectedAddress = "${selectedLocation.latitude}, ${selectedLocation.longitude}"
        }
        isLoading = false
    }

    // Try to get current location
    LaunchedEffect(Unit) {
        if (initialLocation == null && 
            ContextCompat.checkSelfPermission(context, Manifest.permission.ACCESS_FINE_LOCATION) 
            == PackageManager.PERMISSION_GRANTED) {
            try {
                val fusedLocationClient = LocationServices.getFusedLocationProviderClient(context)
                val location = suspendCancellableCoroutine<AndroidLocation?> { continuation ->
                    fusedLocationClient.lastLocation
                        .addOnSuccessListener { loc -> continuation.resume(loc) }
                        .addOnFailureListener { exception -> continuation.resumeWithException(exception) }
                }
                location?.let {
                    selectedLocation = LatLng(it.latitude, it.longitude)
                }
            } catch (e: Exception) {
                // Use default location
            }
        }
    }

    Dialog(
        onDismissRequest = onDismiss,
        properties = DialogProperties(usePlatformDefaultWidth = false)
    ) {
        Card(
            modifier = Modifier
                .fillMaxWidth(0.95f)
                .fillMaxHeight(0.85f)
        ) {
            Column(modifier = Modifier.fillMaxSize()) {
                // Header
                TopAppBar(
                    title = { Text("Pick Location on Map") },
                    colors = TopAppBarDefaults.topAppBarColors(
                        containerColor = EVBlue,
                        titleContentColor = MaterialTheme.colorScheme.onPrimary
                    )
                )

                // Map
                Box(modifier = Modifier.weight(1f)) {
                    val cameraPositionState = rememberCameraPositionState {
                        position = CameraPosition.fromLatLngZoom(selectedLocation, 15f)
                    }

                    GoogleMap(
                        modifier = Modifier.fillMaxSize(),
                        cameraPositionState = cameraPositionState,
                        onMapClick = { latLng ->
                            selectedLocation = latLng
                        },
                        properties = MapProperties(
                            isMyLocationEnabled = ContextCompat.checkSelfPermission(
                                context,
                                Manifest.permission.ACCESS_FINE_LOCATION
                            ) == PackageManager.PERMISSION_GRANTED
                        ),
                        uiSettings = MapUiSettings(
                            zoomControlsEnabled = true,
                            myLocationButtonEnabled = true
                        )
                    ) {
                        Marker(
                            state = MarkerState(position = selectedLocation),
                            title = "Selected Location"
                        )
                    }
                }

                // Address display and actions
                Column(
                    modifier = Modifier.padding(16.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    Text(
                        text = "Selected Location:",
                        style = MaterialTheme.typography.labelMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    
                    if (isLoading) {
                        CircularProgressIndicator(
                            modifier = Modifier.size(24.dp),
                            color = EVBlue
                        )
                    } else {
                        Text(
                            text = selectedAddress,
                            style = MaterialTheme.typography.bodyMedium
                        )
                    }
                    
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        OutlinedButton(
                            onClick = onDismiss,
                            modifier = Modifier.weight(1f)
                        ) {
                            Text("Cancel")
                        }
                        
                        Button(
                            onClick = {
                                onLocationSelected(selectedLocation, selectedAddress)
                            },
                            modifier = Modifier.weight(1f),
                            enabled = !isLoading
                        ) {
                            Text("Confirm")
                        }
                    }
                    
                    Text(
                        text = "Tap anywhere on the map to select location",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }
        }
    }
}

/**
 * Launch Google Places Autocomplete activity
 */
private fun launchPlacesAutocomplete(
    context: Context,
    launcher: androidx.activity.result.ActivityResultLauncher<Intent>
) {
    try {
        // Set the fields to specify which types of place data to return
        val fields = listOf(
            Place.Field.ID,
            Place.Field.NAME,
            Place.Field.ADDRESS,
            Place.Field.LAT_LNG
        )

        // Start the autocomplete intent
        val intent = Autocomplete.IntentBuilder(AutocompleteActivityMode.OVERLAY, fields)
            .build(context)
        
        launcher.launch(intent)
    } catch (e: Exception) {
        // Handle error - API key not configured or other issue
        e.printStackTrace()
    }
}

/**
 * Get Places API key from manifest or resources
 * You should add your API key to AndroidManifest.xml:
 * <meta-data
 *     android:name="com.google.android.geo.API_KEY"
 *     android:value="YOUR_API_KEY_HERE"/>
 */
private fun getPlacesApiKey(context: Context): String {
    return try {
        val applicationInfo = context.packageManager.getApplicationInfo(
            context.packageName,
            android.content.pm.PackageManager.GET_META_DATA
        )
        applicationInfo.metaData?.getString("com.google.android.geo.API_KEY") ?: ""
    } catch (e: Exception) {
        ""
    }
}
