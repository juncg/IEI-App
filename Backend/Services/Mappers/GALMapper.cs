using Backend.Models;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using Serilog;

namespace Backend.Services.Mappers
{
    public class GALMapper : IMapper
    {
        public void Map(string json, List<UnifiedData> list)
        {
            Log.Information("Paso GAL: Iniciando mapeo de datos para Galicia.");
            var data = JArray.Parse(json);

            using var driver = SeleniumGeocoder.CreateDriver();
            bool cookiesAccepted = false;

            foreach (var item in data)
            {
                var u = new UnifiedData();

                string postalCode = (string?)item["CÓDIGO POSTAL"] ?? "";
                if (!Utilities.IsValidPostalCodeForCommunity(postalCode, "Galicia"))
                {
                    Log.Warning("Paso GAL: Estación descartada por código postal inválido '{PostalCode}' para Galicia.", postalCode);
                    continue;
                }

                string rawProvinceName = (string?)item["PROVINCIA"] ?? "";
                u.ProvinceName = Utilities.NormalizeProvinceName(rawProvinceName);

                if (u.ProvinceName == "Desconocida" && !string.IsNullOrEmpty(postalCode))
                {
                    string? provinceFromCP = Utilities.GetProvinceFromPostalCode(postalCode);
                    if (provinceFromCP != null)
                    {
                        u.ProvinceName = provinceFromCP;
                        Log.Information("Paso GAL: Provincia obtenida del código postal: {ProvinceName}", u.ProvinceName);
                    }
                }

                if (u.ProvinceName == "Desconocida")
                {
                    Log.Warning("Paso GAL: Estación descartada por provincia desconocida para '{RawProvinceName}' con CP '{PostalCode}'", rawProvinceName, postalCode);
                    continue;
                }

                u.LocalityName = (string?)item["CONCELLO"] ?? "Desconocido";

                string nombre = (string?)item["NOME DA ESTACIÓN"] ?? u.LocalityName;
                u.Station.name = nombre;

                u.Station.address = (string?)item["ENDEREZO"] ?? "";
                u.Station.postal_code = postalCode;

                string telefono = (string?)item["TELÉFONO"] ?? "";
                string correo = (string?)item["CORREO ELECTRÓNICO"] ?? "";

                var contactParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(telefono))
                    contactParts.Add($"Teléfono: {telefono}");
                if (!string.IsNullOrWhiteSpace(correo))
                    contactParts.Add($"Correo: {correo}");

                u.Station.contact = contactParts.Count > 0 ? string.Join(" ", contactParts) : null;

                u.Station.schedule = (string?)item["HORARIO"] ?? "";
                u.Station.url = (string?)item["SOLICITUDE DE CITA PREVIA"] ?? "";
                u.Station.type = StationType.Fixed_station;

                string coords = (string?)item["COORDENADAS GMAPS"] ?? "";
                if (!string.IsNullOrEmpty(coords))
                {
                    var coordsParsed = Utilities.ParseDegreesMinutesCoordinates(coords);
                    if (coordsParsed.HasValue)
                    {
                        u.Station.latitude = coordsParsed.Value.lat;
                        u.Station.longitude = coordsParsed.Value.lon;
                    }
                }

                if (!Utilities.AreValidCoordinates(u.Station.latitude, u.Station.longitude))
                {
                    Log.Warning("Paso GAL: Coordenadas inválidas ({Lat}, {Lon}) para {StationName}. Intentando obtenerlas con Selenium...",
                        u.Station.latitude, u.Station.longitude, u.Station.name);

                    var (lat, lon) = SeleniumGeocoder.GetCoordinates(
                        driver,
                        u.Station.address ?? "",
                        ref cookiesAccepted,
                        u.Station.postal_code ?? "",
                        u.LocalityName ?? "",
                        u.ProvinceName ?? ""
                    );

                    u.Station.latitude = lat;
                    u.Station.longitude = lon;
                }

                if (!Utilities.CompareAddressWithCoordinates(
                    driver,
                    u.Station.address ?? "",
                    u.Station.latitude,
                    u.Station.longitude,
                    ref cookiesAccepted,
                    u.Station.postal_code ?? "",
                    u.LocalityName ?? "",
                    u.ProvinceName ?? ""
                ))
                {
                    Log.Warning("Estación '{Name}' descartada: la dirección '{Address}' no coincide con las coordenadas (lat: '{Lat}', lon: '{Lon}')", u.Station.name, u.Station.address, u.Station.latitude, u.Station.longitude);
                    continue;
                }

                list.Add(u);
            }
            Log.Information("Paso GAL: Mapeo de datos para Galicia finalizado. Registros totales: {RecordCount}", list.Count);
        }
    }
}
