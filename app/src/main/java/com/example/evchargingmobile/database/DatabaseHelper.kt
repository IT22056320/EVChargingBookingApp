/*
 * File: DatabaseHelper.kt
 * Description: SQLite database helper for local data storage (Kotlin version)
 * Author: EV Charging Team
 * Date: September 21, 2025
 */
package com.example.evchargingmobile.database

import android.content.ContentValues
import android.content.Context
import android.database.sqlite.SQLiteDatabase
import android.database.sqlite.SQLiteOpenHelper
import com.example.evchargingmobile.models.User
import com.example.evchargingmobile.models.Booking
import com.example.evchargingmobile.models.ChargingStation

class DatabaseHelper(context: Context) : SQLiteOpenHelper(context, DATABASE_NAME, null, DATABASE_VERSION) {

    companion object {
        private const val DATABASE_NAME = "EVChargingApp.db"
        private const val DATABASE_VERSION = 1

        // Table names
        private const val TABLE_USERS = "users"
        private const val TABLE_BOOKINGS = "bookings"
        private const val TABLE_CHARGING_STATIONS = "charging_stations"

        // Users table columns
        private const val USER_ID = "id"
        private const val USER_NIC = "nic"
        private const val USER_FULL_NAME = "full_name"
        private const val USER_EMAIL = "email"
        private const val USER_PASSWORD = "password"
        private const val USER_PHONE = "phone_number"
        private const val USER_ADDRESS = "address"
        private const val USER_IS_ACTIVE = "is_active"
        private const val USER_IS_APPROVED = "is_approved"
        private const val USER_TYPE = "user_type"
        private const val USER_LAST_LOGIN = "last_login_at"

        // Bookings table columns
        private const val BOOKING_ID = "id"
        private const val BOOKING_EV_OWNER_NIC = "ev_owner_nic"
        private const val BOOKING_STATION_ID = "charging_station_id"
        private const val BOOKING_RESERVATION_DATETIME = "reservation_datetime"
        private const val BOOKING_STATUS = "status"
        private const val BOOKING_QR_CODE = "qr_code"
        private const val BOOKING_CREATED_AT = "created_at"

        // Charging stations table columns
        private const val STATION_ID = "id"
        private const val STATION_NAME = "name"
        private const val STATION_LOCATION = "location"
        private const val STATION_ADDRESS = "address"
        private const val STATION_LATITUDE = "latitude"
        private const val STATION_LONGITUDE = "longitude"
        private const val STATION_TYPE = "type"
        private const val STATION_TOTAL_SLOTS = "total_slots"
        private const val STATION_AVAILABLE_SLOTS = "available_slots"
        private const val STATION_IS_ACTIVE = "is_active"
    }

    override fun onCreate(db: SQLiteDatabase) {
        // Create users table
        val createUsersTable = """
            CREATE TABLE $TABLE_USERS (
                $USER_ID TEXT PRIMARY KEY,
                $USER_NIC TEXT UNIQUE,
                $USER_FULL_NAME TEXT,
                $USER_EMAIL TEXT,
                $USER_PASSWORD TEXT,
                $USER_PHONE TEXT,
                $USER_ADDRESS TEXT,
                $USER_IS_ACTIVE INTEGER,
                $USER_IS_APPROVED INTEGER,
                $USER_TYPE TEXT,
                $USER_LAST_LOGIN TEXT
            )
        """.trimIndent()

        // Create bookings table
        val createBookingsTable = """
            CREATE TABLE $TABLE_BOOKINGS (
                $BOOKING_ID TEXT PRIMARY KEY,
                $BOOKING_EV_OWNER_NIC TEXT,
                $BOOKING_STATION_ID TEXT,
                $BOOKING_RESERVATION_DATETIME TEXT,
                $BOOKING_STATUS TEXT,
                $BOOKING_QR_CODE TEXT,
                $BOOKING_CREATED_AT TEXT
            )
        """.trimIndent()

        // Create charging stations table
        val createStationsTable = """
            CREATE TABLE $TABLE_CHARGING_STATIONS (
                $STATION_ID TEXT PRIMARY KEY,
                $STATION_NAME TEXT,
                $STATION_LOCATION TEXT,
                $STATION_ADDRESS TEXT,
                $STATION_LATITUDE REAL,
                $STATION_LONGITUDE REAL,
                $STATION_TYPE TEXT,
                $STATION_TOTAL_SLOTS INTEGER,
                $STATION_AVAILABLE_SLOTS INTEGER,
                $STATION_IS_ACTIVE INTEGER
            )
        """.trimIndent()

        db.execSQL(createUsersTable)
        db.execSQL(createBookingsTable)
        db.execSQL(createStationsTable)
    }

    override fun onUpgrade(db: SQLiteDatabase, oldVersion: Int, newVersion: Int) {
        db.execSQL("DROP TABLE IF EXISTS $TABLE_USERS")
        db.execSQL("DROP TABLE IF EXISTS $TABLE_BOOKINGS")
        db.execSQL("DROP TABLE IF EXISTS $TABLE_CHARGING_STATIONS")
        onCreate(db)
    }

    /**
     * Insert or update user in local database
     */
    fun saveUser(user: User): Long {
        val db = writableDatabase
        val values = ContentValues().apply {
            put(USER_ID, user.id)
            put(USER_NIC, user.nic)
            put(USER_FULL_NAME, user.fullName)
            put(USER_EMAIL, user.email)
            put(USER_PASSWORD, user.password)
            put(USER_PHONE, user.phoneNumber)
            put(USER_ADDRESS, user.address)
            put(USER_IS_ACTIVE, if (user.isActive) 1 else 0)
            put(USER_IS_APPROVED, if (user.isApproved) 1 else 0)
            put(USER_TYPE, user.userType.toString())
            put(USER_LAST_LOGIN, user.lastLoginAt)
        }

        val result = db.insertWithOnConflict(TABLE_USERS, null, values, SQLiteDatabase.CONFLICT_REPLACE)
        db.close()
        return result
    }

    /**
     * Get user by NIC with Kotlin null safety
     */
    fun getUserByNIC(nic: String): User? {
        val db = readableDatabase
        var user: User? = null

        val cursor = db.query(
            TABLE_USERS, null, "$USER_NIC=?",
            arrayOf(nic), null, null, null
        )

        cursor?.use {
            if (it.moveToFirst()) {
                user = User().apply {
                    id = it.getString(it.getColumnIndexOrThrow(USER_ID))
                    this.nic = it.getString(it.getColumnIndexOrThrow(USER_NIC))
                    fullName = it.getString(it.getColumnIndexOrThrow(USER_FULL_NAME))
                    email = it.getString(it.getColumnIndexOrThrow(USER_EMAIL))
                    password = it.getString(it.getColumnIndexOrThrow(USER_PASSWORD))
                    phoneNumber = it.getString(it.getColumnIndexOrThrow(USER_PHONE))
                    address = it.getString(it.getColumnIndexOrThrow(USER_ADDRESS))
                    isActive = it.getInt(it.getColumnIndexOrThrow(USER_IS_ACTIVE)) == 1
                    isApproved = it.getInt(it.getColumnIndexOrThrow(USER_IS_APPROVED)) == 1
                    userType = User.UserType.valueOf(it.getString(it.getColumnIndexOrThrow(USER_TYPE)))
                    lastLoginAt = it.getString(it.getColumnIndexOrThrow(USER_LAST_LOGIN))
                }
            }
        }

        db.close()
        return user
    }

    /**
     * Save booking to local database
     */
    fun saveBooking(booking: Booking): Long {
        val db = writableDatabase
        val values = ContentValues().apply {
            put(BOOKING_ID, booking.id)
            put(BOOKING_EV_OWNER_NIC, booking.evOwnerNic)
            put(BOOKING_STATION_ID, booking.chargingStationId)
            put(BOOKING_RESERVATION_DATETIME, booking.reservationDateTime)
            put(BOOKING_STATUS, booking.status.toString())
            put(BOOKING_QR_CODE, booking.qrCode)
            put(BOOKING_CREATED_AT, booking.createdAt)
        }

        val result = db.insertWithOnConflict(TABLE_BOOKINGS, null, values, SQLiteDatabase.CONFLICT_REPLACE)
        db.close()
        return result
    }

    /**
     * Get all bookings for a user with Kotlin collections
     */
    fun getBookingsByUser(evOwnerNic: String): List<Booking> {
        val bookings = mutableListOf<Booking>()
        val db = readableDatabase

        val cursor = db.query(
            TABLE_BOOKINGS, null, "$BOOKING_EV_OWNER_NIC=?",
            arrayOf(evOwnerNic), null, null, "$BOOKING_CREATED_AT DESC"
        )

        cursor?.use {
            while (it.moveToNext()) {
                val booking = Booking(
                    id = it.getString(it.getColumnIndexOrThrow(BOOKING_ID)),
                    evOwnerNic = it.getString(it.getColumnIndexOrThrow(BOOKING_EV_OWNER_NIC)),
                    chargingStationId = it.getString(it.getColumnIndexOrThrow(BOOKING_STATION_ID)),
                    reservationDateTime = it.getString(it.getColumnIndexOrThrow(BOOKING_RESERVATION_DATETIME)),
                    status = Booking.BookingStatus.valueOf(it.getString(it.getColumnIndexOrThrow(BOOKING_STATUS))),
                    qrCode = it.getString(it.getColumnIndexOrThrow(BOOKING_QR_CODE)),
                    createdAt = it.getString(it.getColumnIndexOrThrow(BOOKING_CREATED_AT))
                )
                bookings.add(booking)
            }
        }

        db.close()
        return bookings
    }

    /**
     * Get all active charging stations with Kotlin collections
     */
    fun getAllActiveStations(): List<ChargingStation> {
        val stations = mutableListOf<ChargingStation>()
        val db = readableDatabase

        val cursor = db.query(
            TABLE_CHARGING_STATIONS, null, "$STATION_IS_ACTIVE=?",
            arrayOf("1"), null, null, STATION_NAME
        )

        cursor?.use {
            while (it.moveToNext()) {
                val station = ChargingStation(
                    id = it.getString(it.getColumnIndexOrThrow(STATION_ID)),
                    name = it.getString(it.getColumnIndexOrThrow(STATION_NAME)),
                    location = it.getString(it.getColumnIndexOrThrow(STATION_LOCATION)),
                    address = it.getString(it.getColumnIndexOrThrow(STATION_ADDRESS)),
                    latitude = it.getDouble(it.getColumnIndexOrThrow(STATION_LATITUDE)),
                    longitude = it.getDouble(it.getColumnIndexOrThrow(STATION_LONGITUDE)),
                    type = ChargingStation.StationType.valueOf(it.getString(it.getColumnIndexOrThrow(STATION_TYPE))),
                    totalSlots = it.getInt(it.getColumnIndexOrThrow(STATION_TOTAL_SLOTS)),
                    availableSlots = it.getInt(it.getColumnIndexOrThrow(STATION_AVAILABLE_SLOTS)),
                    isActive = it.getInt(it.getColumnIndexOrThrow(STATION_IS_ACTIVE)) == 1
                )
                stations.add(station)
            }
        }

        db.close()
        return stations
    }

    /**
     * Save charging station to local database
     */
    fun saveChargingStation(station: ChargingStation): Long {
        val db = writableDatabase
        val values = ContentValues().apply {
            put(STATION_ID, station.id)
            put(STATION_NAME, station.name)
            put(STATION_LOCATION, station.location)
            put(STATION_ADDRESS, station.address)
            put(STATION_LATITUDE, station.latitude)
            put(STATION_LONGITUDE, station.longitude)
            put(STATION_TYPE, station.type.toString())
            put(STATION_TOTAL_SLOTS, station.totalSlots)
            put(STATION_AVAILABLE_SLOTS, station.availableSlots)
            put(STATION_IS_ACTIVE, if (station.isActive) 1 else 0)
        }

        val result = db.insertWithOnConflict(TABLE_CHARGING_STATIONS, null, values, SQLiteDatabase.CONFLICT_REPLACE)
        db.close()
        return result
    }
}
