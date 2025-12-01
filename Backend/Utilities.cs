using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;
using OpenQA.Selenium;
using Backend.Services.Mappers;



public class Utilities
{
    #region Constants
    private static readonly List<string> provinces = new List<string>
        {
            "Álava", "Albacete", "Alicante", "Almería", "Asturias", "Ávila", "Badajoz", "Barcelona",
            "Burgos", "Cáceres", "Cádiz", "Cantabria", "Castellón", "Ciudad Real", "Córdoba",
            "Cuenca", "Girona", "Granada", "Guadalajara", "Guipúzcoa", "Huelva", "Huesca",
            "Islas Baleares", "Jaén", "La Coruña", "La Rioja", "Las Palmas", "León", "Lleida",
            "Lugo", "Madrid", "Málaga", "Murcia", "Navarra", "Ourense", "Palencia", "Pontevedra",
            "Salamanca", "Santa Cruz de Tenerife", "Segovia", "Sevilla", "Soria", "Tarragona",
            "Teruel", "Toledo", "Valencia", "Valladolid", "Vizcaya", "Zamora", "Zaragoza"
        };

    private static readonly Dictionary<string, (string Provincia, string Comunidad)> MapeoProvincias = new Dictionary<string, (string, string)>
        {
            {"03", ("Alicante", "Comunidad Valenciana")},
            {"12", ("Castellón", "Comunidad Valenciana")},
            {"46", ("Valencia", "Comunidad Valenciana")},

            {"08", ("Barcelona", "Cataluña")},
            {"17", ("Girona", "Cataluña")},
            {"25", ("Lleida", "Cataluña")},
            {"43", ("Tarragona", "Cataluña")},

            {"15", ("A Coruña", "Galicia")},
            {"27", ("Lugo", "Galicia")},
            {"32", ("Ourense", "Galicia")},
            {"36", ("Pontevedra", "Galicia")}
        };

    #endregion
    #region Public Methods
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
            Console.WriteLine($"Error al analizar las coordenadas '{coords}': {ex.Message}");
        }

        return null;
    }

    public static string CleanAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return "";

        address = Regex.Replace(address, @"\bCtra\.?\b", "Carretera", RegexOptions.IgnoreCase);
        address = Regex.Replace(address, @"\bAvda\.?\b", "Avenida", RegexOptions.IgnoreCase);
        address = Regex.Replace(address, @"\bPol\. Ind\.?\b", "Polígono Industrial", RegexOptions.IgnoreCase);
        address = Regex.Replace(address, @"\bNº\b", "Número", RegexOptions.IgnoreCase);
        address = Regex.Replace(address, @"\bKm\.?\b", "Kilómetro", RegexOptions.IgnoreCase);
        address = Regex.Replace(address, @"\bC/\b", "Calle ", RegexOptions.IgnoreCase);
        address = Regex.Replace(address, @"\bs/n\b", "", RegexOptions.IgnoreCase);
        address = Regex.Replace(address, @"\bPlá\b", "Pla", RegexOptions.IgnoreCase);

        address = Regex.Replace(address, @"(?<!\d)\.(?!\d)", ",");

        address = Regex.Replace(address, @",(\S)", ", $1");

        address = Regex.Replace(address, @"\s+", " ");

        address = address.Trim(' ', ',');

        return address;
    }

    public static string NormalizeProvinceName(string provinceName)
    {
        if (string.IsNullOrWhiteSpace(provinceName))
            return "Desconocida";

        provinceName = provinceName.Trim();

        // Función para calcular la similitud entre cadenas utilizando la distancia de Levenshtein
        // La similitud se mide como un valor entre 0 y 1, donde 1 indica cadenas idénticas
        double CalculateSimilarity(string source, string target)
        {
            source = source.ToLower();
            target = target.ToLower();

            int[,] dp = new int[source.Length + 1, target.Length + 1];

            for (int i = 0; i <= source.Length; i++)
                dp[i, 0] = i;
            for (int j = 0; j <= target.Length; j++)
                dp[0, j] = j;

            for (int i = 1; i <= source.Length; i++)
            {
                for (int j = 1; j <= target.Length; j++)
                {
                    int cost = (source[i - 1] == target[j - 1]) ? 0 : 1;

                    dp[i, j] = Math.Min(
                        Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                        dp[i - 1, j - 1] + cost
                    );
                }
            }

            int levenshteinDistance = dp[source.Length, target.Length];

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

        // Umbral mínimo de similitud, si no se cumple, se considera que no coincide con ninguna provincia
        const double similarityThreshold = 0.5;

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

    public static bool IsValidPostalCodeForCommunity(string postalCode, string comunidad)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
            return false;

        if (postalCode.Length != 5 || !postalCode.All(char.IsDigit))
            return false;

        string codigoProvincia = postalCode.Substring(0, 2);

        if (MapeoProvincias.TryGetValue(codigoProvincia, out var info))
        {
            return info.Comunidad.Equals(comunidad, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    public static string? GetProvinceFromPostalCode(string postalCode)
    {
        if (string.IsNullOrWhiteSpace(postalCode) || postalCode.Length < 2)
            return null;

        string codigoProvincia = postalCode.Substring(0, 2);

        if (MapeoProvincias.TryGetValue(codigoProvincia, out var info))
        {
            return info.Provincia;
        }

        return null;
    }

    public static bool AreValidCoordinates(double? latitude, double? longitude)
    {
        if (!latitude.HasValue || !longitude.HasValue)
            return false;

        if (latitude.Value < -90 || latitude.Value > 90)
            return false;

        if (longitude.Value < -180 || longitude.Value > 180)
            return false;

        if (Math.Abs(latitude.Value) < 0.0001 && Math.Abs(longitude.Value) < 0.0001)
            return false;

        if (Math.Abs(latitude.Value) >= 200 || Math.Abs(longitude.Value) >= 200)
            return false;

        return true;
    }

    public static bool CompareAddressWithCoordinates(IWebDriver driver, string address, double? lat, double? lon, ref bool cookiesAccepted, string postalCode = "", string localityName = "", string provinceName = "")
    {
        var (newLat, newLon) = SeleniumGeocoder.GetCoordinates(
            driver,
            address ?? "",
            ref cookiesAccepted,
            postalCode ?? "",
            localityName ?? "",
            provinceName ?? ""
        );

        // formula de haversine
        double DistanceInKm(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371;
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        return DistanceInKm(lat ?? -9999, lon ?? -9999, newLat ?? 9999, newLon ?? 9999) < 10; // menos de 10 km
    }

    #endregion
}