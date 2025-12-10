using Backend.Models;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Globalization;

namespace Backend.Services.Mappers
{
    public class CATMapper : IMapper
    {
        public MapResult Map(string json, bool validateExistingCoordinates, bool processCV, bool processGAL, bool processCAT)
        {
            var result = new MapResult();
            if (!processCAT) {
                Log.Warning("IGNORANDO CAT.");
                return result;
            }

            Log.Information("");
            Log.Information("------------------------------------------------");
            Log.Information("Paso CAT: Iniciando mapeo de datos para Cataluña.");
            var root = JObject.Parse(json);
            var rows = root["response"]?["row"]?["row"];
            if (rows == null)
            {
                Log.Warning("Paso CAT: No se encontraron datos en el JSON.");
                return result;
            }

            using var driver = SeleniumGeocoder.CreateDriver();
            bool cookiesAccepted = false;

            foreach (var item in rows)
            {
                try
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
                        result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "CAT", Name = stationName, Locality = "", ErrorReason = "Falta dirección" });
                        continue;
                    }
                    string originalAddress = stationAddress;
                    u.Station.address = Utilities.NormalizeAddressCAT(stationAddress);
                    if (!originalAddress.Equals(u.Station.address))
                    {
                        Log.Information("Paso CAT: Actualizado direccion de '{oldAddress}' a '{newAddress}'.", originalAddress, u.Station.address);
                        result.RepairedRecords.Add(new RepairedRecord { DataSource = "CAT", Name = stationName, Locality = "", ErrorReason = "Dirección no normalizada", OperationPerformed = $"Normalizada de '{originalAddress}' a '{u.Station.address}'" });
                    }

                    // código postal
                    string postalCode = (string?)item["cp"] ?? "";
                    if (!Utilities.IsValidPostalCodeForCommunity(postalCode, "Cataluña"))
                    {
                        result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "CAT", Name = stationName, Locality = "", ErrorReason = $"Código postal inválido '{postalCode}' para Cataluña" });
                        continue;
                    }
                    u.Station.postal_code = postalCode;

                    // provincia
                    string rawProvinceName = (string?)item["serveis_territorials"] ?? "";
                    u.ProvinceName = Utilities.NormalizeProvinceName(rawProvinceName);
                    string? provinceFromCP = Utilities.GetProvinceFromPostalCode(postalCode);
                    bool provinceRepaired = false;
                    if ((u.ProvinceName == "Desconocida" && !string.IsNullOrEmpty(postalCode)) || (provinceFromCP != null && !u.ProvinceName.Equals(provinceFromCP)))
                    {
                        if (provinceFromCP != null)
                        {
                            u.ProvinceName = provinceFromCP;
                            provinceRepaired = true;
                            Log.Information("Paso CAT: Provincia '{ProvinceName}' obtenida del código postal '{PostalCode}'.", u.ProvinceName, u.Station.postal_code);
                        }
                    }
                    if (u.ProvinceName == "Desconocida")
                    {
                        result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "CAT", Name = stationName, Locality = "", ErrorReason = $"Provincia desconocida para '{rawProvinceName}' con CP '{postalCode}'" });
                        continue;
                    }

                    // localidad
                    string rawLocalityName = (string?)item["municipi"] ?? "";
                    u.LocalityName = Utilities.NormalizeLocalityName(rawLocalityName);
                    if (string.IsNullOrWhiteSpace(u.LocalityName))
                    {
                        result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "CAT", Name = stationName, Locality = rawLocalityName, ErrorReason = "Localidad desconocida" });
                        continue;
                    }

                    // nombre (2/2)
                    if (string.IsNullOrWhiteSpace(stationName))
                    {
                        stationName = "Estación ITV (CAT) de " + u.LocalityName;
                        u.Station.name = stationName;
                        result.RepairedRecords.Add(new RepairedRecord { DataSource = "CAT", Name = stationName, Locality = u.LocalityName, ErrorReason = "Nombre faltante", OperationPerformed = $"Nombre establecido a '{stationName}'" });
                        Log.Information("Paso CAT: Dado '{Name}' como nombre a estación sin nombre.", stationName);
                    }
                    else
                    {
                        string originalName = stationName;
                        stationName = "Estación ITV (CAT) de " + stationName;
                        u.Station.name = stationName;
                        result.RepairedRecords.Add(new RepairedRecord { DataSource = "CAT", Name = stationName, Locality = u.LocalityName, ErrorReason = "Nombre no prefijado", OperationPerformed = $"Nombre actualizado de '{originalName}' a '{stationName}'" });
                        Log.Information("Paso CAT: Actualizado nombre de estación a '{Name}'.", stationName);
                    }

                    if (provinceRepaired)
                    {
                        result.RepairedRecords.Add(new RepairedRecord { DataSource = "CAT", Name = stationName, Locality = u.LocalityName, ErrorReason = "Provincia incorrecta", OperationPerformed = $"Provincia establecida a '{u.ProvinceName}' desde código postal" });
                    }

                    // información de contacto
                    string correo = (string?)item["correu_electr_nic"] ?? "";
                    if (!string.IsNullOrWhiteSpace(correo) && !Utilities.IsUrl(correo))
                    {
                        u.Station.contact = "Correo: " + correo;
                    }

                    // horario
                    u.Station.schedule = (string?)item["horari_de_servei"] ?? "";

                    // url
                    u.Station.url = (string?)item["web"]?["@url"] ?? "";

                    // tipo
                    u.Station.type = StationType.Fixed_station;

                    // coordenadas
                    if (double.TryParse((string?)item["lat"], NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
                        u.Station.latitude = lat / 1000000.0;

                    if (double.TryParse((string?)item["long"], NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
                        u.Station.longitude = lon / 1000000.0;

                    if (!Utilities.AreValidCoordinates(u.Station.latitude, u.Station.longitude))
                    {
                        result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "CAT", Name = stationName, Locality = u.LocalityName, ErrorReason = $"Coordenadas inválidas (lat: {u.Station.latitude}, lon: {u.Station.longitude})" });
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
                            result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "CAT", Name = stationName, Locality = u.LocalityName, ErrorReason = $"La dirección '{u.Station.address}' no coincide con las coordenadas (lat: {u.Station.latitude}, lon: {u.Station.longitude})" });
                            continue;
                        }
                    }

                    // fin
                    result.UnifiedData.Add(u);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Paso CAT: Error mapeando el item: {Item}", item);
                    result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "CAT", Name = "Desconocido", Locality = "Desconocido", ErrorReason = $"Error mapeando: {ex.Message}" });
                }
            }

            Log.Information("");
            Log.Information("Paso CAT: Mapeo de datos para Cataluña finalizado. Registros totales: {RecordCount}.", result.UnifiedData.Count);
            return result;
        }
    }
}
