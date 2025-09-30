using MongoDB.Driver;
using WebApplication1.Models;
using WebApplication1.DTOs;

namespace WebApplication1.Services
{
    public class ChargingStationService
    {
        private readonly MongoDBService _mongoDBService;
        private readonly ILogger<ChargingStationService> _logger;

        public ChargingStationService(MongoDBService mongoDBService, ILogger<ChargingStationService> logger)
        {
            _mongoDBService = mongoDBService;
            _logger = logger;
        }

        public async Task<List<ChargingStationDto>> GetAllAsync()
        {
            var stations = await _mongoDBService.ChargingStations.Find(_ => true).ToListAsync();
            return stations.Select(MapToDto).ToList();
        }

        public async Task<ChargingStationDto?> GetByIdAsync(string id)
        {
            var station = await _mongoDBService.ChargingStations
                .Find(cs => cs.Id == id)
                .FirstOrDefaultAsync();

            return station == null ? null : MapToDto(station);
        }

        public async Task<(bool Success, string Message, ChargingStationDto? Station)> CreateAsync(ChargingStationDto dto)
        {
            try
            {
                var model = MapToModel(dto);
                model.CreatedAt = DateTime.UtcNow;
                await _mongoDBService.ChargingStations.InsertOneAsync(model);

                return (true, "Charging station created successfully", MapToDto(model));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating charging station");
                return (false, "Error creating charging station", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateAsync(string id, ChargingStationDto dto)
        {
            try
            {
                var existing = await _mongoDBService.ChargingStations.Find(cs => cs.Id == id).FirstOrDefaultAsync();
                if (existing == null)
                    return (false, "Charging station not found");

                // Business rule: prevent deactivation if active bookings exist
                if (dto.Status == ChargingStationStatus.Inactive.ToString() && existing.Bookings?.Any(b => b.IsActive) == true)
                {
                    return (false, "Cannot deactivate station with active bookings");
                }

                var updated = MapToModel(dto);
                updated.Id = id;
                updated.UpdatedAt = DateTime.UtcNow;

                var filter = Builders<ChargingStation>.Filter.Eq(cs => cs.Id, id);
                await _mongoDBService.ChargingStations.ReplaceOneAsync(filter, updated);

                return (true, "Charging station updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating charging station {Id}", id);
                return (false, "Error updating charging station");
            }
        }

        public async Task<(bool Success, string Message)> DeleteAsync(string id)
        {
            try
            {
                // Soft delete (set status to inactive)
                var filter = Builders<ChargingStation>.Filter.Eq(cs => cs.Id, id);
                var update = Builders<ChargingStation>.Update
                    .Set(cs => cs.Status, ChargingStationStatus.Inactive)
                    .Set(cs => cs.IsAvailable, false)
                    .Set(cs => cs.UpdatedAt, DateTime.UtcNow);

                var result = await _mongoDBService.ChargingStations.UpdateOneAsync(filter, update);

                if (result.MatchedCount == 0)
                    return (false, "Charging station not found");

                return (true, "Charging station deactivated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating charging station {Id}", id);
                return (false, "Error deactivating charging station");
            }
        }

        #region Private Mapping Methods

        private ChargingStationDto MapToDto(ChargingStation station)
        {
            return new ChargingStationDto
            {
                Id = station.Id,
                StationName = station.StationName,
                Location = station.Location,
                Address = station.Address,
                Latitude = station.Latitude,
                Longitude = station.Longitude,
                ConnectorType = station.ConnectorType.ToString(),
                PowerRatingKW = station.PowerRatingKW,
                PricePerKWh = station.PricePerKWh,
                Status = station.Status.ToString(),
                OperatorId = station.OperatorId,
                Description = station.Description,
                Amenities = station.Amenities,
                OperatingHours = station.OperatingHours,
                IsAvailable = station.IsAvailable,
                TotalSlots = station.TotalSlots, // Correct mapping
                MaxBookingDurationMinutes = station.MaxBookingDurationMinutes,
                AvailableSlots = station.AvailableSlots,
                CreatedAt = station.CreatedAt,
                UpdatedAt = station.UpdatedAt,
                LastMaintenanceDate = station.LastMaintenanceDate,
                NextMaintenanceDate = station.NextMaintenanceDate // Correct mapping
            };
        }

        private ChargingStation MapToModel(ChargingStationDto dto)
        {
            return new ChargingStation
            {
                Id = dto.Id,
                StationName = dto.StationName,
                Location = dto.Location,
                Address = dto.Address,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                ConnectorType = Enum.TryParse<ConnectorType>(dto.ConnectorType, out var connector)
                                ? connector : ConnectorType.Unknown,
                PowerRatingKW = dto.PowerRatingKW,
                PricePerKWh = dto.PricePerKWh,
                Status = Enum.TryParse<ChargingStationStatus>(dto.Status, out var status)
                                ? status : ChargingStationStatus.Active,
                OperatorId = dto.OperatorId,
                Description = dto.Description,
                Amenities = dto.Amenities,
                OperatingHours = dto.OperatingHours,
                IsAvailable = dto.IsAvailable,
                MaxBookingDurationMinutes = dto.MaxBookingDurationMinutes,
                TotalSlots = dto.TotalSlots,
                AvailableSlots = dto.TotalSlots,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                LastMaintenanceDate = dto.LastMaintenanceDate,
                NextMaintenanceDate = dto.NextMaintenanceDate
            };
        }

        #endregion
    }
}
