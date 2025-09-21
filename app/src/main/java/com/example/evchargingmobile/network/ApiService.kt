/*
 * File: ApiService.kt
 * Description: API service to communicate with C# backend (Kotlin version with coroutines)
 * Author: EV Charging Team
 * Date: September 21, 2025
 */
package com.example.evchargingmobile.network

import android.util.Log
import com.google.gson.Gson
import com.google.gson.GsonBuilder
import com.google.gson.JsonObject
import com.example.evchargingmobile.models.User
import com.example.evchargingmobile.models.Booking
import com.example.evchargingmobile.models.ChargingStation
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlinx.coroutines.withContext
import okhttp3.*
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.RequestBody.Companion.toRequestBody
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
        // API URLs for different environments
        private const val EMULATOR_URL = "https://10.0.2.2:7180/api/"
        private const val LOCALHOST_URL = "https://localhost:7180/api/"
        private const val HTTP_EMULATOR_URL = "http://10.0.2.2:5175/api/"

        private const val BASE_URL = EMULATOR_URL
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

    init {
        client = OkHttpClient.Builder()
            .connectTimeout(10, TimeUnit.SECONDS)
            .readTimeout(15, TimeUnit.SECONDS)
            .writeTimeout(15, TimeUnit.SECONDS)
            .retryOnConnectionFailure(true)
            .hostnameVerifier { _, _ -> true } // Trust all hostnames for development
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
                Address = user.address ?: ""
            )

            val json = gson.toJson(request)
            val body = json.toRequestBody("application/json".toMediaType())

            val httpRequest = Request.Builder()
                .url("${BASE_URL}EVOwners/register")
                .post(body)
                .addHeader("Content-Type", "application/json")
                .build()

            val response = client.newCall(httpRequest).await()
            val responseBody = response.body?.string() ?: ""

            if (response.isSuccessful) {
                val registerResponse = gson.fromJson(responseBody, RegisterResponse::class.java)
                Result.success(registerResponse)
            } else {
                Log.e(TAG, "Registration failed: ${response.code} - $responseBody")
                Result.failure(Exception("Registration failed: ${response.message}"))
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error in registerEVOwner", e)
            Result.failure(e)
        }
    }

    /**
     * Login EV owner with email-based authentication (Kotlin coroutines version)
     */
    suspend fun loginEVOwner(email: String, password: String): Result<LoginResponse> = withContext(Dispatchers.IO) {
        try {
            val request = LoginRequest(Email = email, Password = password)
            val json = gson.toJson(request)
            val body = json.toRequestBody("application/json".toMediaType())

            val httpRequest = Request.Builder()
                .url("${BASE_URL}EVOwners/login")
                .post(body)
                .addHeader("Content-Type", "application/json")
                .build()

            val response = client.newCall(httpRequest).await()
            val responseBody = response.body?.string() ?: ""

            if (response.isSuccessful) {
                // Parse response safely
                val jsonObj = gson.fromJson(responseBody, JsonObject::class.java)
                val loginResponse = LoginResponse(
                    message = jsonObj.get("message")?.asString ?: "Login successful",
                    user = null, // We'll handle user data separately to avoid parsing issues
                    token = null
                )
                Result.success(loginResponse)
            } else {
                // Parse error message
                val errorMessage = try {
                    val errorObj = gson.fromJson(responseBody, JsonObject::class.java)
                    errorObj.get("message")?.asString ?: responseBody
                } catch (e: Exception) {
                    responseBody.ifEmpty { "Login failed" }
                }
                Log.e(TAG, "Login failed: ${response.code} - $responseBody")
                Result.failure(Exception(errorMessage))
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error in loginEVOwner", e)
            Result.failure(e)
        }
    }

    /**
     * Login station operator with email-based authentication (Kotlin coroutines version)
     */
    suspend fun loginStationOperator(email: String, password: String): Result<LoginResponse> = withContext(Dispatchers.IO) {
        try {
            val request = WebUserLoginRequest(Email = email, Password = password)
            val json = gson.toJson(request)
            val body = json.toRequestBody("application/json".toMediaType())

            val httpRequest = Request.Builder()
                .url("${BASE_URL}WebUsers/login")
                .post(body)
                .addHeader("Content-Type", "application/json")
                .build()

            val response = client.newCall(httpRequest).await()
            val responseBody = response.body?.string() ?: ""

            if (response.isSuccessful) {
                // Parse response safely
                val jsonObj = gson.fromJson(responseBody, JsonObject::class.java)
                val loginResponse = LoginResponse(
                    message = jsonObj.get("message")?.asString ?: "Login successful",
                    user = null, // We'll handle user data separately to avoid parsing issues
                    token = null
                )
                Result.success(loginResponse)
            } else {
                // Parse error message
                val errorMessage = try {
                    val errorObj = gson.fromJson(responseBody, JsonObject::class.java)
                    errorObj.get("message")?.asString ?: responseBody
                } catch (e: Exception) {
                    responseBody.ifEmpty { "Login failed" }
                }
                Log.e(TAG, "Station operator login failed: ${response.code} - $responseBody")
                Result.failure(Exception(errorMessage))
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error in loginStationOperator", e)
            Result.failure(e)
        }
    }

    /**
     * Unified login method that tries both EV owner and station operator authentication
     */
    suspend fun loginUser(email: String, password: String): Result<UnifiedLoginResponse> = withContext(Dispatchers.IO) {
        try {
            // First try EV owner login
            val evOwnerResult = loginEVOwner(email, password)

            if (evOwnerResult.isSuccess) {
                return@withContext Result.success(UnifiedLoginResponse(
                    message = "Login successful",
                    userType = "EV_OWNER",
                    user = null,
                    token = null
                ))
            }

            // If EV owner login fails, try station operator login
            val stationOperatorResult = loginStationOperator(email, password)

            if (stationOperatorResult.isSuccess) {
                return@withContext Result.success(UnifiedLoginResponse(
                    message = "Login successful",
                    userType = "STATION_OPERATOR",
                    user = null,
                    token = null
                ))
            }

            // Both failed - return the last error
            Result.failure(Exception("Invalid email or password"))

        } catch (e: Exception) {
            Log.e(TAG, "Error in loginUser", e)
            Result.failure(e)
        }
    }

    /**
     * Get EV owner by NIC
     */
    suspend fun getEVOwnerByNIC(nic: String): Result<User> = withContext(Dispatchers.IO) {
        try {
            val request = Request.Builder()
                .url("${BASE_URL}EVOwners/$nic")
                .get()
                .build()

            val response = client.newCall(request).await()
            val responseBody = response.body?.string() ?: ""

            if (response.isSuccessful) {
                val user = gson.fromJson(responseBody, User::class.java)
                Result.success(user)
            } else if (response.code == 404) {
                Result.failure(Exception("User not found"))
            } else {
                Log.e(TAG, "Get user failed: ${response.code} - $responseBody")
                Result.failure(Exception("Failed to get user: ${response.message}"))
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error in getEVOwnerByNIC", e)
            Result.failure(e)
        }
    }

    /**
     * Extension function to convert OkHttp Call to suspending function
     */
    private suspend fun Call.await(): Response = suspendCancellableCoroutine { continuation ->
        enqueue(object : Callback {
            override fun onResponse(call: Call, response: Response) {
                continuation.resume(response)
            }

            override fun onFailure(call: Call, e: IOException) {
                continuation.resumeWithException(e)
            }
        })

        continuation.invokeOnCancellation {
            cancel()
        }
    }

    /**
     * Create SSL context that trusts all certificates (for development only)
     */
    private fun createTrustAllSSLContext(): SSLContext {
        return SSLContext.getInstance("TLS").apply {
            init(null, arrayOf(createTrustAllManager()), java.security.SecureRandom())
        }
    }

    /**
     * Create trust manager that trusts all certificates (for development only)
     */
    private fun createTrustAllManager(): X509TrustManager {
        return object : X509TrustManager {
            override fun checkClientTrusted(chain: Array<X509Certificate>, authType: String) {}
            override fun checkServerTrusted(chain: Array<X509Certificate>, authType: String) {}
            override fun getAcceptedIssuers(): Array<X509Certificate> = arrayOf()
        }
    }

    // Data classes for API requests/responses
    data class RegisterRequest(
        val NIC: String,
        val FullName: String,
        val Email: String,
        val Password: String,
        val PhoneNumber: String,
        val Address: String
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
}
