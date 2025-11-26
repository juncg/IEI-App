using Backend.Models;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Backend
{
    public class UnifiedData
    {
        public string ProvinceName { get; set; } = string.Empty;
        public string LocalityName { get; set; } = string.Empty;
        public Station Station { get; set; } = new Station();
    }

    public class Mapper
    {
        public static List<UnifiedData> ExecuteMapping(string folderPath)
        {
            var unifiedList = new List<UnifiedData>();
            var files = Directory.GetFiles(folderPath, "*.json");

            foreach (var file in files)
            {
                string json = File.ReadAllText(file);
                string fileName = Path.GetFileName(file).ToLower();

                if (fileName.Contains("estaciones")) // CV (Comunidad Valenciana)
                {
                    MapCV(json, unifiedList);
                }
                else if (fileName.Contains("itv-cat")) // CAT (Cataluña)
                {
                    MapCAT(json, unifiedList);
                }
                else if (fileName.Contains("estacions_itv")) // GAL (Galicia)
                {
                    MapGAL(json, unifiedList);
                }
            }

            Console.WriteLine($"Mapping finished. Total records: {unifiedList.Count}");
            return unifiedList;
        }

        private static async void MapCV(string json, List<UnifiedData> list)
        {
            var data = JArray.Parse(json);
            foreach (var item in data)
            {
                var u = new UnifiedData();
                u.ProvinceName = (string)item["PROVINCIA"] ?? "Desconocida";
                u.LocalityName = (string)item["MUNICIPIO"] ?? "Desconocido";

                string tipo = ((string)item["TIPO ESTACIÓN"] ?? "").ToLower();
                if (tipo.Contains("móvil")) u.Station.type = StationType.Mobile_station;
                else if (tipo.Contains("fija")) u.Station.type = StationType.Fixed_station;
                else u.Station.type = StationType.Others;

                if (u.Station.type == StationType.Fixed_station)
                {
                    u.Station.name = $"Estación ITV de {u.LocalityName} ({(string)item["Nº ESTACIÓN"]})";
                }
                else
                {
                    string direccion = (string)item["DIRECCIÓN"] ?? u.LocalityName;
                    direccion = Regex.Replace(direccion, @"\.+", "");
                    u.Station.name = $"Estación ITV de " + direccion + $" ({(string)item["Nº ESTACIÓN"]})";
                }

                u.Station.address = (string)item["DIRECCIÓN"];
                u.Station.postal_code = (string)item["C.POSTAL"];
                u.Station.contact = (string)item["CORREO"];
                u.Station.schedule = (string)item["HORARIOS"];
                u.Station.url = "https://sitval.com";
                
                var (lat, lon) = await GeocodeAddressAsync(u.Station.address, u.Station.postal_code);
                u.Station.latitude = lat;
                u.Station.longitude = lon;

                list.Add(u);
            }
        }

        private static void MapCAT(string json, List<UnifiedData> list)
        {
            var root = JObject.Parse(json);
            var rows = root["response"]?["row"]?["row"];
            if (rows == null) return;

            foreach (var item in rows)
            {
                var u = new UnifiedData();
                u.ProvinceName = (string)item["serveis_territorials"] ?? "Barcelona"; // Default o mapeo específico
                u.LocalityName = (string)item["municipi"] ?? "Desconocido";

                string nombre = (string)item["denominaci"] ?? u.LocalityName;
                string codigo = (string)item["cod_estacion"] ?? "";
                u.Station.name = $"Estación ITV de {nombre} ({codigo})";
                
                u.Station.address = (string)item["adre_a"];
                u.Station.postal_code = (string)item["cp"];
                u.Station.contact = (string)item["correu_electr_nic"];
                u.Station.schedule = (string)item["horari_de_servei"];
                u.Station.url = (string)item["web"]?["@url"];
                u.Station.type = StationType.Fixed_station;

                if (double.TryParse((string)item["lat"], NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
                    u.Station.latitude = lat / 100000.0;
                
                if (double.TryParse((string)item["long"], NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
                    u.Station.longitude = lon / 100000.0;

                list.Add(u);
            }
        }

        private static void MapGAL(string json, List<UnifiedData> list)
        {
            var data = JArray.Parse(json);
            foreach (var item in data)
            {
                var u = new UnifiedData();
                u.ProvinceName = (string)item["PROVINCIA"] ?? "Desconocida";
                u.LocalityName = (string)item["CONCELLO"] ?? "Desconocido";

                string nombre = (string)item["NOME DA ESTACIÓN"] ?? u.LocalityName;
                u.Station.name = nombre;
                
                u.Station.address = (string)item["ENDEREZO"];
                u.Station.postal_code = (string)item["CÓDIGO POSTAL"];
                u.Station.contact = $"{item["TELÉFONO"]} {item["CORREO ELECTRÓNICO"]}";
                u.Station.schedule = (string)item["HORARIO"];
                u.Station.url = (string)item["SOLICITUDE DE CITA PREVIA"];
                u.Station.type = StationType.Fixed_station; 

                string coords = (string)item["COORDENADAS GMAPS"];
                if (!string.IsNullOrEmpty(coords))
                {
                    var coordsParsed = ParseDegreesMinutesCoordinates(coords);
                    if (coordsParsed.HasValue)
                    {
                        u.Station.latitude = coordsParsed.Value.lat;
                        u.Station.longitude = coordsParsed.Value.lon;
                    }
                }

                list.Add(u);
            }
        }

        private static (double lat, double lon)? ParseDegreesMinutesCoordinates(string coords)
        {
            try
            {
                if (!coords.Contains("°"))
                {
                    var parts = coords.Split(',');
                    if (parts.Length == 2)
                    {
                        if (double.TryParse(parts[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) &&
                            double.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
                        {
                            return (lat, lon);
                        }
                    }
                }
                else
                {
                    var pattern = @"(-?\d+)°\s*(\d+\.?\d*)',?\s*(-?\d+)°\s*(\d+\.?\d*)";
                    var match = Regex.Match(coords, pattern);
                    
                    if (match.Success)
                    {
                        double latDegrees = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                        double latMinutes = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                        double lonDegrees = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                        double lonMinutes = double.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);

                        double lat = latDegrees + (latMinutes / 60.0);
                        double lon = lonDegrees + (lonMinutes / 60.0);

                        return (lat, lon);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing coordinates '{coords}': {ex.Message}");
            }
            
            return null;
        }

        public static async Task<(double? lat, double? lon)> GeocodeAddressAsync(string address, string postalCode)
        {
            string query = Uri.EscapeDataString($"{address} {postalCode}");
            string url = $"https://nominatim.openstreetmap.org/search?q={query}&format=json&limit=1";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("IEI-App/1.0");

            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var arr = JArray.Parse(json);
                Console.WriteLine($"Nominatim response: {json}");
                if (arr.Count > 0)
                {
                    double lat = double.Parse(arr[0]["lat"].ToString(), CultureInfo.InvariantCulture);
                    double lon = double.Parse(arr[0]["lon"].ToString(), CultureInfo.InvariantCulture);
                    Console.WriteLine($"Latitude: {lat}, Longitude: {lon}");
                    return (lat, lon);
                }
            }
            return (null, null);
        }
    }
}
