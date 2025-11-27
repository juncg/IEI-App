using System.Text.RegularExpressions;
using System.Globalization;



public class Utilities
{
        public static (double lat, double lon)? ParseDegreesMinutesCoordinates(string coords)
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

        public static string CleanAddress(string address)
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

        public static string NormalizeProvinceName(string provinceName)
        {
            if (string.IsNullOrWhiteSpace(provinceName))
                return "Desconocida";

            provinceName = provinceName.Trim();

            // Lista de provincias conocidas
            var provinces = new List<string>
            {
                "A Coruña", "Álava", "Albacete", "Alicante", "Almería", "Asturias", "Ávila",
                "Badajoz", "Barcelona", "Burgos", "Cáceres", "Cádiz", "Cantabria", "Castellón",
                "Ciudad Real", "Córdoba", "Cuenca", "Girona", "Granada", "Guadalajara", "Guipúzcoa",
                "Huelva", "Huesca", "Jaén", "La Rioja", "Las Palmas", "León", "Lleida", "Lugo",
                "Madrid", "Málaga", "Murcia", "Navarra", "Ourense", "Palencia", "Pontevedra",
                "Salamanca", "Santa Cruz de Tenerife", "Segovia", "Sevilla", "Soria", "Tarragona",
                "Teruel", "Toledo", "Valencia", "Valladolid", "Vizcaya", "Zamora", "Zaragoza"
            };

            // Función para calcular la similitud entre cadenas
            // Calcula la similitud entre dos cadenas utilizando la distancia de Levenshtein.
            // La similitud se mide como un valor entre 0 y 1, donde 1 indica cadenas idénticas.
            // source: La primera cadena a comparar.
            // target: La segunda cadena a comparar.
            double CalculateSimilarity(string source, string target)
            {
                source = source.ToLower();
                target = target.ToLower();

                // Crear una matriz para almacenar las distancias entre subcadenas.
                int[,] dp = new int[source.Length + 1, target.Length + 1];

                // Inicializar la primera fila y columna de la matriz.
                for (int i = 0; i <= source.Length; i++)
                    dp[i, 0] = i; // Costo de eliminar todos los caracteres de "source".
                for (int j = 0; j <= target.Length; j++)
                    dp[0, j] = j; // Costo de insertar todos los caracteres de "target".

                // Rellenar la matriz utilizando la distancia de Levenshtein.
                for (int i = 1; i <= source.Length; i++)
                {
                    for (int j = 1; j <= target.Length; j++)
                    {
                        // Determinar el costo de sustitución (0 si los caracteres son iguales, 1 si son diferentes).
                        int cost = (source[i - 1] == target[j - 1]) ? 0 : 1;

                        // Calcular el costo mínimo entre eliminar, insertar o sustituir un carácter.
                        dp[i, j] = Math.Min(
                            Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1), // Eliminar o insertar.
                            dp[i - 1, j - 1] + cost // Sustituir.
                        );
                    }
                }

                // La distancia de Levenshtein es el valor en la esquina inferior derecha de la matriz.
                int levenshteinDistance = dp[source.Length, target.Length];

                // Calcular la similitud como 1 menos la distancia normalizada por la longitud máxima de las cadenas.
                return 1.0 - (double)levenshteinDistance / Math.Max(source.Length, target.Length);
            }

            // Buscar la provincia más similar
            string bestMatch = "Desconocida";
            double highestSimilarity = 0.0;

            foreach (var province in provinces)
            {
                double similarity = CalculateSimilarity(provinceName, province);
                if (similarity > highestSimilarity)
                {
                    highestSimilarity = similarity;
                    bestMatch = province;
                }
            }

            // Si la similitud más alta es menor a un umbral, considerar que no coincide con ninguna provincia.
            const double similarityThreshold = 0.5; // Umbral mínimo de similitud
            if (highestSimilarity < similarityThreshold)
            {
                return "Desconocida";
            }

            return bestMatch;
        }

        
        public static bool IsUrl(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                   text.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ||
                   Regex.IsMatch(text, @"^[a-zA-Z0-9-]+\.[a-zA-Z]{2,}");
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
}