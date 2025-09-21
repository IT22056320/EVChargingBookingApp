/*
 * File: ChargingStation.kt
 * Description: Charging station model class (Kotlin version)
 * Author: EV Charging Team
 * Date: September 21, 2025
 */
package com.example.evchargingmobile.models

data class ChargingStation(
    var id: String? = null,
    var name: String = "",
    var location: String = "",
    var address: String = "",
    var latitude: Double = 0.0,
    var longitude: Double = 0.0,
    var type: StationType = StationType.AC,
    var totalSlots: Int = 0,
    var availableSlots: Int = 0,
    var isActive: Boolean = true,
    var operatorId: String? = null,
    var createdAt: String? = null,
    var updatedAt: String? = null,
    var pricePerHour: Double = 0.0,
    var operatingHours: String? = null
) {
    enum class StationType {
        AC,
        DC,
        BOTH
    }

    /**
     * Constructor for creating charging station with essential fields
     */
    constructor(
        name: String,
        location: String,
        address: String,
        latitude: Double,
        longitude: Double,
        type: StationType,
        totalSlots: Int
    ) : this(
        name = name,
        location = location,
        address = address,
        latitude = latitude,
        longitude = longitude,
        type = type,
        totalSlots = totalSlots,
        availableSlots = totalSlots,
        isActive = true
    )

    /**
     * Check if station has available slots
     */
    fun hasAvailableSlots(): Boolean = availableSlots > 0

    /**
     * Get availability percentage
     */
    fun getAvailabilityPercentage(): Int {
        return if (totalSlots > 0) {
            (availableSlots * 100) / totalSlots
        } else 0
    }

    /**
     * Get distance from given coordinates (simple calculation)
     */
    fun getDistanceFrom(userLat: Double, userLng: Double): Double {
        val earthRadius = 6371.0 // Earth's radius in kilometers
        val latDiff = Math.toRadians(latitude - userLat)
        val lngDiff = Math.toRadians(longitude - userLng)

        val a = Math.sin(latDiff / 2) * Math.sin(latDiff / 2) +
                Math.cos(Math.toRadians(userLat)) * Math.cos(Math.toRadians(latitude)) *
                Math.sin(lngDiff / 2) * Math.sin(lngDiff / 2)

        val c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a))
        return earthRadius * c
    }
}
