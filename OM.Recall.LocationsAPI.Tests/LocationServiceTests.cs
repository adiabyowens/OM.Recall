using Moq;
using Xunit;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using FluentAssertions;
using OM.Recall.LocationsAPI.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OM.Recall.LocationsAPI.Services;
using OM.Recall.LocationsAPI.Models.DTOs;
using OM.Recall.LocationsAPI.Tests.Helpers;
using OM.Recall.LocationsAPI.Tests.Fixtures;
using static System.Net.Mime.MediaTypeNames;

namespace OM.Recall.LocationsAPI.Tests
{
    public class LocationServiceTests : IClassFixture<DatabaseFixture>
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<LocationService>> _mockLogger;
        private readonly LocationService _service;

        public LocationServiceTests(DatabaseFixture fixture)
        {
            _context = fixture.Context;
            _mockLogger = new Mock<ILogger<LocationService>>();
            _service = new LocationService(_context, _mockLogger.Object);

            // Clean database before each test
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
        }

        [Fact]
        public async Task GetAllLocationsAsync_ShouldReturnEmptyList_WhenNoLocationsExist()
        {
            // Act
            var result = await _service.GetAllLocationsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllLocationsAsync_ShouldReturnAllLocations_WhenLocationsExist()
        {
            // Arrange
            var locations = TestDataGenerator.GenerateLocations(3);
            await _context.Locations.AddRangeAsync(locations);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllLocationsAsync();

            // Assert
            result.Should().HaveCount(3);
            result.Should().OnlyContain(l => !string.IsNullOrEmpty(l.Identifier));
        }

        [Fact]
        public async Task GetLocationByIdAsync_ShouldReturnLocation_WhenLocationExists()
        {
            // Arrange
            var location = TestDataGenerator.GenerateLocation();
            await _context.Locations.AddAsync(location);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetLocationByIdAsync(location.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(location.Id);
            result.Identifier.Should().Be(location.Identifier);
            result.Description.Should().Be(location.Description);
        }

        [Fact]
        public async Task GetLocationByIdAsync_ShouldReturnNull_WhenLocationDoesNotExist()
        {
            // Act
            var result = await _service.GetLocationByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetLocationByIdentifierAsync_ShouldReturnLocation_WhenLocationExists()
        {
            // Arrange
            var location = TestDataGenerator.GenerateLocation();
            location.Identifier = "TEST123";
            await _context.Locations.AddAsync(location);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetLocationByIdentifierAsync("TEST123");

            // Assert
            result.Should().NotBeNull();
            result!.Identifier.Should().Be("TEST123");
        }

        [Fact]
        public async Task CreateLocationAsync_ShouldCreateLocation_WithValidData()
        {
            // Arrange
            var locationDto = TestDataGenerator.GenerateLocationDto();

            // Act
            var result = await _service.CreateLocationAsync(locationDto);

            // Assert
            result.Should().NotBeNull();
            result.Identifier.Should().Be(locationDto.Identifier);
            result.Description.Should().Be(locationDto.Description);
            result.SystemTypeName.Should().Be(locationDto.SystemTypeName);
            result.Id.Should().BeGreaterThan(0);

            // Verify it was saved to database
            var savedLocation = await _context.Locations.FindAsync(result.Id);
            savedLocation.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateLocationAsync_ShouldUpdateLocation_WhenLocationExists()
        {
            // Arrange
            var location = TestDataGenerator.GenerateLocation();
            await _context.Locations.AddAsync(location);
            await _context.SaveChangesAsync();

            var updateDto = new LocationDto
            {
                Description = "Updated Description",
                Identifier = location.Identifier, // Can't update identifier
                SystemTypeName = "NEW"
            };

            // Act
            var result = await _service.UpdateLocationAsync(location.Id, updateDto);

            // Assert
            result.Should().NotBeNull();
            result!.Description.Should().Be("Updated Description");
            result.SystemTypeName.Should().Be("NEW");
            result.UpdatedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateLocationAsync_ShouldReturnNull_WhenLocationDoesNotExist()
        {
            // Arrange
            var updateDto = TestDataGenerator.GenerateLocationDto();

            // Act
            var result = await _service.UpdateLocationAsync(999, updateDto);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task DeleteLocationAsync_ShouldDeleteLocation_WhenLocationExists()
        {
            // Arrange
            var location = TestDataGenerator.GenerateLocation();
            await _context.Locations.AddAsync(location);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteLocationAsync(location.Id);

            // Assert
            result.Should().BeTrue();

            // Verify it was deleted from database
            var deletedLocation = await _context.Locations.FindAsync(location.Id);
            deletedLocation.Should().BeNull();
        }

        [Fact]
        public async Task DeleteLocationAsync_ShouldReturnFalse_WhenLocationDoesNotExist()
        {
            // Act
            var result = await _service.DeleteLocationAsync(999);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task BulkInsertAsync_ShouldInsertAllLocations_WhenAllAreValid()
        {
            // Arrange
            var locationDtos = TestDataGenerator.GenerateLocationDtos(3);

            // Act
            var result = await _service.BulkInsertAsync(locationDtos);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.InsertedCount.Should().Be(3);
            result.DuplicatesSkipped.Should().Be(0);
            result.Errors.Should().BeEmpty();

            // Verify in database
            var locationsInDb = await _context.Locations.CountAsync();
            locationsInDb.Should().Be(3);
        }

        [Fact]
        public async Task BulkInsertAsync_ShouldSkipDuplicates_WhenIdentifiersAlreadyExist()
        {
            // Arrange
            var existingLocation = TestDataGenerator.GenerateLocation();
            existingLocation.Identifier = "DUPLICATE";
            await _context.Locations.AddAsync(existingLocation);
            await _context.SaveChangesAsync();

            var locationDtos = new List<LocationDto>
            {
                new LocationDto { Identifier = "DUPLICATE", Description = "Duplicate", SystemTypeName = "CSW" },
                new LocationDto { Identifier = "NEW1", Description = "New 1", SystemTypeName = "CSW" },
                new LocationDto { Identifier = "NEW2", Description = "New 2", SystemTypeName = "CSW" }
            };

            // Act
            var result = await _service.BulkInsertAsync(locationDtos);

            // Assert
            result.Success.Should().BeTrue();
            result.InsertedCount.Should().Be(2);
            result.DuplicatesSkipped.Should().Be(1);

            // Verify in database
            var locationsInDb = await _context.Locations.CountAsync();
            locationsInDb.Should().Be(3); // 1 existing + 2 new
        }

        [Fact]
        public async Task BulkInsertAsync_ShouldReportErrors_WhenIdentifierIsMissing()
        {
            // Arrange
            var locationDtos = new List<LocationDto>
            {
                new LocationDto { Identifier = "", Description = "Missing Identifier", SystemTypeName = "CSW" },
                new LocationDto { Identifier = "VALID", Description = "Valid", SystemTypeName = "CSW" }
            };

            // Act
            var result = await _service.BulkInsertAsync(locationDtos);

            // Assert
            result.Success.Should().BeTrue();
            result.InsertedCount.Should().Be(1);
            result.Errors.Should().HaveCount(1);
            result.Errors.First().Should().Contain("Missing identifier");
        }

        [Fact]
        public async Task BulkInsertFromJsonAsync_ShouldParseAndInsertLocations_WithValidJson()
        {
            // Arrange
            var locationDtos = TestDataGenerator.GetSampleJsonData();
            var jsonContent = JsonConvert.SerializeObject(locationDtos);

            // Act
            var result = await _service.BulkInsertFromJsonAsync(jsonContent);

            // Assert
            result.Success.Should().BeTrue();
            result.InsertedCount.Should().Be(locationDtos.Count);
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task BulkInsertFromJsonAsync_ShouldReturnError_WithInvalidJson()
        {
            // Arrange
            var invalidJson = "{ invalid json }";

            // Act
            var result = await _service.BulkInsertFromJsonAsync(invalidJson);

            // Assert
            result.Success.Should().BeFalse();
            result.InsertedCount.Should().Be(0);
            result.Errors.Should().NotBeEmpty();
            result.Errors.First().Should().Contain("JSON parsing error");
        }
    }
}



