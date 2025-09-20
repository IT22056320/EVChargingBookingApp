/*
 * File: SystemTestController.cs
 * Description: System testing endpoints for comprehensive validation
 * Author: [Your Team Name]
 * Date: [Current Date]
 */

using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;
using MongoDB.Driver;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SystemTestController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;

        /// <summary>
        /// Initialize SystemTest controller with MongoDB service
        /// </summary>
        public SystemTestController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        /// <summary>
        /// Get system health status
        /// </summary>
        [HttpGet("health")]
        public async Task<ActionResult> GetSystemHealth()
        {
            try
            {
                // Test database connectivity
                var webUsersCount = await _mongoDBService.WebUsers.CountDocumentsAsync(_ => true);
                var evOwnersCount = await _mongoDBService.EVOwners.CountDocumentsAsync(_ => true);

                var health = new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    database = new
                    {
                        connected = true,
                        collections = new
                        {
                            webUsers = webUsersCount,
                            evOwners = evOwnersCount
                        }
                    },
                    version = "1.0.0",
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
                };

                return Ok(health);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "unhealthy",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Test all major endpoints
        /// </summary>
        [HttpGet("endpoints-test")]
        public async Task<ActionResult> TestEndpoints()
        {
            var results = new List<object>();

            try
            {
                // Test WebUsers endpoints
                var webUsers = await _mongoDBService.WebUsers.Find(_ => true).ToListAsync();
                results.Add(new
                {
                    endpoint = "WebUsers",
                    status = "success",
                    count = webUsers.Count,
                    roles = webUsers.GroupBy(u => u.Role).Select(g => new { role = g.Key.ToString(), count = g.Count() })
                });

                // Test EVOwners endpoints
                var evOwners = await _mongoDBService.EVOwners.Find(_ => true).ToListAsync();
                results.Add(new
                {
                    endpoint = "EVOwners",
                    status = "success",
                    count = evOwners.Count,
                    approved = evOwners.Count(u => u.IsApproved),
                    pending = evOwners.Count(u => !u.IsApproved)
                });

                return Ok(new
                {
                    timestamp = DateTime.UtcNow,
                    totalTests = results.Count,
                    allPassed = true,
                    results = results
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    timestamp = DateTime.UtcNow,
                    error = ex.Message,
                    results = results
                });
            }
        }

        /// <summary>
        /// Setup complete test environment
        /// </summary>
        [HttpPost("setup-complete-environment")]
        public async Task<ActionResult> SetupCompleteEnvironment()
        {
            try
            {
                var setupResults = new List<object>();

                // Create web users if they don't exist
                var webUsersCreated = 0;
                var testWebUsers = new[]
                {
                    new { email = "admin@evcharging.com", fullName = "System Administrator", role = Models.WebUserRole.Backoffice },
                    new { email = "operator@evcharging.com", fullName = "Station Operator", role = Models.WebUserRole.StationOperator },
                    new { email = "operator2@evcharging.com", fullName = "Station Operator 2", role = Models.WebUserRole.StationOperator }
                };

                foreach (var userData in testWebUsers)
                {
                    var existing = await _mongoDBService.WebUsers
                        .Find(u => u.Email == userData.email)
                        .FirstOrDefaultAsync();

                    if (existing == null)
                    {
                        var newUser = new Models.WebUser
                        {
                            Email = userData.email,
                            FullName = userData.fullName,
                            Password = BCrypt.Net.BCrypt.HashPassword("password123"),
                            Role = userData.role,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = "SystemTest"
                        };

                        await _mongoDBService.WebUsers.InsertOneAsync(newUser);
                        webUsersCreated++;
                    }
                }

                setupResults.Add(new
                {
                    category = "WebUsers",
                    created = webUsersCreated,
                    total = testWebUsers.Length
                });

                // Create EV owners if they don't exist
                var evOwnersCreated = 0;
                var testEVOwners = new[]
                {
                    new { nic = "200012345678", fullName = "John Doe", email = "john.doe@example.com", approved = true },
                    new { nic = "199512345679", fullName = "Jane Smith", email = "jane.smith@example.com", approved = false },
                    new { nic = "198812345680", fullName = "Bob Wilson", email = "bob.wilson@example.com", approved = true }
                };

                foreach (var ownerData in testEVOwners)
                {
                    var existing = await _mongoDBService.EVOwners
                        .Find(u => u.NIC == ownerData.nic)
                        .FirstOrDefaultAsync();

                    if (existing == null)
                    {
                        var newOwner = new Models.User
                        {
                            NIC = ownerData.nic,
                            FullName = ownerData.fullName,
                            Email = ownerData.email,
                            Password = BCrypt.Net.BCrypt.HashPassword("user123"),
                            PhoneNumber = "+94771234567",
                            Address = "Test Address",
                            IsActive = true,
                            IsApproved = ownerData.approved,
                            RegisteredAt = DateTime.UtcNow,
                            ApprovedAt = ownerData.approved ? DateTime.UtcNow : null,
                            ApprovedBy = ownerData.approved ? "SystemTest" : string.Empty
                        };

                        await _mongoDBService.EVOwners.InsertOneAsync(newOwner);
                        evOwnersCreated++;
                    }
                }

                setupResults.Add(new
                {
                    category = "EVOwners",
                    created = evOwnersCreated,
                    total = testEVOwners.Length
                });

                return Ok(new
                {
                    message = "Complete test environment setup successfully",
                    timestamp = DateTime.UtcNow,
                    results = setupResults,
                    testCredentials = new
                    {
                        webUsers = new
                        {
                            backoffice = new { email = "admin@evcharging.com", password = "password123" },
                            stationOperator = new { email = "operator@evcharging.com", password = "password123" }
                        },
                        mobileUsers = new
                        {
                            evOwner = new { nic = "200012345678", password = "user123" }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to setup test environment",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}