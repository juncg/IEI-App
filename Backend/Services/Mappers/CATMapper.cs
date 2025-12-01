using Backend.Models;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using Serilog;
using System.Globalization;

namespace Backend.Services.Mappers
{
    public class CATMapper : IMapper
    {
        public void Map(string json, List<UnifiedData> list)
        {
            Log.Information("Paso CAT: Iniciando mapeo de datos para Cataluña.");
            var root = JObject.Parse(json);
            var rows = root["response"]?["row"]?["row"];
            if (rows == null) return;

            using var driver = SeleniumGeocoder.CreateDriver();
            bool cookiesAccepted = false;

            foreach (var item in rows)
            {
                var u = new UnifiedData();

                string postalCode = (string?)item["cp"] ?? "";
                if (!Utilities.IsValidPostalCodeForCommunity(postalCode, "Cataluña"))
                {
                    Log.Warning("Paso CAT: Estación descartada por código postal inválido '{PostalCode}' para Cataluña.", postalCode);
                    continue;
                }

                string rawProvinceName = (string?)item["serveis_territorials"] ?? "";
                u.ProvinceName = Utilities.NormalizeProvinceName(rawProvinceName);

                if (u.ProvinceName == "Desconocida" && !string.IsNullOrEmpty(postalCode))
                {
                    string? provinceFromCP = Utilities.GetProvinceFromPostalCode(postalCode);
                    if (provinceFromCP != null)
                    {
                        u.ProvinceName = provinceFromCP;
                        Log.Information("Provincia obtenida del código postal: {ProvinceName}", u.ProvinceName);
                    }
                }

                if (u.ProvinceName == "Desconocida")
                {
                    Log.Warning("Estación descartada: no se pudo determinar la provincia para '{RawProvinceName}' con CP '{PostalCode}'", rawProvinceName, postalCode);
                    continue;
                }

                u.LocalityName = (string?)item["municipi"] ?? "Desconocido";

                string nombre = (string?)item["denominaci"] ?? u.LocalityName;
                u.Station.name = $"Estación ITV de {nombre}";

                u.Station.address = (string?)item["adre_a"];
                u.Station.postal_code = postalCode;

                string contact = (string?)item["correu_electr_nic"] ?? "";
                if (!string.IsNullOrWhiteSpace(contact) && !Utilities.IsUrl(contact))
                {
                    u.Station.contact = contact;
                }

                u.Station.schedule = (string?)item["horari_de_servei"] ?? "";
                u.Station.url = (string?)item["web"]?["@url"] ?? "";
                u.Station.type = StationType.Fixed_station;

                if (double.TryParse((string?)item["lat"], NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
                    u.Station.latitude = lat / 100000.0;

                if (double.TryParse((string?)item["long"], NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
                    u.Station.longitude = lon / 100000.0;

                if (!Utilities.AreValidCoordinates(u.Station.latitude, u.Station.longitude))
                {
                    Log.Warning("Coordenadas inválidas ({Lat}, {Lon}) para {StationName}. Intentando obtenerlas con Selenium...",
                        u.Station.latitude, u.Station.longitude, u.Station.name);

                    var (newLat, newLon) = SeleniumGeocoder.GetCoordinates(
                        driver,
                        u.Station.address ?? "",
                        ref cookiesAccepted,
                        u.Station.postal_code ?? "",
                        u.LocalityName ?? "",
                        u.ProvinceName ?? ""
                    );

                    u.Station.latitude = newLat;
                    u.Station.longitude = newLon;
                }

                list.Add(u);
            }
            Log.Information("Paso CAT: Mapeo de datos para Cataluña finalizado. Registros totales: {RecordCount}", list.Count);
        }
    }
}
