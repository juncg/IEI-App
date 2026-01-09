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

        public SearchService()
        {
            _stationRepository = new StationRepository();
        }

        public IEnumerable<StationDto> SearchStations(string? name, int? type, string? locality, string? postalCode, string? province)
        {
            using var connection = new SqliteConnection("Data Source=databases/iei.db");
            connection.Open();

            List<Station> stations = _stationRepository.SearchStations(connection, name, type, locality, postalCode, province);
            return stations.Select(StationConverter.Convert);
        }

        public IEnumerable<StationDto> GetStationsWithCoordinates()
        {
            using var connection = new SqliteConnection("Data Source=databases/iei.db");
            connection.Open();

            List<Station> stations = _stationRepository.GetStationsWithCoordinates(connection);
            return stations.Select(StationConverter.Convert);
        }
    }
}