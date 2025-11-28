using Backend.Models;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Serilog;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Backend.Services.Mappers
{
    public class CVMapper : IMapper
    {
        public void Map(string json, List<UnifiedData> list)
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

                    u.LocalityName = (string?)item["MUNICIPIO"] ?? "Desconocido";

                    string tipo = ((string?)item["TIPO ESTACIÓN"] ?? "").ToLower();
                    Log.Debug("Tipo de estación: {StationType}", tipo);

                    if (tipo.Contains("móvil"))
                        u.Station.type = StationType.Mobile_station;
                    else if (tipo.Contains("fija"))
                        u.Station.type = StationType.Fixed_station;
                    else
                        u.Station.type = StationType.Others;

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

                    if (u.ProvinceName == "Desconocida")
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

                    // Manejo de valores nulos al llamar a GetLatLonSeleniumGoogleMaps
                    var (lat, lon) = u.Station.type == StationType.Fixed_station ? GetLatLonSeleniumGoogleMaps(driver, u.Station.address ?? "", ref cookiesAccepted, u.Station.postal_code ?? "", u.LocalityName ?? "", u.ProvinceName ?? "") : (null, null);

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

        private (double? lat, double? lon) GetLatLonSeleniumGoogleMaps(IWebDriver driver, string address, ref bool cookiesAccepted, string postalCode = "", string localityName = "", string provinceName = "")
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

        private void AcceptCookies(IWebDriver driver)
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
                        Log.Information("1b. Cookies rechazadas");
                        return;
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}
