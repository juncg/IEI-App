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

                // nombre (1/2)
                string stationName = (string?)item["NOME DA ESTACIÓN"] ?? "";
                if (string.IsNullOrWhiteSpace(stationName))
                {
                    Log.Information("Paso GAL: Procesando estación sin nombre...");
                } else
                {
                    Log.Information("Paso GAL: Procesando estación '{Name}'...", stationName);
                }

                // dirección
                u.Station.address = (string?)item["ENDEREZO"] ?? "";
                if (string.IsNullOrWhiteSpace(u.Station.address))
                {
                    if (string.IsNullOrWhiteSpace(stationName))
                    {
                        Log.Warning("Paso GAL: Estación sin nombre descartada por falta de dirección.");
                    } else
                    {
                        Log.Warning("Paso GAL: Estación '{Name}' descartada por falta de dirección.", stationName);
                    }
                    continue;
                }

                // nombre (2/2)
                if (string.IsNullOrWhiteSpace(stationName))
                {
                    stationName = "Estación " + u.Station.address;
                    u.Station.name = stationName;
                    Log.Information("Paso GAL: Dado '{Name}' como nombre a estación sin nombre.", stationName);
                } else
                {
                    stationName = "Estación " + Utilities.ExtractStationNameWithSimilarity(stationName);
                    u.Station.name = stationName;
                    Log.Information("Paso GAL: Actualizado nombre de estación a '{Name}'.", stationName);
                }

                // código postal
                string postalCode = (string?)item["CÓDIGO POSTAL"] ?? "";
                if (!Utilities.IsValidPostalCodeForCommunity(postalCode, "Galicia"))
                {
                    Log.Warning("Paso GAL: Estación '{Name}' descartada por código postal inválido '{PostalCode}' para Galicia.", stationName, postalCode);
                    continue;
                }
                u.Station.postal_code = postalCode;

                // provincia
                string rawProvinceName = (string?)item["PROVINCIA"] ?? "";
                u.ProvinceName = Utilities.NormalizeProvinceName(rawProvinceName);
                if (u.ProvinceName == "Desconocida" && !string.IsNullOrEmpty(postalCode))
                {
                    string? provinceFromCP = Utilities.GetProvinceFromPostalCode(postalCode);
                    if (provinceFromCP != null)
                    {
                        u.ProvinceName = provinceFromCP;
                        Log.Information("Paso GAL: Provincia '{ProvinceName}' obtenida del código postal.", u.ProvinceName);
                    }
                }
                if (u.ProvinceName == "Desconocida")
                {
                    Log.Warning("Paso GAL: Estación descartada por provincia desconocida para '{RawProvinceName}' con CP '{PostalCode}'", rawProvinceName, postalCode);
                    continue;
                }

                // localidad
                u.LocalityName = (string?)item["CONCELLO"] ?? "";
                if (string.IsNullOrWhiteSpace(u.LocalityName))
                {
                    Log.Warning("Paso GAL: Estación '{Name}' descartada por localidad desconocida", stationName);
                    continue;
                }

                // información de contacto
                string telefono = (string?)item["TELÉFONO"] ?? "";
                string correo = (string?)item["CORREO ELECTRÓNICO"] ?? "";

                var contactParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(telefono))
                    contactParts.Add($"Teléfono: {telefono}");
                if (!string.IsNullOrWhiteSpace(correo))
                    contactParts.Add($"Correo: {correo}");

                u.Station.contact = contactParts.Count > 0 ? string.Join(" ", contactParts) : null;

                // horario
                u.Station.schedule = (string?)item["HORARIO"] ?? "";

                // url
                u.Station.url = (string?)item["SOLICITUDE DE CITA PREVIA"] ?? "https://sitval.com/";

                // tipo
                u.Station.type = StationType.Fixed_station;

                // coordenadas
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
                    Log.Warning("Paso GAL: Coordenadas inválidas (lat: {Lat}, lon: {Lon}) para '{Name}'. Intentando obtenerlas con Selenium...",
                        u.Station.latitude, u.Station.longitude, stationName);

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

                Log.Information("Paso GAL: Comprobando que la dirección '{Address}' y las coordenadas (lat: {Lat}, lon: {Lon}) apuntan al mismo lugar con Selenium...",
                    u.Station.address, u.Station.latitude, u.Station.longitude);

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
                    Log.Warning("Estación '{Name}' descartada: la dirección '{Address}' no coincide con las coordenadas (lat: {Lat}, lon: {Lon})",
                        stationName, u.Station.address, u.Station.latitude, u.Station.longitude);
                    continue;
                }

                list.Add(u);
            }

            Log.Information("Paso GAL: Mapeo de datos para Galicia finalizado. Registros totales: {RecordCount}", list.Count);
        }
    }
}
