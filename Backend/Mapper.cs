using Backend.Models;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

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
            var unifiedList = new List<UnifiedData>();
            var files = Directory.GetFiles(folderPath, "*.json");

            foreach (var file in files)
            {
                string json = File.ReadAllText(file);
                string fileName = Path.GetFileName(file).ToLower();

                if (fileName.Contains("estaciones")) // CV (Comunidad Valenciana)
                {
                    MapCV(json, unifiedList);
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

        private static void MapCV(string json, List<UnifiedData> list)
        {
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
                var u = new UnifiedData();
                u.ProvinceName = (string)item["PROVINCIA"] ?? "Desconocida";
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
                    u.Station.name = $"Estación ITV de {direccion}";
                }

                u.Station.address = (string)item["DIRECCIÓN"];
                u.Station.postal_code = (string)item["C.POSTAL"];
                u.Station.contact = (string)item["CORREO"];
                u.Station.schedule = (string)item["HORARIOS"];
                u.Station.url = "https://sitval.com";

                var (lat, lon) = GetLatLonSeleniumGoogleMaps(driver, u.Station.address, ref cookiesAccepted, u.Station.postal_code, u.LocalityName, u.ProvinceName);

                u.Station.latitude = lat;
                u.Station.longitude = lon;

                list.Add(u);
            }
        }

        private static (double? lat, double? lon) GetLatLonSeleniumGoogleMaps(IWebDriver driver, string address, ref bool cookiesAccepted, string postalCode = "", string localityName = "", string provinceName = "")
        {
            string fullAddress = $"{address} {postalCode} {localityName} {provinceName} España".Trim();
            if (string.IsNullOrEmpty(fullAddress) || fullAddress == "España") return (null, null);

            try
            {
                string searchUrl = $"https://www.google.com/maps/search/{Uri.EscapeDataString(fullAddress)}";
                driver.Navigate().GoToUrl(searchUrl);

                Console.WriteLine($"1. Buscando coordenadas de: {fullAddress}");

                if (!cookiesAccepted)
                {
                    Console.WriteLine("1a. Pausando busqueda para responder al consentimiento de cookies.");
                    AcceptCookies(driver);
                    cookiesAccepted = true;
                    Thread.Sleep(1000);
                    Console.WriteLine("1c. Reanudando busqueda.");
                }

                // wait until coords are loaded
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait.Until(d => d.Url.Contains("@"));

                string currentUrl = driver.Url;
                var match = Regex.Match(currentUrl, @"@(-?\d+\.\d+),(-?\d+\.\d+)");
                if (match.Success)
                {
                    double lat = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    double lon = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                    Console.WriteLine($"2. Latitud encontrada: {lat}. Longitud encontrada: {lon}.\n");
                    return (lat, lon);
                }

                Console.WriteLine($"!!! Coordenadas no encontradas para: {fullAddress}\n");
                return (null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"!!! ERROR procesando coordenadas para '{fullAddress}': {ex.Message}\n");
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

        /*
        private static (double? lat, double? lon) GetLatLonWithSeleniumInstance(
            IWebDriver driver, string address, string postalCode = "", string localityName = "", string provinceName = "", string oldLatLong = "", int attempt = 1)
        {
            string fullQuery = $"{address} {postalCode} {localityName} {provinceName} España";

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));

            try
            {
                var searchBox = wait.Until(d => d.FindElement(By.Id("address")));
                searchBox.Clear();
                searchBox.SendKeys(fullQuery);
                Console.WriteLine($"Query introducida en gps-coordinates.net: {fullQuery}");

                var getAddressButton = wait.Until(d => d.FindElement(By.CssSelector("button[onclick='codeAddress()']")));
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", getAddressButton);


                Console.WriteLine($"Intento de obtener LatLong: {attempt}");
                Thread.Sleep(1000);
                string latlong = driver.FindElement(By.Id("latlong")).GetAttribute("value");
                if ((latlong == oldLatLong) && (attempt < 5))
                {
                    Thread.Sleep(1000);
                    GetLatLonWithSeleniumInstance(driver, address, postalCode, localityName, provinceName, oldLatLong, attempt + 1);
                }

                oldLatLong = latlong;
                
                var parts = latlong.Split(',');
                double? latitude = double.Parse(parts[0]);
                double? longitude = double.Parse(parts[1]);

                Console.WriteLine($"Latitud encontrada: {latitude}. Longitud encontrada: {longitude}.");

                return (latitude, longitude);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return (null, null);
            }
        }

        private static void PrepareSiteForSelenium(IWebDriver driver, int attempt = 1)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));

            try
            {
                Console.WriteLine($"Intento de preparar web para Selenium: {attempt + 1}");

                var consentButton = driver.FindElement(By.CssSelector("button.fc-cta-consent"));
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", consentButton);

                var preSearch = wait.Until(d => d.FindElement(By.Id("address")));
                Thread.Sleep(1000);
                preSearch.SendKeys("a");
                preSearch.Clear();
                Console.WriteLine("Dado consentimiento a gps-coordinates.net.");
            }
            catch (Exception)
            {
                if (attempt < 5)
                {
                    Thread.Sleep(1000);
                    PrepareSiteForSelenium(driver, attempt + 1);
                }
            }
        }
        */

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
    }
}
