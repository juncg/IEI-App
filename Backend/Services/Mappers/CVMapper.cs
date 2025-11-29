using Backend.Models;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using Serilog;

namespace Backend.Services.Mappers
{
    public class CVMapper : IMapper
    {
        public void Map(string json, List<UnifiedData> list)
        {
            Log.Information("Empezando el mapeo para la Comunidad Valenciana.");
            var data = JArray.Parse(json);

            using var driver = SeleniumGeocoder.CreateDriver();
            bool cookiesAccepted = false;

            foreach (var item in data)
            {
                try
                {
                    var u = new UnifiedData();

                    string tipo = ((string?)item["TIPO ESTACIÓN"] ?? "").ToLower();
                    Log.Debug("Tipo de estación: {StationType}", tipo);

                    if (tipo.Contains("móvil"))
                        u.Station.type = StationType.Mobile_station;
                    else if (tipo.Contains("fija"))
                        u.Station.type = StationType.Fixed_station;
                    else
                        u.Station.type = StationType.Others;

                    string rawLocalityName = (string?)item["MUNICIPIO"] ?? "";
                    u.LocalityName = u.Station.type == StationType.Fixed_station && string.IsNullOrWhiteSpace(rawLocalityName)
                        ? "Desconocido"
                        : rawLocalityName;

                    string postalCode = (string?)item["C.POSTAL"] ?? "";
                    if (u.Station.type == StationType.Fixed_station && !Utilities.IsValidPostalCodeForCommunity(postalCode, "Comunidad Valenciana"))
                    {
                        Log.Warning("Estación descartada: código postal inválido '{PostalCode}' para Comunidad Valenciana", postalCode);
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
                            Log.Information("Provincia obtenida del código postal: {ProvinceName}", u.ProvinceName);
                        }
                    }

                    if (u.ProvinceName == "Desconocida" && u.Station.type == StationType.Fixed_station)
                    {
                        Log.Warning("Estación descartada: no se pudo determinar la provincia para '{RawProvinceName}' con CP '{PostalCode}'", rawProvinceName, postalCode);
                        continue;
                    }

                    u.Station.name = u.Station.type == StationType.Fixed_station
                        ? $"Estación ITV de {u.LocalityName}"
                        : $"Estación {(string?)item["DIRECCIÓN"] ?? u.LocalityName}";

                    u.Station.address = (string?)item["DIRECCIÓN"] ?? "";
                    u.Station.postal_code = u.Station.type == StationType.Fixed_station ? postalCode : "";
                    u.Station.contact = (string?)item["CORREO"] ?? "";
                    u.Station.schedule = (string?)item["HORARIOS"] ?? "";
                    u.Station.url = "https://sitval.com";

                    Log.Debug("Detalles de la estación: {@Station}", u.Station);

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

                    list.Add(u);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error mapeando: {Item}", item);
                }
            }

            Log.Information("Acabado el mapeo para la Comunidad Valenciana. Registros totales: {RecordCount}", list.Count);
        }
    }
}
