using Newtonsoft.Json;
using OM.Recall.LocationsAPI.Data;
using Microsoft.EntityFrameworkCore;
using OM.Recall.LocationsAPI.Models;
using OM.Recall.LocationsAPI.Models.DTOs;

namespace OM.Recall.LocationsAPI.Services
{
    public class LocationService : ILocationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LocationService> _logger;

        public LocationService(ApplicationDbContext context, ILogger<LocationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<LocationResponseDto>> GetAllLocationsAsync()
        {
            var locations = await _context.Locations
                .OrderBy(l => l.Description)
                .ToListAsync();

            return locations.Select(MapToResponseDto);
        }

        public async Task<LocationResponseDto?> GetLocationByIdAsync(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            return location != null ? MapToResponseDto(location) : null;
        }

        public async Task<LocationResponseDto?> GetLocationByIdentifierAsync(string identifier)
        {
            var location = await _context.Locations
                .FirstOrDefaultAsync(l => l.Identifier == identifier);
            return location != null ? MapToResponseDto(location) : null;
        }

        public async Task<LocationResponseDto> CreateLocationAsync(LocationDto locationDto)
        {
            var location = new Location
            {
                Description = locationDto.Description,
                Identifier = locationDto.Identifier,
                SystemTypeName = locationDto.SystemTypeName,
                CreatedDate = DateTime.UtcNow
            };

            _context.Locations.Add(location);
            await _context.SaveChangesAsync();

            return MapToResponseDto(location);
        }

        public async Task<LocationResponseDto?> UpdateLocationAsync(int id, LocationDto locationDto)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) return null;

            location.Description = locationDto.Description;
            location.SystemTypeName = locationDto.SystemTypeName;
            location.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToResponseDto(location);
        }

        public async Task<bool> DeleteLocationAsync(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) return false;

            _context.Locations.Remove(location);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<BulkInsertResponse> BulkInsertFromJsonAsync(string jsonContent)
        {
            try
            {
                var locationDtos = JsonConvert.DeserializeObject<List<LocationDto>>(jsonContent);
                if (locationDtos == null)
                {
                    return new BulkInsertResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Invalid JSON format" }
                    };
                }

                return await BulkInsertAsync(locationDtos);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing JSON content");
                return new BulkInsertResponse
                {
                    Success = false,
                    Errors = new List<string> { $"JSON parsing error: {ex.Message}" }
                };
            }
        }

        public async Task<BulkInsertResponse> BulkInsertAsync(List<LocationDto> locationDtos)
        {
            var response = new BulkInsertResponse();
            var errors = new List<string>();
            var insertedCount = 0;
            var duplicatesSkipped = 0;

            try
            {
                // Get existing identifiers to avoid duplicates
                var existingIdentifiers = await _context.Locations
                    .Select(l => l.Identifier)
                    .ToHashSetAsync();

                var locationsToInsert = new List<Location>();

                foreach (var dto in locationDtos)
                {
                    // Validate required fields
                    if (string.IsNullOrWhiteSpace(dto.Identifier))
                    {
                        errors.Add($"Missing identifier for location: {dto.Description}");
                        continue;
                    }

                    // Check for duplicates
                    if (existingIdentifiers.Contains(dto.Identifier))
                    {
                        duplicatesSkipped++;
                        _logger.LogInformation($"Skipping duplicate identifier: {dto.Identifier}");
                        continue;
                    }

                    var location = new Location
                    {
                        Description = dto.Description ?? string.Empty,
                        Identifier = dto.Identifier,
                        SystemTypeName = dto.SystemTypeName ?? "CSW",
                        CreatedDate = DateTime.UtcNow
                    };

                    locationsToInsert.Add(location);
                    existingIdentifiers.Add(dto.Identifier); // Prevent duplicates within the same batch
                }

                if (locationsToInsert.Any())
                {
                    await _context.Locations.AddRangeAsync(locationsToInsert);
                    await _context.SaveChangesAsync();
                    insertedCount = locationsToInsert.Count;
                }

                response.Success = true;
                response.InsertedCount = insertedCount;
                response.DuplicatesSkipped = duplicatesSkipped;
                response.Errors = errors;

                _logger.LogInformation($"Bulk insert completed: {insertedCount} inserted, {duplicatesSkipped} duplicates skipped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk insert operation");
                response.Success = false;
                response.Errors.Add($"Database error: {ex.Message}");
            }

            return response;
        }

        private static LocationResponseDto MapToResponseDto(Location location)
        {
            return new LocationResponseDto
            {
                Id = location.Id,
                Description = location.Description,
                Identifier = location.Identifier,
                SystemTypeName = location.SystemTypeName,
                CreatedDate = location.CreatedDate,
                UpdatedDate = location.UpdatedDate
            };
        }
    }
}