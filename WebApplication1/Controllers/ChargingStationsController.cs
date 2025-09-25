/*
 * File: ChargingStationsController.cs
 * Description: API Controller for Charging Stations management
 * Author: [Your Team Name]
 * Date: [Current Date]
 */

using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using WebApplication1.Models;
using WebApplication1.Services;
using WebApplication1.DTOs;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChargingStationsController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;
        private readonly ILogger<ChargingStationsController> _logger;

        public ChargingStationsController(MongoDBService mongoDBService, ILogger<ChargingStationsController> logger)
        {
            _mongoDBService = mongoDBService;
            _logger = logger;
        }

        /// <summary>
        /// Get all active charging stations
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetChargingStations()
        {
            try
            {
                var filter = Builders<ChargingStation>.Filter.Eq(cs => cs.Status, ChargingStationStatus.Active);
                var chargingStations = await _mongoDBService.ChargingStations
                    .Find(filter)
                    .ToListAsync();

                var response = chargingStations.Select(cs => new ChargingStationResponseDto
                {
                    Id = cs.Id ?? string.Empty,
                    StationName = cs.StationName,
                    Location = cs.Location,
                    Address = cs.Address,
                    ConnectorType = cs.ConnectorType.ToString(),
                    PowerRatingKW = cs.PowerRatingKW,
                    PricePerKWh = cs.PricePerKWh,
                    Status = cs.Status.ToString(),
                    Description = cs.Description,
                    Amenities = cs.Amenities,
                    OperatingHours = cs.OperatingHours,
                    IsAvailable = cs.IsAvailable,
                    MaxBookingDurationMinutes = cs.MaxBookingDurationMinutes,
                    Coordinates = new CoordinatesDto
                    {
                        Latitude = cs.Latitude ?? 0,
                        Longitude = cs.Longitude ?? 0
                    }
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving charging stations");
                return StatusCode(500, new { message = "An error occurred while retrieving charging stations." });
            }
        }

        /// <summary>
        /// Get charging station by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetChargingStation(string id)
        {
            try
            {
                var chargingStation = await _mongoDBService.ChargingStations
                    .Find(cs => cs.Id == id)
                    .FirstOrDefaultAsync();

                if (chargingStation == null)
                {
                    return NotFound(new { message = "Charging station not found." });
                }

                var response = new ChargingStationResponseDto
                {
                    Id = chargingStation.Id ?? string.Empty,
                    StationName = chargingStation.StationName,
                    Location = chargingStation.Location,
                    Address = chargingStation.Address,
                    ConnectorType = chargingStation.ConnectorType.ToString(),
                    PowerRatingKW = chargingStation.PowerRatingKW,
                    PricePerKWh = chargingStation.PricePerKWh,
                    Status = chargingStation.Status.ToString(),
                    Description = chargingStation.Description,
                    Amenities = chargingStation.Amenities,
                    OperatingHours = chargingStation.OperatingHours,
                    IsAvailable = chargingStation.IsAvailable,
                    MaxBookingDurationMinutes = chargingStation.MaxBookingDurationMinutes,
                    Coordinates = new CoordinatesDto
                    {
                        Latitude = chargingStation.Latitude ?? 0,
                        Longitude = chargingStation.Longitude ?? 0
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving charging station with ID: {ChargingStationId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the charging station." });
            }
        }
    }
}