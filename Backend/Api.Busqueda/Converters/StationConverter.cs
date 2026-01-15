using Backend.Models;

namespace Backend.Api.Busqueda.Converters
{
    public static class StationConverter
    {
        /// <summary>
        /// Convierte un objeto Station en un objeto StationDto
        /// </summary>
        /// <param name="station">Objeto Station a convertir</param>
        /// <returns>Objeto StationDto con los mismos datos</returns>
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
                Province = station.province,
                Description = station.description,
                Schedule = station.schedule,
                Contact = station.contact,
                Url = station.url
            };
        }
    }
}