using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Serilog;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Backend.Services.Mappers
{
    public static class SeleniumGeocoder
    {
        /// <summary>
        /// Crea y configura una instancia de WebDriver de Chrome para Selenium
        /// </summary>
        /// <returns>Instancia configurada de IWebDriver en modo headless</returns>
        public static IWebDriver CreateDriver()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--lang=es");
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            return new ChromeDriver(options);
        }

        /// <summary>
        /// Obtiene coordenadas geográficas de una dirección usando Google Maps y Selenium
        /// </summary>
        /// <param name="driver">Instancia del WebDriver de Selenium</param>
        /// <param name="address">Dirección física a geocodificar</param>
        /// <param name="cookiesAccepted">Referencia para indicar si se han aceptado las cookies</param>
        /// <param name="postalCode">Código postal para refinar la búsqueda (opcional)</param>
        /// <param name="localityName">Nombre de la localidad para refinar la búsqueda (opcional)</param>
        /// <param name="provinceName">Nombre de la provincia para refinar la búsqueda (opcional)</param>
        /// <returns>Tupla con latitud y longitud en grados decimales, o null si falla</returns>
        public static (double? lat, double? lon) GetCoordinates(IWebDriver driver, string address, ref bool cookiesAccepted, string postalCode = "", string localityName = "", string provinceName = "")
        {
            string fullAddress = $"{address} {postalCode} {localityName} {provinceName} España".Trim();
            if (string.IsNullOrEmpty(fullAddress) || fullAddress == "España") return (null, null);

            try
            {
                string searchUrl = $"https://www.google.com/maps/search/{Uri.EscapeDataString(fullAddress)}";
                driver.Navigate().GoToUrl(searchUrl);

                Log.Information("Servicio Selenium: Buscando coordenadas para '{FullAddress}'...", fullAddress);

                if (!cookiesAccepted)
                {
                    Log.Information("Servicio Selenium: Pausando búsqueda para manejar el consentimiento de cookies...");
                    AcceptCookies(driver);
                    cookiesAccepted = true;
                    Thread.Sleep(1000);
                    Log.Information("Servicio Selenium: Reanudando búsqueda...");
                }

                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait.Until(d => d.Url.Contains("@"));

                string currentUrl = driver.Url;
                var match = Regex.Match(currentUrl, @"@(-?\d+\.\d+),(-?\d+\.\d+)");
                if (match.Success)
                {
                    double lat = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    double lon = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                    Log.Information("Servicio Selenium: Coordenadas encontradas (lat: {Lat}, lon: {Lon}).", lat, lon);
                    return (lat, lon);
                }

                Log.Warning("Servicio Selenium: Coordenadas no encontradas para '{FullAddress}'.", fullAddress);
                return (null, null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Servicio Selenium: Error procesando coordenadas para '{FullAddress}'.", fullAddress);
                return (null, null);
            }
        }

        /// <summary>
        /// Acepta automáticamente las cookies de Google Maps usando Selenium
        /// </summary>
        /// <param name="driver">Instancia del WebDriver de Selenium</param>
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
                        Log.Information("Servicio Selenium: Cookies rechazadas, continuando...");
                        return;
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}