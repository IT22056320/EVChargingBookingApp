/*
 * File: ChargingStationsController.cs
 * Description: API Controller for Charging Stations management
 * Author: EV Charging Team
 * Date: September 30, 2025
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
        private readonly ChargingStationService _stationService;
        private readonly BookingService _bookingService;

        // Constructor for ChargingStationsController
        public ChargingStationsController(
            ChargingStationService stationService,
            BookingService bookingService)
        {
            _stationService = stationService;
            _bookingService = bookingService;
        }

        // Gets all charging stations
        [HttpGet]
        public async Task<IActionResult> GetStations()
        {
            var stations = await _stationService.GetAllAsync();
            return Ok(stations);
        }

        // Gets a charging station by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStation(string id)
        {
            var station = await _stationService.GetByIdAsync(id);
            if (station == null) return NotFound();
            return Ok(station);
        }

        // Creates a new charging station
        [HttpPost]
        public async Task<IActionResult> CreateStation([FromBody] ChargingStationDto dto)
        {
            var result = await _stationService.CreateAsync(dto);
            if (!result.Success) return BadRequest(result.Message);
            return CreatedAtAction(nameof(GetStation), new { id = result.Station!.Id }, result.Station);
        }

        // Updates a charging station
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStation(string id, [FromBody] ChargingStationDto dto)
        {
            // Check if the update is a deactivation
            if (dto.IsAvailable == false)
            {
                // Query for active bookings at this station
                var hasActiveBookings = await _bookingService.HasActiveBookingsAsync(id);
                if (hasActiveBookings)
                {
                    return BadRequest("Cannot deactivate station: active bookings exist.");
                }
            }
            var result = await _stationService.UpdateAsync(id, dto);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(new { message = result.Message });
        }

        // Deletes (deactivates) a charging station
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStation(string id)
        {
            var result = await _stationService.DeleteAsync(id);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(new { message = result.Message });
        }
    }

}