using Backend.Models;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using Serilog;

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
        //static string oldLatLong = "";

        public static async Task<List<UnifiedData>> ExecuteMapping(string folderPath)
        {
            Log.Information("Empezando mapeo de datos: {FolderPath}", folderPath);
            var unifiedList = new List<UnifiedData>();
            var files = Directory.GetFiles(folderPath, "*.json");

            foreach (var file in files)
            {
                Log.Information("Procesando archivo: {FileName}", file);
                string json = File.ReadAllText(file);
                string fileName = Path.GetFileName(file).ToLower();

                if (fileName.Contains("estaciones")) // CV (Comunidad Valenciana)
                {
                    Log.Information("Mapeando datos para la Comunidad Valenciana.");
                    MapCV(json, unifiedList);
                    Log.Information("Finalizado el mapeo de datos para la Comunidad Valenciana.");
                }
                else if (fileName.Contains("itv-cat")) // CAT (Cataluña)
                {
                    Log.Information("Mapeando datos para Cataluña.");
                    MapCAT(json, unifiedList);
                    Log.Information("Finalizado el mapeo de datos para Cataluña.");
                }
                else if (fileName.Contains("estacions_itv")) // GAL (Galicia)
                {
                    Log.Information("Mapeando datos para Galicia.");
                    MapGAL(json, unifiedList);
                    Log.Information("Finalizado el mapeo de datos para Galicia.");
                }
                else
                {
                    Log.Warning("Formato de archivo desconocido: {FileName}", fileName);
                }
            }

            Log.Information("Mapeo de datos completado. Registros mapeados totales: {RecordCount}", unifiedList.Count);
            return unifiedList;
        }

        private static void MapCV(string json, List<UnifiedData> list)
        {
            Log.Information("Empezando el mapeo para la Comunidad Valenciana.");
            var data = JArray.Parse(json);

            // options for consistency / to trick google into thinking we are a normal user
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--lang=es");
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            using var driver = new ChromeDriver(options);
            bool cookiesAccepted = false;

            foreach (var item in data)
            {
                try
                {
                    var u = new UnifiedData();
                    u.ProvinceName = Utilities.NormalizeProvinceName((string?)item["PROVINCIA"] ?? "Desconocida");
                    u.LocalityName = (string?)item["MUNICIPIO"] ?? "Desconocido";

                    string tipo = ((string?)item["TIPO ESTACIÓN"] ?? "").ToLower();
                    Log.Debug("Tipo de estación: {StationType}", tipo);

                    // Asignar tipo de estación
                    if (tipo.Contains("móvil"))
                        u.Station.type = StationType.Mobile_station;
                    else if (tipo.Contains("fija"))
                        u.Station.type = StationType.Fixed_station;
                    else
                        u.Station.type = StationType.Others;

                    u.Station.name = u.Station.type == StationType.Fixed_station
                        ? $"Estación ITV de {u.LocalityName}"
                        : $"Estación {(string?)item["DIRECCIÓN"] ?? u.LocalityName}";

                    u.Station.address = (string?)item["DIRECCIÓN"] ?? "";
                    u.Station.postal_code = (string?)item["C.POSTAL"] ?? "";
                    u.Station.contact = (string?)item["CORREO"] ?? "";
                    u.Station.schedule = (string?)item["HORARIOS"] ?? "";
                    u.Station.url = "https://sitval.com";

                    Log.Debug("Detalles de la estación: {@Station}", u.Station);

                    // Manejo de valores nulos al llamar a GetLatLonSeleniumGoogleMaps
                    var (lat, lon) = GetLatLonSeleniumGoogleMaps(driver, u.Station.address ?? "", ref cookiesAccepted, u.Station.postal_code ?? "", u.LocalityName ?? "", u.ProvinceName ?? "");

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

        private static (double? lat, double? lon) GetLatLonSeleniumGoogleMaps(IWebDriver driver, string address, ref bool cookiesAccepted, string postalCode = "", string localityName = "", string provinceName = "")
        {
            string fullAddress = $"{address} {postalCode} {localityName} {provinceName} España".Trim();
            if (string.IsNullOrEmpty(fullAddress) || fullAddress == "España") return (null, null);

            try
            {
                string searchUrl = $"https://www.google.com/maps/search/{Uri.EscapeDataString(fullAddress)}";
                driver.Navigate().GoToUrl(searchUrl);

                Log.Information("1. Buscando coordenadas para: {FullAddress}", fullAddress);

                if (!cookiesAccepted)
                {
                    Log.Information("1a. Pausando búsqueda para manejar el consentimiento de cookies.");
                    AcceptCookies(driver);
                    cookiesAccepted = true;
                    Thread.Sleep(1000);
                    Log.Information("1c. Reanudando búsqueda.");
                }

                // wait until coords are loaded
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait.Until(d => d.Url.Contains("@"));

                string currentUrl = driver.Url;
                // Corrección de la expresión regular con secuencias de escape válidas
                var match = Regex.Match(currentUrl, "@(-?\\d+\\.\\d+),(-?\\d+\\.\\d+)");
                if (match.Success)
                {
                    double lat = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    double lon = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                    Log.Information("2. Latitud encontrada: {Lat}. Longitud encontrada: {Lon}.", lat, lon);
                    return (lat, lon);
                }

                Log.Warning("!!! Coordenadas no encontradas para: {FullAddress}", fullAddress);
                return (null, null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "!!! ERROR procesando coordenadas para '{FullAddress}'", fullAddress);
                return (null, null);
            }
        }

        private static void AcceptCookies(IWebDriver driver)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));

                string[] selectors = {
                    "//button[contains(., 'Rechazar todo')]",
                    "//button[contains(., 'Rechazar')]",
                    "button[aria-label='Rechazar todo']",
                };

                foreach (var selector in selectors)
                {
                    try
                    {
                        IWebElement button = selector.StartsWith("//")
                            ? wait.Until(d => d.FindElement(By.XPath(selector)))
                            : wait.Until(d => d.FindElement(By.CssSelector(selector)));
                        button.Click();
                        Console.WriteLine("1b. Cookies rechazadas");
                        return;
                    }
                    catch { }
                }
            }
            catch { }
        }



        private static void MapCAT(string json, List<UnifiedData> list)
        {
            var root = JObject.Parse(json);
            var rows = root["response"]?["row"]?["row"];
            if (rows == null) return;

            foreach (var item in rows)
            {
                var u = new UnifiedData();
                u.ProvinceName = Utilities.NormalizeProvinceName((string?)item["serveis_territorials"] ?? "Barcelona"); // Default o mapeo específico
                u.LocalityName = (string?)item["municipi"] ?? "Desconocido";

                string nombre = (string?)item["denominaci"] ?? u.LocalityName;
                u.Station.name = $"Estación ITV de {nombre}";

                u.Station.address = (string?)item["adre_a"];

                string cp = (string?)item["cp"] ?? "";
                u.Station.postal_code = cp.Length >= 5 ? cp.Substring(0, 5) : cp;

                string contact = (string?)item["correu_electr_nic"] ?? "";
                if (!string.IsNullOrWhiteSpace(contact) && !Utilities.IsUrl(contact))
                {
                    u.Station.contact = contact;
                }

                u.Station.schedule = (string?)item["horari_de_servei"] ?? "";
                u.Station.url = (string?)item["web"]?["@url"] ?? "";
                u.Station.type = StationType.Fixed_station;

                if (double.TryParse((string?)item["lat"], NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
                    u.Station.latitude = lat / 100000.0;

                if (double.TryParse((string?)item["long"], NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
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
                u.ProvinceName = Utilities.NormalizeProvinceName((string?)item["PROVINCIA"] ?? "Desconocida");
                u.LocalityName = (string?)item["CONCELLO"] ?? "Desconocido";

                string nombre = (string?)item["NOME DA ESTACIÓN"] ?? u.LocalityName;
                u.Station.name = nombre;

                u.Station.address = (string?)item["ENDEREZO"] ?? "";
                u.Station.postal_code = (string?)item["CÓDIGO POSTAL"] ?? "";

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
        }
    }
}
