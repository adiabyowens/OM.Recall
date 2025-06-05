using Microsoft.AspNetCore.Mvc;
using OM.Recall.LocationsAPI.Services;
using OM.Recall.LocationsAPI.Models.DTOs;

namespace OM.Recall.LocationsAPI.Controllers
{
    public class LocationsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(ILocationService locationService, ILogger<LocationsController> logger)
    {
        _locationService = locationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all locations
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<LocationResponseDto>), 200)]
    public async Task<ActionResult<IEnumerable<LocationResponseDto>>> GetLocations()
    {
        try
        {
            var locations = await _locationService.GetAllLocationsAsync();
            return Ok(locations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving locations");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get location by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LocationResponseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<LocationResponseDto>> GetLocation(int id)
    {
        try
        {
            var location = await _locationService.GetLocationByIdAsync(id);
            if (location == null)
                return NotFound($"Location with ID {id} not found");

            return Ok(location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving location with ID {LocationId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get location by identifier
    /// </summary>
    [HttpGet("identifier/{identifier}")]
    [ProducesResponseType(typeof(LocationResponseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<LocationResponseDto>> GetLocationByIdentifier(string identifier)
    {
        try
        {
            var location = await _locationService.GetLocationByIdentifierAsync(identifier);
            if (location == null)
                return NotFound($"Location with identifier '{identifier}' not found");

            return Ok(location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving location with identifier {Identifier}", identifier);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new location
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(LocationResponseDto), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<LocationResponseDto>> CreateLocation([FromBody] LocationDto locationDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var createdLocation = await _locationService.CreateLocationAsync(locationDto);
            return CreatedAtAction(nameof(GetLocation), new { id = createdLocation.Id }, createdLocation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating location");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update an existing location
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(LocationResponseDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<LocationResponseDto>> UpdateLocation(int id, [FromBody] LocationDto locationDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updatedLocation = await _locationService.UpdateLocationAsync(id, locationDto);
            if (updatedLocation == null)
                return NotFound($"Location with ID {id} not found");

            return Ok(updatedLocation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location with ID {LocationId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a location
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteLocation(int id)
    {
        try
        {
            var deleted = await _locationService.DeleteLocationAsync(id);
            if (!deleted)
                return NotFound($"Location with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting location with ID {LocationId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Bulk insert locations from JSON
    /// </summary>
    [HttpPost("bulk/json")]
    [ProducesResponseType(typeof(BulkInsertResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<BulkInsertResponse>> BulkInsertFromJson([FromBody] string jsonContent)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
                return BadRequest("JSON content cannot be empty");

            var result = await _locationService.BulkInsertFromJsonAsync(jsonContent);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk insert from JSON");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Bulk insert locations
    /// </summary>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(BulkInsertResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<BulkInsertResponse>> BulkInsert([FromBody] BulkInsertRequest request)
    {
        try
        {
            if (request?.Locations == null || !request.Locations.Any())
                return BadRequest("Locations list cannot be empty");

            var result = await _locationService.BulkInsertAsync(request.Locations);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk insert");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Upload JSON file and insert locations
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(BulkInsertResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<BulkInsertResponse>> UploadJsonFile(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only JSON files are allowed");

            using var reader = new StreamReader(file.OpenReadStream());
            var jsonContent = await reader.ReadToEndAsync();

            var result = await _locationService.BulkInsertFromJsonAsync(jsonContent);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing uploaded file");
            return StatusCode(500, "Internal server error");
        }
    }
}
