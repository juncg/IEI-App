using Backend.Api.Busqueda.Converters;
using Backend.Models;
using Backend.Repositories;
using Microsoft.Data.Sqlite;
using Serilog;

namespace Backend.Api.Busqueda.Logic
{
    public class SearchService
    {
        private readonly StationRepository _stationRepository;

        /// <summary>
        /// Inicializa una nueva instancia de SearchService
        /// </summary>
        public SearchService()
        {
            _stationRepository = new StationRepository();
        }

        /// <summary>
        /// Busca estaciones en la base de datos aplicando filtros opcionales
        /// </summary>
        /// <param name="name">Nombre de la estación (búsqueda parcial, opcional)</param>
        /// <param name="type">Tipo de estación (opcional)</param>
        /// <param name="locality">Nombre de la localidad (búsqueda parcial, opcional)</param>
        /// <param name="postalCode">Código postal (búsqueda parcial, opcional)</param>
        /// <param name="province">Nombre de la provincia (búsqueda parcial, opcional)</param>
        /// <returns>Enumeración de StationDto que cumplen con los criterios</returns>
        public IEnumerable<StationDto> SearchStations(string? name, int? type, string? locality, string? postalCode, string? province)
        {
            using var connection = new SqliteConnection("Data Source=databases/iei.db");
            connection.Open();

            List<Station> stations = _stationRepository.SearchStations(connection, name, type, locality, postalCode, province);
            return stations.Select(StationConverter.Convert);
        }

        /// <summary>
        /// Obtiene todas las estaciones que tienen coordenadas geográficas válidas
        /// </summary>
        /// <returns>Enumeración de StationDto con coordenadas definidas</returns>
        public IEnumerable<StationDto> GetStationsWithCoordinates()
        {
            using var connection = new SqliteConnection("Data Source=databases/iei.db");
            connection.Open();

            List<Station> stations = _stationRepository.GetStationsWithCoordinates(connection);
            return stations.Select(StationConverter.Convert);
        }
    }
}