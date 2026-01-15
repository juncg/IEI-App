using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
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

    public static string RemoveAccents(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();
        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string NormalizeLocalityName(string localityName)
    {
        if (string.IsNullOrWhiteSpace(localityName))
            return localityName;

        string normalized = Regex.Replace(localityName, @"\s*\([^)]*\)", "");
        normalized = normalized.Trim();

        var match = Regex.Match(normalized, @"^(.+),\s*(O|A|Os|As|O'|A')$", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            string baseName = match.Groups[1].Value.Trim();
            string article = match.Groups[2].Value;
            normalized = $"{article} {baseName}";
        }

        return normalized;
    }

    public static (double lat, double lon)? ParseDegreesMinutesCoordinates(string coords)
    {
        var regex = new Regex(@"([+-]?\d+)°\s*([\d\.]+)',?\s*([+-]?\d+)°\s*([\d\.]+)'");
        var match = regex.Match(coords);
        if (!match.Success) return null;

        double latDeg = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        double latMin = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        double lonDeg = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
        double lonMin = double.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);

        double lat = latDeg >= 0
            ? latDeg + (latMin / 60.0)
            : latDeg - (latMin / 60.0);

        double lon = lonDeg >= 0
            ? lonDeg + (lonMin / 60.0)
            : lonDeg - (lonMin / 60.0);

        return (lat, lon);
    }

    // función para calcular la similitud entre cadenas utilizando la distancia de Levenshtein
    // la similitud se mide como un valor entre 0 y 1, donde 1 indica cadenas idénticas
    public static double CalculateSimilarity(string source, string target)
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

    public static string NormalizeProvinceName(string provinceName)
    {
        if (string.IsNullOrWhiteSpace(provinceName))
            return "Desconocida";

        provinceName = provinceName.Trim();

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

    public static string ExtractStationNameWithSimilarity(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return "";

        var prefixes = new List<string>
        {
            "Estación ITV de", "Estación ITV do", "Estación ITV da", "Estación ITV del",
            "Estación ITV dos", "Estación ITV das", "Estación ITV", "Estación de ITV",
            "Estación", "Estacion ITV", "Estacion"
        };

        string bestPrefix = "";
        double bestScore = 0.0;
        foreach (var prefix in prefixes)
        {
            if (rawName.Length >= prefix.Length)
            {
                string start = rawName.Substring(0, prefix.Length);
                double score = CalculateSimilarity(start, prefix);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPrefix = prefix;
                }
            }
        }

        const double threshold = 0.7;
        string name = rawName;
        if (bestScore > threshold)
        {
            name = rawName.Substring(bestPrefix.Length).Trim();
        }

        name = Regex.Replace(name, @"^[\s\-:.,;]+|[\s\-:.,;]+$", "");
        name = Regex.Replace(name, @"^\d+\s*[-:]?\s*", "");
        name = Regex.Replace(name, @"\s{2,}", " ");

        return name.Trim();
    }

    public static string NormalizeAddressGalAndCv(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return "";

        string normalized = address.Trim();

        // expandir abreviaturas
        normalized = Regex.Replace(normalized, @"\bCtra\.?\b", "Carretera", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bAvda\.?\b", "Avenida", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bAv\.?\b", "Avenida", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bPol\. Ind\.?\b", "Polígono Industrial", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bPol\.?\b", "Polígono", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bPl\.?\b", "Plaza", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bC\.?\b", "Calle ", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bC/\b", "Calle ", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bCº\b", "Calle ", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bNº\b", "Número", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bKm\.?\b|\bKM\.?\b|\bP\.K\.?\b", "km", RegexOptions.IgnoreCase);

        // separar parcela con coma
        normalized = Regex.Replace(normalized, @"\bParcela\s+", "Parcela, ", RegexOptions.IgnoreCase);

        // separar número de calle con coma
        normalized = Regex.Replace(
            normalized,
            @"(?<!\bkm\s)([a-záéíóúàèòùìA-ZÁÉÍÓÚÀÈÒÙÌ]+)(?<!\bkm)\s+(\d+(?:[.,]\d+)*)",
            "$1, $2",
            RegexOptions.IgnoreCase
        );

        // s/n siempre a minúscula
        normalized = Regex.Replace(normalized, @"\bS\/N\b", "s/n", RegexOptions.IgnoreCase);

        // remplazar comas y espacios repetidos
        normalized = Regex.Replace(normalized, @",\s*,", ",");
        normalized = Regex.Replace(normalized, @"\s+", " ");
        normalized = Regex.Replace(normalized, @",\s+", ", ");

        // quitar espacios y comas del final
        normalized = normalized.Trim(' ', ',');

        // primera letra en mayúscula
        if (normalized.Length > 0)
            normalized = char.ToUpper(normalized[0]) + normalized.Substring(1);

        return normalized;
    }

    public static string NormalizeAddressCAT(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return "";

        string normalized = address.Trim();

        // expandir abreviaturas
        normalized = Regex.Replace(normalized, @"\bCtra\.?\b", "Carretera", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bAvda\.?\b", "Avinguda", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bAv\.?\b", "Avinguda", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bPl\.?\b", "Plaça", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bPol\. Ind\.?\b", "Polígon Industrial", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bPol\.?\b", "Polígon", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bC\.?\b", "Carrer ", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bC/\b", "Carrer ", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bCº\b", "Carrer ", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bNº\b", "Número", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bKm\.?\b|\bKM\.?\b|\bP\.K\.?\b", "km", RegexOptions.IgnoreCase);

        // separar parcela con coma
        normalized = Regex.Replace(normalized, @"\bParcela\s+", "Parcela, ", RegexOptions.IgnoreCase);

        // separar número de calle con coma
        normalized = Regex.Replace(
            normalized,
            @"(?<!\bkm\s)([a-záéíóúàèòùìA-ZÁÉÍÓÚÀÈÒÙÌ]+)(?<!\bkm)\s+(\d+(?:[.,]\d+)*)",
            "$1, $2",
            RegexOptions.IgnoreCase
        );

        // s/n siempre a minúscula
        normalized = Regex.Replace(normalized, @"\bS\/N\b", "s/n", RegexOptions.IgnoreCase);

        // remplazar comas y espacios repetidos
        normalized = Regex.Replace(normalized, @",\s*,", ",");
        normalized = Regex.Replace(normalized, @"\s+", " ");
        normalized = Regex.Replace(normalized, @",\s+", ", ");

        // quitar espacios y comas del final
        normalized = normalized.Trim(' ', ',');

        // primera letra en mayúscula
        if (normalized.Length > 0)
            normalized = char.ToUpper(normalized[0]) + normalized.Substring(1);

        return normalized;
    }

    public static string ExtractStationSubtype(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return "General";

        var match = Regex.Match(address, @"I\.T\.V\.\s+([A-Za-zÁÉÍÓÚáéíóúñÑ]+)", RegexOptions.IgnoreCase);
        
        if (match.Success)
        {
            string type = match.Groups[1].Value;
            
            return char.ToUpper(type[0]) + type.Substring(1).ToLower();
        }

        string addressLower = address.ToLower();
        
        if (addressLower.Contains("móvil") || addressLower.Contains("movil"))
            return "Móvil";
        if (addressLower.Contains("agrícola") || addressLower.Contains("agricola"))
            return "Agrícola";


        return "General";
    }

    #endregion
}