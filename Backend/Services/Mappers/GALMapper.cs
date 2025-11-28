using Backend.Models;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Backend.Services.Mappers
{
    public class GALMapper : IMapper
    {
        public void Map(string json, List<UnifiedData> list)
        {
            Log.Information("Mapeando datos para Galicia.");
            var data = JArray.Parse(json);
            foreach (var item in data)
            {
                var u = new UnifiedData();

                string postalCode = (string?)item["CÓDIGO POSTAL"] ?? "";
                if (!Utilities.IsValidPostalCodeForCommunity(postalCode, "Galicia"))
                {
                    Log.Warning("Estación descartada: código postal inválido '{PostalCode}' para Galicia", postalCode);
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

                if (u.ProvinceName == "Desconocida")
                {
                    Log.Warning("Estación descartada: no se pudo determinar la provincia para '{RawProvinceName}' con CP '{PostalCode}'", rawProvinceName, postalCode);
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

                list.Add(u);
            }
            Log.Information("Finalizado el mapeo de datos para Galicia.");
        }
    }
}
