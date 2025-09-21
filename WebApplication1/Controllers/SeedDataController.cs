/*
 * File: SeedDataController.cs
 * Description: Controller to seed test data for development
 * Author: [Your Team Name]
 * Date: [Current Date]
 */

using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Services;
using MongoDB.Driver;
using BCrypt.Net;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeedDataController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;

        /// <summary>
        /// Initialize SeedData controller with MongoDB service
        /// </summary>
        public SeedDataController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        /// <summary>
        /// Create test web users for development
        /// </summary>
        [HttpPost("create-web-users")]
        public async Task<ActionResult> CreateTestWebUsers()
        {
            try
            {
                var testWebUsers = new List<WebUser>
                {
                    new WebUser
                    {
                        Email = "admin@evcharging.com",
                        FullName = "System Administrator",
                        Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                        Role = WebUserRole.Backoffice,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System"
                    },
                    new WebUser
                    {
                        Email = "operator@evcharging.com",
                        FullName = "Station Operator",
                        Password = BCrypt.Net.BCrypt.HashPassword("operator123"),
                        Role = WebUserRole.StationOperator,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System"
                    },
                    new WebUser
                    {
                        Email = "operator2@evcharging.com",
                        FullName = "Station Operator 2",
                        Password = BCrypt.Net.BCrypt.HashPassword("operator123"),
                        Role = WebUserRole.StationOperator,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System"
                    }
                };

                var createdUsers = new List<object>();

                foreach (var user in testWebUsers)
                {
                    // Check if user already exists
                    var filter = Builders<WebUser>.Filter.Eq(u => u.Email, user.Email);
                    var existingUser = await _mongoDBService.WebUsers
                        .Find(filter)
                        .FirstOrDefaultAsync();

                    if (existingUser == null)
                    {
                        await _mongoDBService.WebUsers.InsertOneAsync(user);
                        createdUsers.Add(new
                        {
                            user.Email,
                            user.FullName,
                            Role = user.Role.ToString(),
                            user.IsActive,
                            user.CreatedAt
                        });
                    }
                }

                return Ok(new
                {
                    message = "Test web users created successfully",
                    createdCount = createdUsers.Count,
                    users = createdUsers,
                    credentials = new
                    {
                        backoffice = new { email = "admin@evcharging.com", password = "admin123" },
                        stationOperator = new { email = "operator@evcharging.com", password = "operator123" }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to create test web users",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Create test EV owners for development
        /// </summary>
        [HttpPost("create-ev-owners")]
        public async Task<ActionResult> CreateTestEVOwners()
        {
            try
            {
                var testEVOwners = new List<User>
                {
                    new User
                    {
                        NIC = "200012345678",
                        FullName = "John Doe",
                        Email = "john.doe@example.com",
                        Password = BCrypt.Net.BCrypt.HashPassword("user123"),
                        PhoneNumber = "+94771234567",
                        Address = "123 Main St, Colombo",
                        IsActive = true,
                        IsApproved = true,
                        RegisteredAt = DateTime.UtcNow,
                        ApprovedAt = DateTime.UtcNow,
                        ApprovedBy = "System"
                    },
                    new User
                    {
                        NIC = "199512345679",
                        FullName = "Jane Smith",
                        Email = "jane.smith@example.com",
                        Password = BCrypt.Net.BCrypt.HashPassword("user123"),
                        PhoneNumber = "+94779876543",
                        Address = "456 Oak Ave, Kandy",
                        IsActive = true,
                        IsApproved = false,
                        RegisteredAt = DateTime.UtcNow
                    },
                    new User
                    {
                        NIC = "198812345680",
                        FullName = "Bob Wilson",
                        Email = "bob.wilson@example.com",
                        Password = BCrypt.Net.BCrypt.HashPassword("user123"),
                        PhoneNumber = "+94765432109",
                        Address = "789 Pine Rd, Galle",
                        IsActive = true,
                        IsApproved = true,
                        RegisteredAt = DateTime.UtcNow.AddDays(-5),
                        ApprovedAt = DateTime.UtcNow.AddDays(-4),
                        ApprovedBy = "admin@evcharging.com"
                    }
                };

                var createdOwners = new List<object>();

                foreach (var owner in testEVOwners)
                {
                    // Check if owner already exists
                    var filter = Builders<User>.Filter.Eq(u => u.NIC, owner.NIC);
                    var existingOwner = await _mongoDBService.EVOwners
                        .Find(filter)
                        .FirstOrDefaultAsync();

                    if (existingOwner == null)
                    {
                        await _mongoDBService.EVOwners.InsertOneAsync(owner);
                        createdOwners.Add(new
                        {
                            owner.NIC,
                            owner.FullName,
                            owner.Email,
                            owner.IsActive,
                            owner.IsApproved,
                            owner.RegisteredAt
                        });
                    }
                }

                return Ok(new
                {
                    message = "Test EV owners created successfully",
                    createdCount = createdOwners.Count,
                    owners = createdOwners
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to create test EV owners",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Clear all test data (use with caution)
        /// </summary>
        [HttpDelete("clear-all-test-data")]
        public async Task<ActionResult> ClearAllTestData()
        {
            try
            {
                // Clear test web users
                var testEmails = new[] { "admin@evcharging.com", "operator@evcharging.com", "operator2@evcharging.com" };
                var webUserFilter = Builders<WebUser>.Filter.In("email", testEmails);
                var webUserResult = await _mongoDBService.WebUsers.DeleteManyAsync(webUserFilter);

                // Clear test EV owners
                var testNICs = new[] { "200012345678", "199512345679", "198812345680" };
                var evOwnerFilter = Builders<User>.Filter.In("nic", testNICs);
                var evOwnerResult = await _mongoDBService.EVOwners.DeleteManyAsync(evOwnerFilter);

                return Ok(new
                {
                    message = "Test data cleared successfully",
                    deletedWebUsers = webUserResult.DeletedCount,
                    deletedEVOwners = evOwnerResult.DeletedCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to clear test data",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Test station operator login for mobile app
        /// </summary>
        [HttpPost("test-station-operator-login")]
        public async Task<ActionResult> TestStationOperatorLogin([FromQuery] string email = "operator@evcharging.com", [FromQuery] string password = "operator123")
        {
            var webUsersController = new WebUsersController(_mongoDBService);

            var loginRequest = new WebUserLoginRequest
            {
                Email = email,
                Password = password
            };

            return await webUsersController.Login(loginRequest);
        }

        /// <summary>
        /// Get station operator test credentials
        /// </summary>
        [HttpGet("station-operator-credentials")]
        public ActionResult GetStationOperatorCredentials()
        {
            return Ok(new
            {
                Title = "Station Operator Test Credentials",
                Description = "Use these credentials to test station operator login in the mobile app",
                Credentials = new[]
                {
                    new { Email = "operator@evcharging.com", Password = "operator123", Name = "Station Operator" },
                    new { Email = "operator2@evcharging.com", Password = "operator123", Name = "Station Operator 2" },
                    new { Email = "admin@evcharging.com", Password = "admin123", Name = "System Administrator" }
                },
                Instructions = new[]
                {
                    "1. Run POST /api/SeedData/create-web-users to create test station operators",
                    "2. Use the mobile app to login with any of the above credentials",
                    "3. The app will automatically detect if you're an EV owner or station operator",
                    "4. Station operators can access both web app and mobile app"
                },
                Note = "Station operators authenticate via /api/WebUsers/login endpoint"
            });
        }
    }
}