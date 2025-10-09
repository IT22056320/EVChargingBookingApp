/*
 * File: ApiService.kt
 * Description: API service to communicate with C# backend (Kotlin version with coroutines)
 * Author: EV Charging Team
 * Date: September 21, 2025 (updated)
 */
package com.example.evchargingmobile.network

import android.util.Log
import com.example.evchargingmobile.models.Booking
import com.example.evchargingmobile.models.User
import com.google.gson.Gson
import com.google.gson.GsonBuilder
import com.google.gson.JsonObject
import com.google.gson.annotations.SerializedName
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlinx.coroutines.withContext
import okhttp3.Call
import okhttp3.Callback
import okhttp3.MediaType
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import okhttp3.Response
import java.io.IOException
import java.security.cert.X509Certificate
import java.util.concurrent.TimeUnit
import javax.net.ssl.SSLContext
import javax.net.ssl.TrustManager
import javax.net.ssl.X509TrustManager
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException

class ApiService private constructor() {

    companion object {
        private const val EMULATOR_URL = "https://10.0.2.2:5001/api/"
        private const val LOCALHOST_URL = "https://localhost:7180/api/"
        private const val HTTP_EMULATOR_URL = "http://10.0.2.2:5001/api/"

        private const val BASE_URL = " https://bd8f93d45a1d.ngrok-free.app/api/"
        private const val TAG = "ApiService"

        @Volatile
        private var INSTANCE: ApiService? = null

        fun getInstance(): ApiService {
            return INSTANCE ?: synchronized(this) {
                INSTANCE ?: ApiService().also { INSTANCE = it }
            }
        }
    }

    private val client: OkHttpClient
    private val gson: Gson
    private val jsonMediaType: MediaType = "application/json".toMediaType()

    init {
        client = OkHttpClient.Builder()
            .connectTimeout(10, TimeUnit.SECONDS)
            .readTimeout(20, TimeUnit.SECONDS)
            .writeTimeout(20, TimeUnit.SECONDS)
            .retryOnConnectionFailure(true)
            .hostnameVerifier { _, _ -> true }
            .sslSocketFactory(createTrustAllSSLContext().socketFactory, createTrustAllManager())
            .build()

        gson = GsonBuilder()
            .setDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'")
            .create()
    }

    /**
     * Register new EV owner (Kotlin coroutines version)
     */
    suspend fun registerEVOwner(user: User): Result<RegisterResponse> = withContext(Dispatchers.IO) {
        try {
            val request = RegisterRequest(
                NIC = user.nic,
                FullName = user.fullName,
                Email = user.email,
                Password = user.password ?: "",
                PhoneNumber = user.phoneNumber ?: "",
                Address = user.address ?: "",
                Latitude = user.latitude,
                Longitude = user.longitude
            )

            val body = gson.toJson(request).toRequestBody(jsonMediaType)

            val httpRequest = Request.Builder()
                .url(buildUrl("EVOwners/register"))
                .post(body)
                .addHeader("Content-Type", "application/json")
                .build()

            executeRequest(httpRequest) { responseBody ->
                gson.fromJson(responseBody, RegisterResponse::class.java)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error registering EV owner", e)
            Result.failure(e)
        }
    }

    /**
     * Login EV owner with email-based authentication (returns populated user object)
     */
    suspend fun loginEVOwner(email: String, password: String): Result<LoginResponse> = withContext(Dispatchers.IO) {
        try {
            val requestBody = gson.toJson(LoginRequest(email, password)).toRequestBody(jsonMediaType)

            val httpRequest = Request.Builder()
                .url(buildUrl("EVOwners/login"))
                .post(requestBody)
                .addHeader("Content-Type", "application/json")
                .build()

            executeRequest(httpRequest) { responseBody ->
                val jsonObj = gson.fromJson(responseBody, JsonObject::class.java)
                val userJson = jsonObj.getAsJsonObject("user")
                val user = userJson?.let { parseEVOwnerUser(it) }
                LoginResponse(
                    message = jsonObj.get("message")?.asString ?: "Login successful",
                    user = user,
                    token = null
                )
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error logging in EV owner", e)
            Result.failure(e)
        }
    }

    /**
     * Login station operator (web user) and map to unified user model
     */
    suspend fun loginStationOperator(email: String, password: String): Result<LoginResponse> = withContext(Dispatchers.IO) {
        try {
            val requestBody = gson.toJson(WebUserLoginRequest(email, password)).toRequestBody(jsonMediaType)

            val httpRequest = Request.Builder()
                .url(buildUrl("WebUsers/login"))
                .post(requestBody)
                .addHeader("Content-Type", "application/json")
                .build()

            executeRequest(httpRequest) { responseBody ->
                val jsonObj = gson.fromJson(responseBody, JsonObject::class.java)
                val userJson = jsonObj.getAsJsonObject("user")
                val user = userJson?.let { parseStationOperatorUser(it) }
                LoginResponse(
                    message = jsonObj.get("message")?.asString ?: "Login successful",
                    user = user,
                    token = null
                )
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error logging in station operator", e)
            Result.failure(e)
        }
    }

    /**
     * Update EV owner profile (phone and address with coordinates)
     */
    suspend fun updateEVOwner(
        nic: String,
        phone: String,
        address: String,
        latitude: Double?,
        longitude: Double?
    ): Result<User> = withContext(Dispatchers.IO) {
        try {
            val request = UpdateProfileRequest(
                PhoneNumber = phone,
                Address = address,
                Latitude = latitude,
                Longitude = longitude
            )

            val body = gson.toJson(request).toRequestBody(jsonMediaType)

            val httpRequest = Request.Builder()
                .url(buildUrl("EVOwners/$nic"))
                .put(body)
                .addHeader("Content-Type", "application/json")
                .build()

            executeRequest(httpRequest) { responseBody ->
                val jsonObj = gson.fromJson(responseBody, JsonObject::class.java)
                parseEVOwnerUser(jsonObj)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error updating EV owner profile", e)
            Result.failure(e)
        }
    }

    /**
     * Unified login flow – tries EV owner first, then station operator
     */
    suspend fun loginUser(email: String, password: String): Result<UnifiedLoginResponse> = withContext(Dispatchers.IO) {
        try {
            val evOwnerResult = loginEVOwner(email, password)
            if (evOwnerResult.isSuccess) {
                val evOwner = evOwnerResult.getOrNull()
                return@withContext Result.success(UnifiedLoginResponse(
                    message = evOwner?.message ?: "Login successful",
                    userType = User.UserType.EV_OWNER.name,
                    user = evOwner?.user,
                    token = evOwner?.token
                ))
            }

            val operatorResult = loginStationOperator(email, password)
            if (operatorResult.isSuccess) {
                val operator = operatorResult.getOrNull()
                return@withContext Result.success(UnifiedLoginResponse(
                    message = operator?.message ?: "Login successful",
                    userType = User.UserType.STATION_OPERATOR.name,
                    user = operator?.user,
                    token = operator?.token
                ))
            }

            Result.failure(Exception("Invalid email or password"))
        } catch (e: Exception) {
            Log.e(TAG, "Unified login error", e)
            Result.failure(e)
        }
    }

    /**
     * Get EV owner by NIC
     */
    suspend fun getEVOwnerByNIC(nic: String): Result<User> = withContext(Dispatchers.IO) {
        try {
            val request = Request.Builder()
                .url(buildUrl("EVOwners/$nic"))
                .get()
                .build()

            executeRequest(request) { responseBody ->
                gson.fromJson(responseBody, User::class.java).apply {
                    userType = User.UserType.EV_OWNER
                }
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error fetching EV owner", e)
            Result.failure(e)
        }
    }

    /**
     * Fetch all charging stations with optional filters
     */
    suspend fun fetchChargingStations(
        connectorType: String? = null,
        onlyAvailable: Boolean = false
    ): Result<List<ChargingStationResponse>> = withContext(Dispatchers.IO) {
        try {
            val query = mutableMapOf<String, String?>()
            if (!connectorType.isNullOrBlank()) query["connectorType"] = connectorType
            if (onlyAvailable) query["onlyAvailable"] = "true"

            val request = Request.Builder()
                .url(buildUrl("ChargingStations", query))
                .get()
                .build()

            executeRequest(request) { responseBody ->
                val array = gson.fromJson(responseBody, Array<ChargingStationResponse>::class.java)
                array?.toList() ?: emptyList()
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error fetching charging stations", e)
            Result.failure(e)
        }
    }

    /**
     * Create a booking
     */
    suspend fun createBooking(request: CreateBookingRequest): Result<BookingResponse> = withContext(Dispatchers.IO) {
        try {
            val jsonBody = gson.toJson(request)
            Log.d(TAG, "=== CREATE BOOKING REQUEST ===")
            Log.d(TAG, "URL: ${buildUrl("Bookings")}")
            Log.d(TAG, "Method: POST")
            Log.d(TAG, "Request Body: $jsonBody")
            Log.d(TAG, "Request Object: $request")
            
            val body = jsonBody.toRequestBody(jsonMediaType)
            val httpRequest = Request.Builder()
                .url(buildUrl("Bookings"))
                .post(body)
                .addHeader("Content-Type", "application/json")
                .build()

            Log.d(TAG, "Headers: ${httpRequest.headers}")
            
            executeRequest(httpRequest) { responseBody ->
                Log.d(TAG, "=== CREATE BOOKING RESPONSE ===")
                Log.d(TAG, "Response Body: $responseBody")
                gson.fromJson(responseBody, BookingResponse::class.java)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error creating booking", e)
            Log.e(TAG, "Exception message: ${e.message}")
            Log.e(TAG, "Exception stack trace: ${e.stackTraceToString()}")
            Result.failure(e)
        }
    }

    /**
     * Retrieve bookings for a user (userId can be NIC)
     */
    suspend fun getBookingsByNic(
        userId: String,
        page: Int = 1,
        pageSize: Int = 50
    ): Result<BookingPagedResponse> = withContext(Dispatchers.IO) {
        try {
            val request = Request.Builder()
                .url(buildUrl("Bookings/user/$userId", mapOf("page" to page.toString(), "pageSize" to pageSize.toString())))
                .get()
                .build()

            executeRequest(request) { responseBody ->
                gson.fromJson(responseBody, BookingPagedResponse::class.java)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error fetching user bookings", e)
            Result.failure(e)
        }
    }

    /**
     * Retrieve a booking by ID
     */
    suspend fun getBookingById(bookingId: String): Result<BookingResponse> = withContext(Dispatchers.IO) {
        try {
            val request = Request.Builder()
                .url(buildUrl("Bookings/$bookingId"))
                .get()
                .build()

            executeRequest(request) { responseBody ->
                gson.fromJson(responseBody, BookingResponse::class.java)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error fetching booking", e)
            Result.failure(e)
        }
    }

    /**
     * Update existing booking
     */
    suspend fun updateBooking(
        bookingId: String,
        request: UpdateBookingRequest
    ): Result<BookingResponse> = withContext(Dispatchers.IO) {
        try {
            val body = gson.toJson(request).toRequestBody(jsonMediaType)
            val httpRequest = Request.Builder()
                .url(buildUrl("Bookings/$bookingId"))
                .put(body)
                .addHeader("Content-Type", "application/json")
                .build()

            executeRequest(httpRequest) { responseBody ->
                gson.fromJson(responseBody, BookingResponse::class.java)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error updating booking", e)
            Result.failure(e)
        }
    }

    /**
     * Cancel booking with reason
     */
    suspend fun cancelBooking(
        bookingId: String,
        request: CancelBookingRequest
    ): Result<ApiMessageResponse> = withContext(Dispatchers.IO) {
        try {
            val body = gson.toJson(request).toRequestBody(jsonMediaType)
            val httpRequest = Request.Builder()
                .url(buildUrl("Bookings/$bookingId/cancel"))
                .post(body)
                .addHeader("Content-Type", "application/json")
                .build()

            executeRequest(httpRequest) { responseBody ->
                gson.fromJson(responseBody, ApiMessageResponse::class.java)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error cancelling booking", e)
            Result.failure(e)
        }
    }

    /**
     * Check time-slot availability for booking creation/update
     */
    suspend fun checkTimeSlotAvailability(request: TimeSlotCheckRequest): Result<TimeSlotAvailabilityResponse> = withContext(Dispatchers.IO) {
        try {
            val body = gson.toJson(request).toRequestBody(jsonMediaType)
            val httpRequest = Request.Builder()
                .url(buildUrl("Bookings/check-availability"))
                .post(body)
                .addHeader("Content-Type", "application/json")
                .build()

            executeRequest(httpRequest) { responseBody ->
                gson.fromJson(responseBody, TimeSlotAvailabilityResponse::class.java)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error checking time slot", e)
            Result.failure(e)
        }
    }

    // region Networking helpers

    private suspend fun <T> executeRequest(request: Request, parser: (String) -> T): Result<T> {
        return try {
            Log.d(TAG, "Executing request to: ${request.url}")
            val response = client.newCall(request).await()
            val body = response.body?.string() ?: ""

            Log.d(TAG, "Response Code: ${response.code}")
            Log.d(TAG, "Response Message: ${response.message}")
            Log.d(TAG, "Response Headers: ${response.headers}")
            Log.d(TAG, "Response Body: ${body.take(500)}") // First 500 chars

            if (response.isSuccessful) {
                try {
                    val parsed = parser(body)
                    Log.d(TAG, "Successfully parsed response")
                    Result.success(parsed)
                } catch (parseError: Exception) {
                    Log.e(TAG, "Parsing error for ${request.url}", parseError)
                    Log.e(TAG, "Parse error message: ${parseError.message}")
                    Log.e(TAG, "Response body was: $body")
                    Result.failure(parseError)
                }
            } else {
                val errorMessage = extractErrorMessage(body)
                Log.e(TAG, "Request failed: ${response.code} - $errorMessage")
                Log.e(TAG, "Full error body: $body")
                Result.failure(Exception(errorMessage.ifBlank { response.message }))
            }
        } catch (io: IOException) {
            Log.e(TAG, "Network error for ${request.url}", io)
            Log.e(TAG, "IO Exception: ${io.message}")
            Result.failure(io)
        }
    }

    private fun extractErrorMessage(body: String?): String {
        if (body.isNullOrBlank()) return "Unexpected server error"
        return try {
            val jsonObj = gson.fromJson(body, JsonObject::class.java)
            when {
                jsonObj.has("message") -> jsonObj.get("message").asString
                jsonObj.has("error") -> jsonObj.get("error").asString
                else -> body
            }
        } catch (_: Exception) {
            body
        }
    }

    private fun buildUrl(path: String, query: Map<String, String?> = emptyMap()): String {
        val base = if (BASE_URL.endsWith("/")) BASE_URL else "$BASE_URL/"
        val urlBuilder = StringBuilder().append(base).append(path)
        val filtered = query.filterValues { !it.isNullOrBlank() }
        if (filtered.isNotEmpty()) {
            urlBuilder.append("?")
            urlBuilder.append(filtered.entries.joinToString("&") { entry ->
                "${entry.key}=${entry.value}"
            })
        }
        return urlBuilder.toString()
    }

    private suspend fun Call.await(): Response = suspendCancellableCoroutine { continuation ->
        enqueue(object : Callback {
            override fun onResponse(call: Call, response: Response) {
                continuation.resume(response)
            }

            override fun onFailure(call: Call, e: IOException) {
                continuation.resumeWithException(e)
            }
        })

        continuation.invokeOnCancellation { cancel() }
    }

    private fun createTrustAllSSLContext(): SSLContext {
        return SSLContext.getInstance("TLS").apply {
            init(null, arrayOf(createTrustAllManager()), java.security.SecureRandom())
        }
    }

    private fun createTrustAllManager(): X509TrustManager {
        return object : X509TrustManager {
            override fun checkClientTrusted(chain: Array<X509Certificate>, authType: String) {}
            override fun checkServerTrusted(chain: Array<X509Certificate>, authType: String) {}
            override fun getAcceptedIssuers(): Array<X509Certificate> = arrayOf()
        }
    }

    // endregion

    // region Parsing helpers

    private fun parseEVOwnerUser(json: JsonObject): User {
        return User(
            id = json.get("id")?.asString,
            nic = json.get("nic")?.asString ?: json.get("NIC")?.asString ?: "",
            fullName = json.get("fullName")?.asString ?: json.get("FullName")?.asString ?: "",
            email = json.get("email")?.asString ?: "",
            phoneNumber = json.get("phoneNumber")?.asString,
            address = json.get("address")?.asString,
            isActive = json.get("isActive")?.asBoolean ?: true,
            isApproved = json.get("isApproved")?.asBoolean ?: false,
            registeredAt = json.get("registeredAt")?.asString,
            approvedAt = json.get("approvedAt")?.asString,
            approvedBy = json.get("approvedBy")?.asString,
            lastLoginAt = json.get("lastLoginAt")?.asString,
            userType = User.UserType.EV_OWNER
        )
    }

    private fun parseStationOperatorUser(json: JsonObject): User {
        // Try to extract station ID from various possible fields
        val stationId = json.get("chargingStationId")?.asString 
            ?: json.get("stationId")?.asString
            ?: json.get("assignedStationId")?.asString
        
        return User(
            id = json.get("id")?.asString,
            nic = json.get("id")?.asString ?: json.get("email")?.asString ?: "",
            fullName = json.get("fullName")?.asString ?: "",
            email = json.get("email")?.asString ?: "",
            phoneNumber = null,
            address = stationId, // Store stationId in address field for later use
            isActive = json.get("isActive")?.asBoolean ?: true,
            isApproved = true,
            registeredAt = json.get("createdAt")?.asString,
            approvedAt = json.get("createdAt")?.asString,
            approvedBy = json.get("role")?.asString,
            lastLoginAt = json.get("lastLoginAt")?.asString,
            userType = User.UserType.STATION_OPERATOR
        )
    }

    // endregion

    // region Data classes

    data class RegisterRequest(
        val NIC: String,
        val FullName: String,
        val Email: String,
        val Password: String,
        val PhoneNumber: String,
        val Address: String,
        val Latitude: Double? = null,
        val Longitude: Double? = null
    )

    data class UpdateProfileRequest(
        val PhoneNumber: String,
        val Address: String,
        val Latitude: Double?,
        val Longitude: Double?
    )

    data class RegisterResponse(
        val message: String,
        val user: User?
    )

    data class LoginRequest(
        val Email: String,
        val Password: String
    )

    data class LoginResponse(
        val message: String,
        val user: User?,
        val token: String?
    )

    data class WebUserLoginRequest(
        val Email: String,
        val Password: String
    )

    data class UnifiedLoginResponse(
        val message: String,
        val userType: String,
        val user: User?,
        val token: String?
    )

    data class CreateBookingRequest(
        val UserId: String,
        val ChargingStationId: String,
        val BookingDate: String,
        val StartTime: String,
        val EndTime: String,
        val VehicleNumber: String,
        val VehicleType: String,
        val EstimatedChargingTimeMinutes: Int,
        val Notes: String? = null
    )

    data class UpdateBookingRequest(
        val BookingDate: String? = null,
        val StartTime: String? = null,
        val EndTime: String? = null,
        val VehicleNumber: String? = null,
        val VehicleType: String? = null,
        val EstimatedChargingTimeMinutes: Int? = null,
        val Notes: String? = null
    )

    data class CancelBookingRequest(
        val CancelledBy: String,
        val CancellationReason: String
    )

    data class ApiMessageResponse(
        val message: String
    )

    data class TimeSlotCheckRequest(
        val ChargingStationId: String,
        val Date: String,
        val StartTime: String,
        val EndTime: String,
        val ExcludeBookingId: String? = null
    )

    data class TimeSlotAvailabilityResponse(
        @SerializedName("isAvailable") val isAvailable: Boolean,
        val message: String,
        val conflictingBookings: List<ConflictingBookingResponse> = emptyList()
    )

    data class ConflictingBookingResponse(
        val bookingId: String,
        val startTime: String,
        val endTime: String,
        val status: String,
        val userName: String
    )

    data class BookingPagedResponse(
        val bookings: List<BookingResponse> = emptyList(),
        val totalCount: Int = 0,
        val page: Int = 1,
        val pageSize: Int = 0,
        val totalPages: Int = 0,
        val hasNextPage: Boolean = false,
        val hasPreviousPage: Boolean = false
    )

    data class BookingResponse(
        val id: String = "",
        val bookingNumber: String = "",
        val userId: String = "",
        val chargingStationId: String = "",
        val bookingDate: String = "",
        val startTime: String = "",
        val endTime: String = "",
        val status: String = "",
        val vehicleNumber: String = "",
        val vehicleType: String = "",
        val estimatedChargingTimeMinutes: Int = 0,
        val notes: String? = null,
        val qrCode: String? = null,
        val qrCodeGeneratedAt: String? = null,
        val createdAt: String? = null,
        val modifiedAt: String? = null,
        val approvedAt: String? = null,
        val approvedBy: String? = null,
        val completedAt: String? = null,
        val cancelledAt: String? = null,
        val cancelledBy: String? = null,
        val cancellationReason: String? = null,
        val rejectedAt: String? = null,
        val rejectedBy: String? = null,
        val rejectionReason: String? = null,
        val actualStartTime: String? = null,
        val actualEndTime: String? = null,
        val totalCost: Double? = null,
        val energyConsumedKWh: Double? = null,
        val user: BookingUserResponse? = null,
        val chargingStation: BookingChargingStationResponse? = null,
        val isWithinBookingWindow: Boolean = false,
        val canBeModified: Boolean = false,
        val canBeCancelled: Boolean = false,
        val isActive: Boolean = false,
        val durationMinutes: Int = 0
    )

    data class BookingUserResponse(
        val id: String = "",
        val nic: String = "",
        val fullName: String = "",
        val email: String = "",
        val phoneNumber: String? = null,
        val address: String? = null,
        val isActive: Boolean = false,
        val isApproved: Boolean = false
    )

    data class BookingChargingStationResponse(
        val id: String = "",
        val stationName: String = "",
        val location: String = "",
        val address: String = "",
        val connectorType: String = "",
        val powerRatingKW: Double? = null,
        val pricePerKWh: Double? = null,
        val status: String = "",
        val description: String? = null,
        val amenities: List<String>? = null,
        val operatingHours: String? = null,
        val isAvailable: Boolean = false,
        val maxBookingDurationMinutes: Int = 0,
        val coordinates: CoordinatesResponse? = null
    )

    data class CoordinatesResponse(
        val latitude: Double? = null,
        val longitude: Double? = null
    )

    data class ChargingStationResponse(
        val id: String? = null,
        val stationName: String = "",
        val location: String = "",
        val address: String = "",
        val latitude: Double? = null,
        val longitude: Double? = null,
        val connectorType: String = "",
        val powerRatingKW: Double? = null,
        val pricePerKWh: Double? = null,
        val status: String = "",
        val operatorId: String? = null,
        val description: String? = null,
        val amenities: List<String>? = null,
        val operatingHours: String? = null,
        val isAvailable: Boolean = false,
        val totalSlots: Int = 0,
        val maxBookingDurationMinutes: Int = 0,
        val availableSlots: Int = 0,
        val createdAt: String? = null,
        val updatedAt: String? = null,
        val lastMaintenanceDate: String? = null,
        val nextMaintenanceDate: String? = null
    )

    // endregion

    // region Station Operator Operations

    /**
     * Get bookings for a specific charging station
     */
    suspend fun getStationBookings(
        stationId: String,
        status: String? = null,
        startDate: String? = null,
        endDate: String? = null
    ): Result<List<BookingResponse>> {
        return withContext(Dispatchers.IO) {
            try {
                var url = "${BASE_URL}Bookings/station/$stationId?"
                if (status != null) url += "status=$status&"
                if (startDate != null) url += "startDate=$startDate&"
                if (endDate != null) url += "endDate=$endDate&"
                
                val request = Request.Builder()
                    .url(url)
                    .get()
                    .build()

                client.newCall(request).execute().use { response ->
                    val responseBody = response.body?.string() ?: ""

                    if (response.isSuccessful) {
                        val bookings = gson.fromJson(
                            responseBody,
                            Array<BookingResponse>::class.java
                        ).toList()
                        Result.success(bookings)
                    } else {
                        Log.e(TAG, "Get station bookings failed: ${response.code} - $responseBody")
                        Result.failure(Exception("Failed to get bookings: ${response.message}"))
                    }
                }
            } catch (e: Exception) {
                Log.e(TAG, "Error getting station bookings", e)
                Result.failure(e)
            }
        }
    }

    /**
     * Verify booking from QR code scan
     */
    suspend fun verifyBooking(bookingId: String, operatorStationId: String?): Result<BookingResponse> {
        return withContext(Dispatchers.IO) {
            try {
                // Build URL with optional operatorStationId query parameter
                var url = buildUrl("Bookings/$bookingId/verify")
                if (!operatorStationId.isNullOrEmpty()) {
                    url = "$url?operatorStationId=$operatorStationId"
                }
                
                val request = Request.Builder()
                    .url(url)
                    .get()
                    .build()

                client.newCall(request).execute().use { response ->
                    val body = response.body?.string()
                    Log.d(TAG, "Verify Booking Response (Status: ${response.code}): $body")

                    if (response.isSuccessful && body != null) {
                        val booking = gson.fromJson(body, BookingResponse::class.java)
                        Result.success(booking)
                    } else if (response.code == 403) {
                        // Security violation - wrong station
                        Result.failure(Exception(body ?: "This booking belongs to a different charging station"))
                    } else {
                        Result.failure(Exception("Failed to verify booking: ${response.code} - $body"))
                    }
                }
            } catch (e: Exception) {
                Log.e(TAG, "Error verifying booking", e)
                Result.failure(e)
            }
        }
    }

    /**
     * Complete a charging session
     */
    suspend fun completeBooking(bookingId: String, energyConsumed: Double, notes: String): Result<BookingResponse> {
        return withContext(Dispatchers.IO) {
            try {
                val url = buildUrl("Bookings/$bookingId/complete")
                val requestBody = gson.toJson(mapOf(
                    "energyConsumedKWh" to energyConsumed,
                    "notes" to notes
                ))

                val request = Request.Builder()
                    .url(url)
                    .post(requestBody.toRequestBody("application/json".toMediaType()))
                    .build()

                client.newCall(request).execute().use { response ->
                    val body = response.body?.string()
                    Log.d(TAG, "Complete Booking Response: $body")

                    if (response.isSuccessful && body != null) {
                        val booking = gson.fromJson(body, BookingResponse::class.java)
                        Result.success(booking)
                    } else {
                        Result.failure(Exception("Failed to complete booking: ${response.code}"))
                    }
                }
            } catch (e: Exception) {
                Log.e(TAG, "Error completing booking", e)
                Result.failure(e)
            }
        }
    }

    // endregion
}







