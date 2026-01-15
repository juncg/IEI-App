using Backend.Models;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Backend.Services.Mappers
{
    public class CVMapper : IMapper
    {
        private readonly Dictionary<string, int> stationCounters = new Dictionary<string, int>();

        /// <summary>
        /// Transforma datos de estaciones ITV de la Comunidad Valenciana en formato JSON al modelo unificado
        /// </summary>
        /// <param name="json">Cadena JSON con los datos de estaciones de la Comunidad Valenciana</param>
        /// <param name="validateExistingCoordinates">Si true, valida coordenadas con Selenium</param>
        /// <param name="processCV">Si false, retorna resultado vacío</param>
        /// <param name="processGAL">No utilizado en este mapper</param>
        /// <param name="processCAT">No utilizado en este mapper</param>
        /// <returns>Objeto MapResult con datos unificados de la Comunidad Valenciana</returns>
        public MapResult Map(string json, bool validateExistingCoordinates, bool processCV, bool processGAL, bool processCAT)
        {
            var result = new MapResult();
            if (!processCV)
            {
                Log.Warning("IGNORANDO CV.");
                return result;
            }

            Log.Information("Paso CV: Iniciando mapeo de datos para la Comunidad Valenciana.");
            var data = JArray.Parse(json);
            if (data == null)
            {
                Log.Warning("Paso CV: No se encontraron datos en el JSON.");
                return result;
            }

            using var driver = SeleniumGeocoder.CreateDriver();
            bool cookiesAccepted = false;

            foreach (var item in data)
            {
                try
                {
                    var u = new UnifiedData();

                    Log.Information("");
                    Log.Information("Paso CV: Procesando nueva estación...");
                    var operations = new List<RepairedOperation>();

                    // tipo
                    string tipo = ((string?)item["TIPO ESTACIÓN"] ?? "").ToLower();
                    Log.Information("Paso CV: Tipo de estación '{Type}' detectado", tipo);

                    if (tipo.Contains("móvil"))
                        u.Station.type = StationType.Mobile_station;
                    else if (tipo.Contains("fija"))
                        u.Station.type = StationType.Fixed_station;
                    else
                        u.Station.type = StationType.Others;

                    // dirección
                    string stationAddress = (string?)item["DIRECCIÓN"] ?? "";
                    if (string.IsNullOrWhiteSpace(stationAddress))
                    {
                        result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "CV", Name = "", Locality = "", ErrorReason = "Falta dirección" });
                        continue;
                    }
                    string originalAddress = stationAddress;
                    u.Station.address = u.Station.type == StationType.Fixed_station
                        ? Utilities.NormalizeAddressGalAndCv(stationAddress)
                        : "";
                    if (u.Station.type == StationType.Fixed_station && !originalAddress.Equals(u.Station.address))
                    {
                        Log.Information("Paso CV: Actualizado direccion de '{oldAddress}' a '{newAddress}'.", originalAddress, u.Station.address);
                        operations.Add(new RepairedOperation { ErrorReason = "Dirección no normalizada", OperationPerformed = $"Normalizada de '{originalAddress}' a '{u.Station.address}'" });
                    }

                    // localidad
                    string rawLocalityName = (string?)item["MUNICIPIO"] ?? "";
                    u.LocalityName = Utilities.NormalizeLocalityName(rawLocalityName);
                    if (string.IsNullOrWhiteSpace(u.LocalityName) && u.Station.type == StationType.Fixed_station)
                    {
                        result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "CV", Name = "", Locality = rawLocalityName, ErrorReason = "Falta localidad para estación fija" });
                        continue;
                    }

                    // nombre
                    string assignedName = "";
                    if (u.Station.type == StationType.Fixed_station)
                    {
                        assignedName = "Estación ITV (CV) de " + u.LocalityName;
                        u.Station.name = assignedName;
                        operations.Add(new RepairedOperation { ErrorReason = "Nombre faltante", OperationPerformed = $"Nombre asignado a '{assignedName}'" });
                        Log.Information("Paso CV: Nombre '{Name}' asignado a estación fija.", u.Station.name);
                    }
                    else
                    {
                        string stationType = Utilities.ExtractStationSubtype(stationAddress);

                        if (!stationCounters.ContainsKey(stationType))
                        {
                            stationCounters[stationType] = 1;
                        }
                        else
                        {
                            stationCounters[stationType]++;
                        }

                        assignedName = $"Estación ITV (CV) {stationType} {stationCounters[stationType]:D2}";
                        u.Station.name = assignedName;
                        operations.Add(new RepairedOperation { ErrorReason = "Nombre faltante", OperationPerformed = $"Nombre asignado a '{assignedName}'" });
                        Log.Information("Paso CV: Nombre '{Name}' asignado a estación no fija.", u.Station.name);
                    }

                    // codigo postal
                    string postalCode = (string?)item["C.POSTAL"] ?? "";
                    if (u.Station.type == StationType.Fixed_station && !Utilities.IsValidPostalCodeForCommunity(postalCode, "Comunidad Valenciana"))
                    {
                        result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "CV", Name = assignedName, Locality = u.LocalityName, ErrorReason = $"Código postal inválido '{postalCode}' para la Comunidad Valenciana" });
                        continue;
                    }
                    u.Station.postal_code = u.Station.type == StationType.Fixed_station ? postalCode : "";


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
                            Log.Information("Paso CV: Provincia '{ProvinceName}' obtenida del código postal '{PostalCode}'.", u.ProvinceName, u.Station.postal_code);
                        }
                    }
                    if (u.ProvinceName == "Desconocida" && u.Station.type == StationType.Fixed_station)
                    {
                        result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "CV", Name = assignedName, Locality = u.LocalityName, ErrorReason = $"Provincia desconocida para '{rawProvinceName}' con CP '{postalCode}'" });
                        continue;
                    }

                    if (provinceRepaired)
                    {
                        operations.Add(new RepairedOperation { ErrorReason = "Provincia incorrecta", OperationPerformed = $"Provincia establecida a '{u.ProvinceName}' desde código postal" });
                    }

                    // información de contacto
                    string correo = (string?)item["CORREO"] ?? "";
                    if (!string.IsNullOrWhiteSpace(correo) && !Utilities.IsUrl(correo))
                    {
                        u.Station.contact = "Correo: " + correo;
                    }

                    // horario
                    u.Station.schedule = (string?)item["HORARIOS"] ?? "";

                    // url
                    u.Station.url = "https://sitval.com";

                    // coordenadas
                    var (lat, lon) = u.Station.type == StationType.Fixed_station
                        ? SeleniumGeocoder.GetCoordinates(
                            driver,
                            u.Station.address ?? "",
                            ref cookiesAccepted,
                            u.Station.postal_code ?? "",
                            u.LocalityName ?? "",
                            u.ProvinceName ?? ""
                        )
                        : (null, null);

                    u.Station.latitude = lat;
                    u.Station.longitude = lon;

                    if (u.Station.type == StationType.Fixed_station && !Utilities.AreValidCoordinates(u.Station.latitude, u.Station.longitude))
                    {
                        result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "CV", Name = assignedName, Locality = u.LocalityName, ErrorReason = $"Coordenadas inválidas (lat: {u.Station.latitude}, lon: {u.Station.longitude})" });
                        continue;
                    }

                    // fin
                    if (operations.Any())
                    {
                        result.RepairedRecords.Add(new RepairedRecord { DataSource = "CV", Name = assignedName, Locality = u.LocalityName ?? "", Operations = operations });
                    }
                    result.UnifiedData.Add(u);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Paso CV: Error mapeando el item: {Item}", item);
                    result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "CV", Name = "Desconocido", Locality = "Desconocido", ErrorReason = $"Error mapeando: {ex.Message}" });
                }
            }

            Log.Information("");
            Log.Information("Paso CV: Mapeo de datos para la Comunidad Valenciana finalizado. Registros totales: {RecordCount}", result.UnifiedData.Count);
            return result;
        }
    }
}
