using Backend.Models;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using Serilog;

namespace Backend.Services.Mappers
{
    public class CVMapper : IMapper
    {
        private readonly Dictionary<string, int> stationCounters = new Dictionary<string, int>();

        public void Map(string json, List<UnifiedData> list, bool validateExistingCoordinates)
        {
            Log.Information("");
            Log.Information("------------------------------------------------");

            Log.Information("Paso CV: Iniciando mapeo de datos para la Comunidad Valenciana.");
            var data = JArray.Parse(json);

            using var driver = SeleniumGeocoder.CreateDriver();
            bool cookiesAccepted = false;

            foreach (var item in data)
            {
                try
                {
                    var u = new UnifiedData();

                    Log.Information("");
                    Log.Information("Paso CV: Procesando nueva estación...");

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
                        Log.Warning("Paso CV: Estación sin nombre DESCARTADA por falta de dirección.");
                        continue;
                    }
                    u.Station.address = u.Station.type == StationType.Fixed_station
                        ? Utilities.NormalizeAddressGalAndCv(stationAddress)
                        : "";
                    if (u.Station.type == StationType.Fixed_station && !stationAddress.Equals(u.Station.address))
                    {
                        Log.Information("Paso CV: Actualizado direccion de '{oldAddress}' a '{newAddress}'.", stationAddress, u.Station.address);
                    }

                    // localidad
                    string rawLocalityName = (string?)item["MUNICIPIO"] ?? "";
                    u.LocalityName = Utilities.NormalizeLocalityName(rawLocalityName);
                    if (string.IsNullOrWhiteSpace(u.LocalityName) && u.Station.type == StationType.Fixed_station)
                    {
                        Log.Warning("Paso CV: Estación en '{Address}' fija DESCARTADA por falta de localidad.", u.Station.address);
                        continue;
                    }

                    // nombre
                    if (u.Station.type == StationType.Fixed_station)
                    {
                        u.Station.name = "Estación ITV (CV) de " + u.LocalityName;
                        Log.Warning("Paso CV: Nombre '{Name}' asignado a estación fija.", u.Station.name);
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

                        u.Station.name = $"Estación ITV (CV) de {stationType} {stationCounters[stationType]:D2}";
                        Log.Information("Paso CV: Nombre '{Name}' asignado a estación no fija.", u.Station.name);
                    }

                    // codigo postal
                    string postalCode = (string?)item["C.POSTAL"] ?? "";
                    if (u.Station.type == StationType.Fixed_station && !Utilities.IsValidPostalCodeForCommunity(postalCode, "Comunidad Valenciana"))
                    {
                        Log.Warning("Paso CV: Estación fija '{Name}' DESCARTADA por código postal inválido '{PostalCode}' para la Comunidad Valenciana.", u.Station.name, postalCode);
                        continue;
                    }
                    u.Station.postal_code = u.Station.type == StationType.Fixed_station ? postalCode : "";


                    // provincia
                    string rawProvinceName = (string?)item["PROVINCIA"] ?? "";
                    u.ProvinceName = Utilities.NormalizeProvinceName(rawProvinceName);
                    if (u.ProvinceName == "Desconocida" && !string.IsNullOrEmpty(postalCode))
                    {
                        string? provinceFromCP = Utilities.GetProvinceFromPostalCode(postalCode);
                        if (provinceFromCP != null)
                        {
                            u.ProvinceName = provinceFromCP;
                            Log.Information("Paso CV: Provincia '{ProvinceName}' obtenida del código postal '{PostalCode}'.", u.ProvinceName, u.Station.postal_code);
                        }
                    }
                    if (u.ProvinceName == "Desconocida" && u.Station.type == StationType.Fixed_station)
                    {
                        Log.Warning("Paso CV: Estación fija '{Name}' DESCARTADA por provincia desconocida para '{RawProvinceName}' con CP '{PostalCode}'", u.Station.name, rawProvinceName, postalCode);
                        continue;
                    }

                    // información de contacto
                    u.Station.contact = (string?)item["CORREO"] ?? "";

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

                    if (!Utilities.AreValidCoordinates(u.Station.latitude, u.Station.longitude))
                    {
                        Log.Warning("Paso CV: Estación '{Name}' DESCARTADA por coordenadas inválidas (lat: {Lat}, lon: {Lon}).",
                            u.Station.name, u.Station.latitude, u.Station.longitude);
                        continue;
                    }

                    // fin
                    list.Add(u);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Paso CV: Error mapeando el item: {Item}", item);
                }
            }

            Log.Information("");
            Log.Information("Paso CV: Mapeo de datos para la Comunidad Valenciana finalizado. Registros totales: {RecordCount}", list.Count);
            Log.Information("");
        }
    }
}
