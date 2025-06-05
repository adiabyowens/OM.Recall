namespace OM.Recall.LocationsAPI.Models.DTOs
{
    public class LocationDto
    {
        public string Description { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public string SystemTypeName { get; set; } = string.Empty;
    }

    public class LocationResponseDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public string SystemTypeName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class BulkInsertRequest
    {
        public List<LocationDto> Locations { get; set; } = new List<LocationDto>();
    }

    public class BulkInsertResponse
    {
        public bool Success { get; set; }
        public int InsertedCount { get; set; }
        public int DuplicatesSkipped { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}

