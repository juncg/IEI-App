using Backend.Models;
using Newtonsoft.Json.Linq;
using System.Globalization;

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

        private static void MapCV(string json, List<UnifiedData> list)
        {
            var data = JArray.Parse(json);
            foreach (var item in data)
            {
                var u = new UnifiedData();
                u.ProvinceName = (string)item["PROVINCIA"] ?? "Desconocida";
                u.LocalityName = (string)item["MUNICIPIO"] ?? "Desconocido";

                u.Station.name = $"Estación ITV de {u.LocalityName} {(string)item["Nº ESTACIÓN"]}";
                u.Station.address = (string)item["DIRECCIÓN"];
                u.Station.postal_code = (string)item["C.POSTAL"];
                u.Station.contact = (string)item["CORREO"];
                u.Station.schedule = (string)item["HORARIOS"];
                u.Station.url = "https://sitval.com";
                
                // Mapeo de tipo
                string tipo = ((string)item["TIPO ESTACIÓN"] ?? "").ToLower();
                if (tipo.Contains("movil")) u.Station.type = StationType.Mobile_station;
                else if (tipo.Contains("fija")) u.Station.type = StationType.Fixed_station;
                else u.Station.type = StationType.Others;

                
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
                u.Station.name = $"Estación ITV de {nombre} {codigo}";
                
                u.Station.address = (string)item["adre_a"];
                u.Station.postal_code = (string)item["cp"];
                u.Station.contact = (string)item["correu_electr_nic"];
                u.Station.schedule = (string)item["horari_de_servei"];
                u.Station.url = (string)item["web"]?["@url"];
                u.Station.type = StationType.Fixed_station; // Todas fijas según mapping

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
                u.Station.name = $"Estación ITV de {nombre}";
                
                u.Station.address = (string)item["ENDEREZO"];
                u.Station.postal_code = (string)item["CÓDIGO POSTAL"];
                u.Station.contact = $"{item["TELEFONO"]} {item["EMAIL"]}";
                u.Station.schedule = (string)item["HORARIO"];
                u.Station.url = (string)item["SOLICITUD CITA PREVIA"];
                u.Station.type = StationType.Fixed_station; // Todas fijas según mapping

                string coords = (string)item["COORDENADAS_GOOGLE_MAPS"];
                if (!string.IsNullOrEmpty(coords))
                {
                    var parts = coords.Split(',');
                    if (parts.Length == 2)
                    {
                        if (double.TryParse(parts[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
                            u.Station.latitude = lat;
                        if (double.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
                            u.Station.longitude = lon;
                    }
                }

                list.Add(u);
            }
        }
    }
}