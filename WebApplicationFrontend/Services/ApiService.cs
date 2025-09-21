/*
 * File: ApiService.cs
 * Description: Service to handle API communication for web application
 * Author: [Your Team Name]
 * Date: [Current Date]
 */

using System.Text;
using System.Text.Json;

namespace WebApplicationFrontend.Services
{
    /// <summary>
    /// Service to handle API communication with the backend
    /// </summary>
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7180/api";
        }

        /// <summary>
        /// Authenticate web user login (Email/Password)
        /// </summary>
        public async Task<ApiResponse<WebUserLoginResponse>> LoginAsync(string email, string password)
        {
            try
            {
                var loginRequest = new
                {
                    Email = email,
                    Password = password
                };

                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/WebUsers/login", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = JsonSerializer.Deserialize<ApiResponse<WebUserLoginResponse>>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return loginResponse ?? new ApiResponse<WebUserLoginResponse> { Success = false, Message = "Invalid response" };
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<ApiResponse<object>>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return new ApiResponse<WebUserLoginResponse>
                    {
                        Success = false,
                        Message = errorResponse?.Message ?? "Login failed"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<WebUserLoginResponse>
                {
                    Success = false,
                    Message = $"Connection error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get all web users (for admin purposes)
        /// </summary>
        public async Task<List<WebUserDto>> GetWebUsersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/WebUsers");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<List<WebUserDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return users ?? new List<WebUserDto>();
                }
                return new List<WebUserDto>();
            }
            catch
            {
                return new List<WebUserDto>();
            }
        }

        /// <summary>
        /// Get all EV owners
        /// </summary>
        public async Task<List<EVOwnerDto>> GetEVOwnersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/EVOwners");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var owners = JsonSerializer.Deserialize<List<EVOwnerDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return owners ?? new List<EVOwnerDto>();
                }
                return new List<EVOwnerDto>();
            }
            catch
            {
                return new List<EVOwnerDto>();
            }
        }

        /// <summary>
        /// Get pending EV owner approvals
        /// </summary>
        public async Task<List<EVOwnerDto>> GetPendingApprovalsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/EVOwners/pending-approvals");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var owners = JsonSerializer.Deserialize<List<EVOwnerDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return owners ?? new List<EVOwnerDto>();
                }
                return new List<EVOwnerDto>();
            }
            catch
            {
                return new List<EVOwnerDto>();
            }
        }

        /// <summary>
        /// Approve or reject EV owner
        /// </summary>
        public async Task<bool> UpdateEVOwnerApprovalAsync(string nic, bool isApproved, string approvedBy)
        {
            try
            {
                var request = new { IsApproved = isApproved, ApprovedBy = approvedBy };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync($"{_baseUrl}/EVOwners/{nic}/approval", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get EV owner by NIC
        /// </summary>
        public async Task<EVOwnerDto?> GetEVOwnerByNICAsync(string nic)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/EVOwners/{nic}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var owner = JsonSerializer.Deserialize<EVOwnerDto>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return owner;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Update EV owner account status
        /// </summary>
        public async Task<bool> UpdateEVOwnerStatusAsync(string nic, bool isActive)
        {
            try
            {
                var request = new { IsActive = isActive };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync($"{_baseUrl}/EVOwners/{nic}/status", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// API response wrapper
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public WebUserLoginResponse? User { get; set; }
    }

    /// <summary>
    /// Web user login response model
    /// </summary>
    public class WebUserLoginResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    /// <summary>
    /// Web user DTO
    /// </summary>
    public class WebUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// EV Owner DTO
    /// </summary>
    public class EVOwnerDto
    {
        public string Id { get; set; } = string.Empty;
        public string NIC { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsApproved { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string ApprovedBy { get; set; } = string.Empty;
        public DateTime? LastLoginAt { get; set; }
    }
}