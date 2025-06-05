using OM.Recall.LocationsAPI.Models.DTOs;

namespace OM.Recall.LocationsAPI.Services
{
    public interface ILocationService
    {
        Task<IEnumerable<LocationResponseDto>> GetAllLocationsAsync();
        Task<LocationResponseDto?> GetLocationByIdAsync(int id);
        Task<LocationResponseDto?> GetLocationByIdentifierAsync(string identifier);
        Task<LocationResponseDto> CreateLocationAsync(LocationDto locationDto);
        Task<LocationResponseDto?> UpdateLocationAsync(int id, LocationDto locationDto);
        Task<bool> DeleteLocationAsync(int id);
        Task<BulkInsertResponse> BulkInsertFromJsonAsync(string jsonContent);
        Task<BulkInsertResponse> BulkInsertAsync(List<LocationDto> locations);
    }
}
