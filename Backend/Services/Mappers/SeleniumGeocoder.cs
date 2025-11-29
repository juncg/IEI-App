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

        public static (double? lat, double? lon) GetCoordinates(IWebDriver driver, string address, ref bool cookiesAccepted, string postalCode = "", string localityName = "", string provinceName = "")
        {
            string fullAddress = $"{address} {postalCode} {localityName} {provinceName} España".Trim();
            if (string.IsNullOrEmpty(fullAddress) || fullAddress == "España") return (null, null);

            try
            {
                string searchUrl = $"https://www.google.com/maps/search/{Uri.EscapeDataString(fullAddress)}";
                driver.Navigate().GoToUrl(searchUrl);

                Log.Information("Buscando coordenadas para: {FullAddress}", fullAddress);

                if (!cookiesAccepted)
                {
                    Log.Information("Pausando búsqueda para manejar el consentimiento de cookies.");
                    AcceptCookies(driver);
                    cookiesAccepted = true;
                    Thread.Sleep(1000);
                    Log.Information("Reanudando búsqueda.");
                }

                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait.Until(d => d.Url.Contains("@"));

                string currentUrl = driver.Url;
                var match = Regex.Match(currentUrl, @"@(-?\d+\.\d+),(-?\d+\.\d+)");
                if (match.Success)
                {
                    double lat = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    double lon = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                    Log.Information("Coordenadas encontradas: Lat={Lat}, Lon={Lon}", lat, lon);
                    return (lat, lon);
                }

                Log.Warning("Coordenadas no encontradas para: {FullAddress}", fullAddress);
                return (null, null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error procesando coordenadas para '{FullAddress}'", fullAddress);
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
                        Log.Information("Cookies rechazadas");
                        return;
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}