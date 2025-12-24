using Backend.Models;

namespace Backend.Converters
{
    public static class StationConverter
    {
        public static StationDto Convert(Station station)
        {
            return new StationDto
            {
                Code = station.code,
                Name = station.name,
                Type = station.type,
                Address = station.address,
                PostalCode = station.postal_code,
                Longitude = station.longitude,
                Latitude = station.latitude,
                Locality = station.locality,
                Province = station.province
            };
        }
    }
}