using Backend.Models;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
        public static async Task<List<UnifiedData>> ExecuteMapping(string folderPath)
        {
            var unifiedList = new List<UnifiedData>();
            var files = Directory.GetFiles(folderPath, "*.json");

            foreach (var file in files)
            {
                string json = File.ReadAllText(file);
                string fileName = Path.GetFileName(file).ToLower();

                if (fileName.Contains("estaciones")) // CV (Comunidad Valenciana)
                {
                    await MapCV(json, unifiedList);
                    Console.WriteLine($"Finished CV");
                }
                else if (fileName.Contains("itv-cat")) // CAT (Cataluña)
                {
                    MapCAT(json, unifiedList);
                    Console.WriteLine($"Finished CAT");
                }
                else if (fileName.Contains("estacions_itv")) // GAL (Galicia)
                {
                    MapGAL(json, unifiedList);
                    Console.WriteLine($"Finished GAL");
                }
            }

            Console.WriteLine($"Mapping finished. Total records: {unifiedList.Count}");
            return unifiedList;
        }

        private static async Task MapCV(string json, List<UnifiedData> list)
        {
            var data = JArray.Parse(json);
            foreach (var item in data)
            {
                var u = new UnifiedData();
                u.ProvinceName = NormalizeProvinceName((string)item["PROVINCIA"] ?? "Desconocida");
                u.LocalityName = (string)item["MUNICIPIO"] ?? "Desconocido";

                string tipo = ((string)item["TIPO ESTACIÓN"] ?? "").ToLower();
                if (tipo.Contains("móvil")) u.Station.type = StationType.Mobile_station;
                else if (tipo.Contains("fija")) u.Station.type = StationType.Fixed_station;
                else u.Station.type = StationType.Others;

                if (u.Station.type == StationType.Fixed_station)
                {
                    u.Station.name = $"Estación ITV de {u.LocalityName}";
                }
                else
                {
                    string direccion = (string)item["DIRECCIÓN"] ?? u.LocalityName;
                    direccion = Regex.Replace(direccion, @"\.+", "");
                    u.Station.name = $"Estación {direccion}";
                }

                u.Station.address = (string)item["DIRECCIÓN"];
                u.Station.postal_code = (string)item["C.POSTAL"];
                u.Station.contact = (string)item["CORREO"];
                u.Station.schedule = (string)item["HORARIOS"];
                u.Station.url = "https://sitval.com";

                var (lat, lon) = await GeocodeAddressWithPhotonAsync(
                    u.Station.address,
                    u.Station.postal_code,
                    u.LocalityName,
                    u.ProvinceName);
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
                u.ProvinceName = NormalizeProvinceName((string)item["serveis_territorials"] ?? "Barcelona"); // Default o mapeo específico
                u.LocalityName = (string)item["municipi"] ?? "Desconocido";

                string nombre = (string)item["denominaci"] ?? u.LocalityName;
                u.Station.name = $"Estación ITV de {nombre}";

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
                u.ProvinceName = NormalizeProvinceName((string)item["PROVINCIA"] ?? "Desconocida");
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

        // returns lots of empty responses
        public static async Task<(double? lat, double? lon)> GeocodeAddressWithNominatimAsync(string address, string postalCode)
        {
            string query = Uri.EscapeDataString($"{address} {postalCode}");
            string url = $"https://nominatim.openstreetmap.org/search?q={query}&format=json&limit=1";

            int maxAttempts = 5;
            int delayMs = 1000;

            await Task.Delay(1000); // nominatim has a 1 request per second limit

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("IEI-App/1.0");

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var arr = JArray.Parse(json);
                    Console.WriteLine($"Nominatim response (attempt {attempt}): {json}");
                    if (arr.Count > 0)
                    {
                        double lat = double.Parse(arr[0]["lat"].ToString(), CultureInfo.InvariantCulture);
                        double lon = double.Parse(arr[0]["lon"].ToString(), CultureInfo.InvariantCulture);
                        Console.WriteLine($"Latitude: {lat}, Longitude: {lon}");
                        return (lat, lon);
                    }
                }
                if (attempt < maxAttempts)
                {
                    await Task.Delay(delayMs);
                }
            }
            Console.WriteLine($"Nominatim geocoding failed for '{address} {postalCode}' after {maxAttempts} attempts.");
            return (null, null);
        }

        // faster and returns less empty responses
        public static async Task<(double? lat, double? lon)> GeocodeAddressWithPhotonAsync(
            string address, string postalCode, string localityName = "", string provinceName = "")
        {
            string query = Uri.EscapeDataString($"{address} {postalCode} {localityName} {provinceName} España");
            string url = $"https://photon.komoot.io/api/?q={query}&limit=1";

            int maxAttempts = 5;
            int delayMs = 1000;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("IEI-App/1.0");

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var obj = JObject.Parse(json);
                    Console.WriteLine($"Photon response (attempt {attempt}): {json}");
                    var features = obj["features"];
                    if (features != null && features.Any())
                    {
                        var coords = features[0]?["geometry"]?["coordinates"];
                        if (coords != null && coords.Count() == 2)
                        {
                            double lon = coords[0].Value<double>();
                            double lat = coords[1].Value<double>();
                            Console.WriteLine($"Latitude: {lat}, Longitude: {lon}");
                            return (lat, lon);
                        }
                    }
                }
                if (attempt < maxAttempts)
                {
                    await Task.Delay(delayMs);
                }
            }
            Console.WriteLine($"Photon geocoding failed for '{address} {postalCode}' after {maxAttempts} attempts.");
            return (null, null);
        }

        private static string CleanAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return "";

            // common abbreviations
            address = Regex.Replace(address, @"\bCtra\.?\b", "Carretera", RegexOptions.IgnoreCase);
            address = Regex.Replace(address, @"\bAvda\.?\b", "Avenida", RegexOptions.IgnoreCase);
            address = Regex.Replace(address, @"\bPol\. Ind\.?\b", "Polígono Industrial", RegexOptions.IgnoreCase);
            address = Regex.Replace(address, @"\bNº\b", "Número", RegexOptions.IgnoreCase);
            address = Regex.Replace(address, @"\bKm\.?\b", "Kilómetro", RegexOptions.IgnoreCase);
            address = Regex.Replace(address, @"\bC/\b", "Calle ", RegexOptions.IgnoreCase);
            address = Regex.Replace(address, @"\bs/n\b", "", RegexOptions.IgnoreCase);
            address = Regex.Replace(address, @"\bPlá\b", "Pla", RegexOptions.IgnoreCase);

            // replace periods with commas, unless part of decimal numbers
            address = Regex.Replace(address, @"(?<!\d)\.(?!\d)", ",");

            // add space after comma if missing
            address = Regex.Replace(address, @",(\S)", ", $1");

            // remove duplicate spaces
            address = Regex.Replace(address, @"\s+", " ");

            // trim trailing spaces and commas
            address = address.Trim(' ', ',');

            return address;
        }

        private static string NormalizeProvinceName(string provinceName)
        {
            if (string.IsNullOrWhiteSpace(provinceName))
                return "Desconocida";

            provinceName = provinceName.Trim();

            if (provinceName.Equals("Castellón", StringComparison.OrdinalIgnoreCase) ||
                provinceName.Equals("Castelló", StringComparison.OrdinalIgnoreCase))
                return "Castellón";

            if (provinceName.Equals("Valencia", StringComparison.OrdinalIgnoreCase) ||
                provinceName.Equals("València", StringComparison.OrdinalIgnoreCase))
                return "Valencia";

            if (provinceName.Equals("Alicante", StringComparison.OrdinalIgnoreCase) ||
                provinceName.Equals("Aligante", StringComparison.OrdinalIgnoreCase) ||
                provinceName.Equals("Alacant", StringComparison.OrdinalIgnoreCase))
                return "Alicante";

            if (provinceName.Equals("A Coruña", StringComparison.OrdinalIgnoreCase) ||
                provinceName.Equals("CoruÃ±a", StringComparison.OrdinalIgnoreCase) ||
                provinceName.Equals("Coruña", StringComparison.OrdinalIgnoreCase))
                return "A Coruña";

            if (provinceName.Equals("Girona", StringComparison.OrdinalIgnoreCase) ||
                provinceName.Equals("Gerona", StringComparison.OrdinalIgnoreCase))
                return "Girona";

            return provinceName;
        }
    }
}
