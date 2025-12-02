using Backend.Models;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using Serilog;
using System.Globalization;

namespace Backend.Services.Mappers
{
    public class CATMapper : IMapper
    {
        public void Map(string json, List<UnifiedData> list, bool validateExistingCoordinates)
        {
            Log.Information("");
            Log.Information("------------------------------------------------");
            Log.Information("Paso CAT: Iniciando mapeo de datos para Cataluña.");
            var root = JObject.Parse(json);
            var rows = root["response"]?["row"]?["row"];
            if (rows == null)
            {
                Log.Warning("Paso CAT: No se encontraron datos en el JSON.");
                return;
            }

            using var driver = SeleniumGeocoder.CreateDriver();
            bool cookiesAccepted = false;

            foreach (var item in rows)
            {
                var u = new UnifiedData();

                // nombre (1/2)
                string stationName = (string?)item["denominaci"] ?? "";
                if (string.IsNullOrWhiteSpace(stationName))
                {
                    Log.Information("");
                    Log.Information("Paso CAT: Procesando estación sin nombre...");
                }
                else
                {
                    Log.Information("");
                    Log.Information("Paso CAT: Procesando estación '{Name}'...", stationName);
                }

                // dirección
                string stationAddress = (string?)item["adre_a"] ?? "";
                if (string.IsNullOrWhiteSpace(stationAddress))
                {
                    if (string.IsNullOrWhiteSpace(stationName))
                    {
                        Log.Warning("Paso CAT: Estación sin nombre descartada por falta de dirección.");
                    }
                    else
                    {
                        Log.Warning("Paso CAT: Estación '{Name}' descartada por falta de dirección.", stationName);
                    }
                    continue;
                }
                u.Station.address = stationAddress;

                // código postal
                string postalCode = (string?)item["cp"] ?? "";
                if (!Utilities.IsValidPostalCodeForCommunity(postalCode, "Cataluña"))
                {
                    if (string.IsNullOrWhiteSpace(stationName))
                    {
                        Log.Warning("Paso CAT: Estación sin nombre descartada por código postal inválido '{PostalCode}' para Cataluña.", postalCode);
                    }
                    else
                    {
                        Log.Warning("Paso CAT: Estación '{Name}' descartada por código postal inválido '{PostalCode}' para Cataluña.", stationName, postalCode);
                    }
                    continue;
                }
                u.Station.postal_code = postalCode;

                // provincia
                string rawProvinceName = (string?)item["serveis_territorials"] ?? "";
                u.ProvinceName = Utilities.NormalizeProvinceName(rawProvinceName);
                if (u.ProvinceName == "Desconocida" && !string.IsNullOrEmpty(postalCode))
                {
                    string? provinceFromCP = Utilities.GetProvinceFromPostalCode(postalCode);
                    if (provinceFromCP != null)
                    {
                        u.ProvinceName = provinceFromCP;
                        Log.Information("Paso CAT: Provincia '{ProvinceName}' obtenida del código postal.", u.ProvinceName);
                    }
                }
                if (u.ProvinceName == "Desconocida")
                {
                    if (string.IsNullOrWhiteSpace(stationName))
                    {
                        Log.Warning("Paso CAT: Estación sin nombre descartada por provincia desconocida para '{RawProvinceName}' con CP '{PostalCode}'", rawProvinceName, postalCode);
                    }
                    else
                    {
                        Log.Warning("Paso CAT: Estación '{Name}' descartada por provincia desconocida para '{RawProvinceName}' con CP '{PostalCode}'", stationName, rawProvinceName, postalCode);
                    }
                    continue;
                }

                // localidad
                string rawLocalityName = (string?)item["municipi"] ?? "";
                u.LocalityName = Utilities.NormalizeLocalityName(rawLocalityName);
                if (string.IsNullOrWhiteSpace(u.LocalityName))
                {
                    if (string.IsNullOrWhiteSpace(stationName))
                    {
                        Log.Warning("Paso CAT: Estación sin nombre descartada por localidad desconocida.");
                    }
                    else
                    {
                        Log.Warning("Paso CAT: Estación '{Name}' descartada por localidad desconocida.", stationName);
                    }
                    continue;
                }

                // nombre (2/2)
                if (string.IsNullOrWhiteSpace(stationName))
                {
                    stationName = "Estación ITV de " + u.LocalityName;
                    u.Station.name = stationName;
                    Log.Information("Paso CAT: Dado '{Name}' como nombre a estación sin nombre.", stationName);
                }
                else
                {
                    stationName = "Estación ITV de " + stationName;
                    u.Station.name = stationName;
                    Log.Information("Paso CAT: Actualizado nombre de estación a '{Name}'.", stationName);
                }

                // información de contacto
                string contact = (string?)item["correu_electr_nic"] ?? "";
                if (!string.IsNullOrWhiteSpace(contact) && !Utilities.IsUrl(contact))
                {
                    u.Station.contact = contact;
                }

                // horario
                u.Station.schedule = (string?)item["horari_de_servei"] ?? "";

                // url
                u.Station.url = (string?)item["web"]?["@url"] ?? "";

                // tipo
                u.Station.type = StationType.Fixed_station;

                // coordenadas (divididas por 1000000)
                if (double.TryParse((string?)item["lat"], NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
                    u.Station.latitude = lat / 1000000.0;

                if (double.TryParse((string?)item["long"], NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
                    u.Station.longitude = lon / 1000000.0;

                if (!Utilities.AreValidCoordinates(u.Station.latitude, u.Station.longitude))
                {
                    Log.Warning("Paso CAT: Estación '{Name}' descartada: coordenadas inválidas (lat: {Lat}, lon: {Lon}).",
                        stationName, u.Station.latitude, u.Station.longitude);
                    continue;
                }

                if (validateExistingCoordinates)
                {
                    Log.Information("Paso CAT: Comprobando que la dirección '{Address}' y las coordenadas (lat: {Lat}, lon: {Lon}) apuntan al mismo lugar con Selenium...",
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
                        Log.Warning("Paso CAT: Estación '{Name}' descartada: la dirección '{Address}' no coincide con las coordenadas (lat: {Lat}, lon: {Lon})",
                            stationName, u.Station.address, u.Station.latitude, u.Station.longitude);
                        continue;
                    }
                }

                list.Add(u);
            }

            Log.Information("");
            Log.Information("Paso CAT: Mapeo de datos para Cataluña finalizado. Registros totales: {RecordCount}.", list.Count);
            Log.Information("");
        }
    }
}
