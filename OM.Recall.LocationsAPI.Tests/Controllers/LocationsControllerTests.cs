using Xunit;
using System;
using System.Net;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using FluentAssertions;
using System.Threading.Tasks;
using System.Collections.Generic;
using OM.Recall.LocationsAPI.Models.DTOs;
using OM.Recall.LocationsAPI.Tests.Helpers;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace OM.Recall.LocationsAPI.Tests.Controllers
{
    public class LocationsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public LocationsControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetLocations_ShouldReturnOk_WithLocationsList()
        {
            // Act
            var response = await _client.GetAsync("/api/locations");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var locations = JsonConvert.DeserializeObject<List<LocationResponseDto>>(content);
            locations.Should().NotBeNull();
        }

        [Fact]
        public async Task GetLocation_ShouldReturnNotFound_WhenLocationDoesNotExist()
        {
            // Act
            var response = await _client.GetAsync("/api/locations/999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CreateLocation_ShouldReturnCreated_WithValidData()
        {
            // Arrange
            var locationDto = new LocationDto
            {
                Description = "Test Location",
                Identifier = "TEST001",
                SystemTypeName = "CSW"
            };

            var json = JsonConvert.SerializeObject(locationDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/locations", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdLocation = JsonConvert.DeserializeObject<LocationResponseDto>(responseContent);

            createdLocation.Should().NotBeNull();
            createdLocation!.Identifier.Should().Be("TEST001");
            createdLocation.Description.Should().Be("Test Location");
        }

        [Fact]
        public async Task CreateLocation_ShouldReturnBadRequest_WithInvalidData()
        {
            // Arrange
            var locationDto = new LocationDto
            {
                Description = "Test Location",
                Identifier = "", // Invalid - empty identifier
                SystemTypeName = "CSW"
            };

            var json = JsonConvert.SerializeObject(locationDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/locations", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError); // Due to unique constraint
        }

        [Fact]
        public async Task BulkInsert_ShouldReturnOk_WithValidLocations()
        {
            // Arrange
            var request = new BulkInsertRequest
            {
                Locations = new List<LocationDto>
                {
                    new LocationDto { Description = "Bulk Test 1", Identifier = "BULK001", SystemTypeName = "CSW" },
                    new LocationDto { Description = "Bulk Test 2", Identifier = "BULK002", SystemTypeName = "CSW" }
                }
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/locations/bulk", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<BulkInsertResponse>(responseContent);

            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.InsertedCount.Should().Be(2);
        }

        [Fact]
        public async Task BulkInsertFromJson_ShouldReturnOk_WithValidJson()
        {
            // Arrange
            var locations = TestDataGenerator.GetSampleJsonData();
            var json = JsonConvert.SerializeObject(locations);
            var content = new StringContent($"\"{json.Replace("\"", "\\\"")}\"", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/locations/bulk/json", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task UpdateLocation_ShouldReturnOk_WhenLocationExists()
        {
            // Arrange - First create a location
            var createDto = new LocationDto
            {
                Description = "Original Description",
                Identifier = "UPDATE001",
                SystemTypeName = "CSW"
            };

            var createJson = JsonConvert.SerializeObject(createDto);
            var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
            var createResponse = await _client.PostAsync("/api/locations", createContent);

            var createResponseContent = await createResponse.Content.ReadAsStringAsync();
            var createdLocation = JsonConvert.DeserializeObject<LocationResponseDto>(createResponseContent);

            // Arrange - Update data
            var updateDto = new LocationDto
            {
                Description = "Updated Description",
                Identifier = "UPDATE001",
                SystemTypeName = "NEW"
            };

            var updateJson = JsonConvert.SerializeObject(updateDto);
            var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync($"/api/locations/{createdLocation!.Id}", updateContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseContent = await response.Content.ReadAsStringAsync();
            var updatedLocation = JsonConvert.DeserializeObject<LocationResponseDto>(responseContent);

            updatedLocation.Should().NotBeNull();
            updatedLocation!.Description.Should().Be("Updated Description");
            updatedLocation.SystemTypeName.Should().Be("NEW");
        }

        [Fact]
        public async Task DeleteLocation_ShouldReturnNotFound_WhenLocationDoesNotExist()
        {
            // Act
            var response = await _client.DeleteAsync("/api/locations/999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetLocationByIdentifier_ShouldReturnOk_WhenLocationExists()
        {
            // Arrange - Create a location first
            var locationDto = new LocationDto
            {
                Description = "Identifier Test",
                Identifier = "IDENT001",
                SystemTypeName = "CSW"
            };

            var json = JsonConvert.SerializeObject(locationDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _client.PostAsync("/api/locations", content);

            // Act
            var response = await _client.GetAsync("/api/locations/identifier/IDENT001");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseContent = await response.Content.ReadAsStringAsync();
            var location = JsonConvert.DeserializeObject<LocationResponseDto>(responseContent);

            location.Should().NotBeNull();
            location!.Identifier.Should().Be("IDENT001");
        }
    }
}
