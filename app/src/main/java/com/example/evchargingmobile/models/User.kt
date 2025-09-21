/*
 * File: User.kt
 * Description: User model class for EV owners and station operators (Kotlin version)
 * Author: EV Charging Team
 * Date: September 21, 2025
 */
package com.example.evchargingmobile.models

data class User(
    var id: String? = null,
    var nic: String = "",
    var fullName: String = "",
    var email: String = "",
    var password: String? = null,
    var phoneNumber: String? = null,
    var address: String? = null,
    var isActive: Boolean = true,
    var isApproved: Boolean = false,
    var registeredAt: String? = null,
    var approvedAt: String? = null,
    var approvedBy: String? = null,
    var lastLoginAt: String? = null,
    var userType: UserType = UserType.EV_OWNER
) {
    enum class UserType {
        EV_OWNER,
        STATION_OPERATOR
    }

    /**
     * Constructor for creating new user with essential fields
     */
    constructor(
        nic: String,
        fullName: String,
        email: String,
        password: String,
        phoneNumber: String?,
        address: String?,
        userType: UserType
    ) : this(
        nic = nic,
        fullName = fullName,
        email = email,
        password = password,
        phoneNumber = phoneNumber,
        address = address,
        userType = userType,
        isActive = true,
        isApproved = false
    )
}
