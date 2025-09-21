/*
 * File: Booking.kt
 * Description: Booking model class for EV charging reservations (Kotlin version)
 * Author: EV Charging Team
 * Date: September 21, 2025
 */
package com.example.evchargingmobile.models

data class Booking(
    var id: String? = null,
    var evOwnerNic: String = "",
    var chargingStationId: String = "",
    var reservationDateTime: String = "",
    var bookingDate: String = "",
    var status: BookingStatus = BookingStatus.PENDING,
    var qrCode: String? = null,
    var createdAt: String? = null,
    var updatedAt: String? = null,
    var cancelledAt: String? = null,
    var confirmedAt: String? = null,
    var completedAt: String? = null,
    var stationName: String? = null,
    var stationLocation: String? = null,
    var stationType: String? = null,
    var estimatedDuration: Double = 0.0,
    var estimatedCost: Double = 0.0
) {
    enum class BookingStatus {
        PENDING,
        APPROVED,
        CONFIRMED,
        ACTIVE,      // Added ACTIVE status for ongoing charging sessions
        COMPLETED,
        CANCELLED
    }

    /**
     * Constructor for creating new booking with essential fields
     */
    constructor(
        evOwnerNic: String,
        chargingStationId: String,
        reservationDateTime: String
    ) : this(
        evOwnerNic = evOwnerNic,
        chargingStationId = chargingStationId,
        reservationDateTime = reservationDateTime,
        status = BookingStatus.PENDING,
        bookingDate = java.time.LocalDateTime.now().toString()
    )

    /**
     * Check if booking can be modified (at least 12 hours before reservation)
     */
    fun canBeModified(): Boolean {
        return try {
            val reservationTime = java.time.LocalDateTime.parse(reservationDateTime)
            val now = java.time.LocalDateTime.now()
            val hoursDifference = java.time.Duration.between(now, reservationTime).toHours()
            hoursDifference >= 12
        } catch (e: Exception) {
            false
        }
    }

    /**
     * Get status color for UI display
     */
    fun getStatusColor(): String {
        return when (status) {
            BookingStatus.PENDING -> "#FF9800"
            BookingStatus.APPROVED -> "#4CAF50"
            BookingStatus.CONFIRMED -> "#2196F3"
            BookingStatus.COMPLETED -> "#9C27B0"
            BookingStatus.CANCELLED -> "#F44336"
            BookingStatus.ACTIVE -> "#FF5722" // Color for ACTIVE status
        }
    }
}
