namespace Backend.Models
{
    public class StationDto
    {
        public int Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public StationType Type { get; set; }
        public string? Address { get; set; }
        public string? PostalCode { get; set; }
        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
        public string? Locality { get; set; }
        public string? Province { get; set; }
    }
}