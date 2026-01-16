using Backend.Models;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Backend.Services.Mappers
{
    public class GALMapper : IMapper
    {
        /// <summary>
        /// Transforma datos de estaciones ITV de Galicia en formato JSON al modelo unificado
        /// </summary>
        /// <param name="json">Cadena JSON con los datos de estaciones de Galicia</param>
        /// <param name="validateExistingCoordinates">Si true, valida coordenadas con Selenium</param>
        /// <param name="processCV">No utilizado en este mapper</param>
        /// <param name="processGAL">Si false, retorna resultado vacío</param>
        /// <param name="processCAT">No utilizado en este mapper</param>
        /// <returns>Objeto MapResult con datos unificados de Galicia</returns>
        public MapResult Map(string json, bool validateExistingCoordinates, bool processCV, bool processGAL, bool processCAT)
        {
            var result = new MapResult();
            if (!processGAL) {
                Log.Warning("IGNORANDO GAL.");
                return result;
            }

            Log.Information("");
            Log.Information("------------------------------------------------");
            Log.Information("Paso GAL: Iniciando mapeo de datos para Galicia.");
            var data = JArray.Parse(json);
            if (data == null)
            {
                Log.Warning("Paso GAL: No se encontraron datos en el JSON.");
                return result;
            }

            using var driver = SeleniumGeocoder.CreateDriver();
            bool cookiesAccepted = false;

            foreach (var item in data)
            {
                try
                {
                    var u = new UnifiedData();

                    // nombre (1/2)
                    string stationName = (string?)item["NOME DA ESTACIÓN"] ?? "";
                    var operations = new List<RepairedOperation>();
                    if (string.IsNullOrWhiteSpace(stationName))
                    {
                        Log.Information("");
                        Log.Information("Paso GAL: Procesando estación sin nombre...");
                    }
                    else
                    {
                        Log.Information("");
                        Log.Information("Paso GAL: Procesando estación '{Name}'...", stationName);
                    }

                    // dirección
                    string stationAddress = (string?)item["ENDEREZO"] ?? "";
                    if (string.IsNullOrWhiteSpace(stationAddress))
                    {
                        result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "GAL", Name = stationName, Locality = "", ErrorReason = "Falta dirección" });
                        continue;
                    }
                    string originalAddress = stationAddress;
                    u.Station.address = Utilities.NormalizeAddressGalAndCv(stationAddress);
                    if (!originalAddress.Equals(u.Station.address))
                    {
                        Log.Information("Paso GAL: Actualizado direccion de '{oldAddress}' a '{newAddress}'.", originalAddress, u.Station.address);
                        operations.Add(new RepairedOperation { ErrorReason = "Dirección no normalizada", OperationPerformed = $"Normalizada de '{originalAddress}' a '{u.Station.address}'" });
                    }

                    // nombre (2/2)
                    if (string.IsNullOrWhiteSpace(stationName))
                    {
                        stationName = "Estación ITV (GAL) de " + u.Station.address;
                        u.Station.name = stationName;
                        operations.Add(new RepairedOperation { ErrorReason = "Nombre faltante", OperationPerformed = $"Nombre establecido a '{stationName}'" });
                        Log.Information("Paso GAL: Dado '{Name}' como nombre a estación sin nombre.", stationName);
                    }
                    else
                    {
                        string originalName = stationName;
                        stationName = "Estación ITV (GAL) de " + Utilities.ExtractStationNameWithSimilarity(stationName);
                        u.Station.name = stationName;
                        operations.Add(new RepairedOperation { ErrorReason = "Nombre no prefijado", OperationPerformed = $"Nombre actualizado de '{originalName}' a '{stationName}'" });
                        Log.Information("Paso GAL: Actualizado nombre de estación a '{Name}'.", stationName);
                    }

                    // código postal
                    string postalCode = (string?)item["CÓDIGO POSTAL"] ?? "";
                    if (!Utilities.IsValidPostalCodeForCommunity(postalCode, "Galicia"))
                    {
                        result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "GAL", Name = stationName, Locality = "", ErrorReason = $"Código postal inválido '{postalCode}' para Galicia" });
                        continue;
                    }
                    u.Station.postal_code = postalCode;

                    // provincia
                    string rawProvinceName = (string?)item["PROVINCIA"] ?? "";
                    u.ProvinceName = Utilities.NormalizeProvinceName(rawProvinceName);
                    string? provinceFromCP = Utilities.GetProvinceFromPostalCode(postalCode);
                    bool provinceRepaired = false;
                    if ((u.ProvinceName == "Desconocida" && !string.IsNullOrEmpty(postalCode)) || (provinceFromCP != null && !u.ProvinceName.Equals(provinceFromCP)))
                    {
                        if (provinceFromCP != null)
                        {
                            u.ProvinceName = provinceFromCP;
                            provinceRepaired = true;
                            Log.Information("Paso GAL: Provincia '{ProvinceName}' obtenida del código postal '{PostalCode}'.", u.ProvinceName, u.Station.postal_code);
                        }
                    }
                    if (u.ProvinceName == "Desconocida")
                    {
                        result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "GAL", Name = stationName, Locality = "", ErrorReason = $"Provincia desconocida para '{rawProvinceName}' con CP '{postalCode}'" });
                        continue;
                    }

                    // localidad
                    string rawLocalityName = (string?)item["CONCELLO"] ?? "";
                    u.LocalityName = Utilities.NormalizeLocalityName(rawLocalityName);
                    if (string.IsNullOrWhiteSpace(u.LocalityName))
                    {
                        result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "GAL", Name = stationName, Locality = rawLocalityName, ErrorReason = "Localidad desconocida" });
                        continue;
                    }

                    if (provinceRepaired)
                    {
                        operations.Add(new RepairedOperation { ErrorReason = "Provincia incorrecta", OperationPerformed = $"Provincia establecida a '{u.ProvinceName}' desde código postal" });
                    }

                    // información de contacto
                    string telefono = (string?)item["TELÉFONO"] ?? "";
                    string correo = (string?)item["CORREO ELECTRÓNICO"] ?? "";

                    var contactParts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(telefono))
                        contactParts.Add($"Teléfono: {telefono}");
                    if (!string.IsNullOrWhiteSpace(correo) && !Utilities.IsUrl(correo) && Utilities.IsValidEmail(correo))
                        contactParts.Add($"Correo: {correo}");

                    u.Station.contact = contactParts.Count > 0 ? string.Join(", ", contactParts) : null;

                    // horario
                    u.Station.schedule = (string?)item["HORARIO"] ?? "";

                    // url
                    u.Station.url = (string?)item["SOLICITUDE DE CITA PREVIA"] ?? "https://www.sycitv.com/es/";

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
                        result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "GAL", Name = stationName, Locality = u.LocalityName, ErrorReason = $"Coordenadas inválidas (lat: {u.Station.latitude}, lon: {u.Station.longitude})" });
                        continue;
                    }

                    if (validateExistingCoordinates)
                    {
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
                            result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "GAL", Name = stationName, Locality = u.LocalityName, ErrorReason = $"La dirección '{u.Station.address}' no coincide con las coordenadas (lat: {u.Station.latitude}, lon: {u.Station.longitude})" });
                            continue;
                        }
                    }

                    // fin
                    if (operations.Any())
                    {
                        result.RepairedRecords.Add(new RepairedRecord { DataSource = "GAL", Name = stationName, Locality = u.LocalityName, Operations = operations });
                    }
                    result.UnifiedData.Add(u);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Paso GAL: Error mapeando el item: {Item}", item);
                    result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "GAL", Name = "Desconocido", Locality = "Desconocido", ErrorReason = $"Error mapeando: {ex.Message}" });
                }
            }

            Log.Information("");
            Log.Information("Paso GAL: Mapeo de datos para Galicia finalizado. Registros totales: {RecordCount}.", result.UnifiedData.Count);
            return result;
        }
    }
}
