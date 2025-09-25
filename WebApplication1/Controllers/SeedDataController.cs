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
        /// Create test charging stations for development
        /// </summary>
        [HttpPost("create-charging-stations")]
        public async Task<ActionResult> CreateTestChargingStations()
        {
            try
            {
                var testChargingStations = new List<ChargingStation>
                {
                    new ChargingStation
                    {
                        StationName = "Downtown Charging Hub",
                        Location = "Colombo 03",
                        Address = "123 Main Street, Colombo 03",
                        Latitude = 6.9271,
                        Longitude = 79.8612,
                        ConnectorType = ConnectorType.CCS,
                        PowerRatingKW = 50,
                        PricePerKWh = 25.0m,
                        Status = ChargingStationStatus.Active,
                        OperatorId = "operator@evcharging.com",
                        Description = "Fast charging station in the heart of Colombo",
                        Amenities = new List<string> { "WiFi", "Restroom", "Café" },
                        OperatingHours = "24/7",
                        IsAvailable = true,
                        MaxBookingDurationMinutes = 240,
                        CreatedAt = DateTime.UtcNow
                    },
                    new ChargingStation
                    {
                        StationName = "Mall Parking Station",
                        Location = "Nugegoda",
                        Address = "456 High Level Road, Nugegoda",
                        Latitude = 6.8649,
                        Longitude = 79.8997,
                        ConnectorType = ConnectorType.Type2,
                        PowerRatingKW = 22,
                        PricePerKWh = 20.0m,
                        Status = ChargingStationStatus.Active,
                        OperatorId = "operator@evcharging.com",
                        Description = "Convenient charging while you shop",
                        Amenities = new List<string> { "Shopping Mall", "Food Court", "Parking" },
                        OperatingHours = "9:00 AM - 10:00 PM",
                        IsAvailable = true,
                        MaxBookingDurationMinutes = 180,
                        CreatedAt = DateTime.UtcNow
                    },
                    new ChargingStation
                    {
                        StationName = "Highway Rest Area",
                        Location = "Kaduwela",
                        Address = "789 Colombo-Kandy Road, Kaduwela",
                        Latitude = 6.9333,
                        Longitude = 79.9833,
                        ConnectorType = ConnectorType.CHAdeMO,
                        PowerRatingKW = 40,
                        PricePerKWh = 22.5m,
                        Status = ChargingStationStatus.Active,
                        OperatorId = "operator2@evcharging.com",
                        Description = "Perfect stop for long-distance travel",
                        Amenities = new List<string> { "Restaurant", "Restroom", "Convenience Store" },
                        OperatingHours = "24/7",
                        IsAvailable = true,
                        MaxBookingDurationMinutes = 120,
                        CreatedAt = DateTime.UtcNow
                    },
                    new ChargingStation
                    {
                        StationName = "Beach Side Charging",
                        Location = "Mount Lavinia",
                        Address = "321 Galle Road, Mount Lavinia",
                        Latitude = 6.8378,
                        Longitude = 79.8653,
                        ConnectorType = ConnectorType.Type1,
                        PowerRatingKW = 11,
                        PricePerKWh = 18.0m,
                        Status = ChargingStationStatus.Active,
                        OperatorId = "operator@evcharging.com",
                        Description = "Charge your vehicle while enjoying the beach",
                        Amenities = new List<string> { "Beach Access", "Restaurants", "Parking" },
                        OperatingHours = "6:00 AM - 11:00 PM",
                        IsAvailable = true,
                        MaxBookingDurationMinutes = 300,
                        CreatedAt = DateTime.UtcNow
                    },
                    new ChargingStation
                    {
                        StationName = "Airport Express Station",
                        Location = "Katunayake",
                        Address = "Airport Expressway, Katunayake",
                        Latitude = 7.1697,
                        Longitude = 79.8841,
                        ConnectorType = ConnectorType.Tesla,
                        PowerRatingKW = 75,
                        PricePerKWh = 30.0m,
                        Status = ChargingStationStatus.Active,
                        OperatorId = "operator2@evcharging.com",
                        Description = "High-speed charging near the airport",
                        Amenities = new List<string> { "Airport Shuttle", "WiFi", "Lounge" },
                        OperatingHours = "24/7",
                        IsAvailable = true,
                        MaxBookingDurationMinutes = 90,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                var createdStations = new List<object>();

                foreach (var station in testChargingStations)
                {
                    // Check if station already exists by name and location
                    var filter = Builders<ChargingStation>.Filter.And(
                        Builders<ChargingStation>.Filter.Eq(s => s.StationName, station.StationName),
                        Builders<ChargingStation>.Filter.Eq(s => s.Location, station.Location)
                    );
                    var existingStation = await _mongoDBService.ChargingStations
                        .Find(filter)
                        .FirstOrDefaultAsync();

                    if (existingStation == null)
                    {
                        await _mongoDBService.ChargingStations.InsertOneAsync(station);
                        createdStations.Add(new
                        {
                            Id = station.Id,
                            station.StationName,
                            station.Location,
                            station.Address,
                            ConnectorType = station.ConnectorType.ToString(),
                            station.PowerRatingKW,
                            station.PricePerKWh,
                            Status = station.Status.ToString()
                        });
                    }
                }

                return Ok(new
                {
                    message = "Test charging stations created successfully",
                    createdCount = createdStations.Count,
                    stations = createdStations
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to create test charging stations",
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

                // Clear test charging stations
                var testStationNames = new[] { "Downtown Charging Hub", "Mall Parking Station", "Highway Rest Area", "Beach Side Charging", "Airport Express Station" };
                var chargingStationFilter = Builders<ChargingStation>.Filter.In("stationName", testStationNames);
                var chargingStationResult = await _mongoDBService.ChargingStations.DeleteManyAsync(chargingStationFilter);

                return Ok(new
                {
                    message = "Test data cleared successfully",
                    deletedWebUsers = webUserResult.DeletedCount,
                    deletedEVOwners = evOwnerResult.DeletedCount,
                    deletedChargingStations = chargingStationResult.DeletedCount
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