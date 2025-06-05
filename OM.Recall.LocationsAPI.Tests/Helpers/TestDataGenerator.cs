using Bogus;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using OM.Recall.LocationsAPI.Models;
using OM.Recall.LocationsAPI.Models.DTOs;

namespace OM.Recall.LocationsAPI.Tests.Helpers
{
    public static class TestDataGenerator
    {
        public static Faker<Location> LocationFaker => new Faker<Location>()
            .RuleFor(l => l.Id, f => f.Random.Int(1, 1000))
            .RuleFor(l => l.Identifier, f => f.Random.AlphaNumeric(3))
            .RuleFor(l => l.Description, f => f.Address.City())
            .RuleFor(l => l.SystemTypeName, f => "CSW")
            .RuleFor(l => l.CreatedDate, f => f.Date.Past())
            .RuleFor(l => l.UpdatedDate, f => f.Random.Bool() ? f.Date.Recent() : null);

        public static Faker<LocationDto> LocationDtoFaker => new Faker<LocationDto>()
            .RuleFor(l => l.Identifier, f => f.Random.AlphaNumeric(3))
            .RuleFor(l => l.Description, f => f.Address.City())
            .RuleFor(l => l.SystemTypeName, f => "CSW");

        public static List<Location> GenerateLocations(int count = 5)
        {
            return LocationFaker.Generate(count);
        }

        public static List<LocationDto> GenerateLocationDtos(int count = 5)
        {
            return LocationDtoFaker.Generate(count);
        }

        public static Location GenerateLocation()
        {
            return LocationFaker.Generate();
        }

        public static LocationDto GenerateLocationDto()
        {
            return LocationDtoFaker.Generate();
        }

        public static List<LocationDto> GetSampleJsonData()
        {
            return new List<LocationDto>
            {
                new LocationDto { Description = "St Louis", Identifier = "14", SystemTypeName = "CSW" },
                new LocationDto { Description = "Seattle", Identifier = "92", SystemTypeName = "CSW" },
                new LocationDto { Description = "Charlotte", Identifier = "20", SystemTypeName = "CSW" },
                new LocationDto { Description = "Portland", Identifier = "82", SystemTypeName = "CSW" },
                new LocationDto { Description = "Cleveland (UHHS)", Identifier = "58", SystemTypeName = "CSW" }
            };
        }
    }
}
