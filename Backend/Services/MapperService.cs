using Backend.Models;
using Backend.Services.Mappers;
using Serilog;

namespace Backend.Services
{
    public class MapperService
    {
        public async Task<List<UnifiedData>> ExecuteMapping(string folderPath)
        {
            Log.Information("Paso Mapper: Iniciando mapeo de datos en la carpeta: {FolderPath}", folderPath);
            var unifiedList = new List<UnifiedData>();
            var files = Directory.GetFiles(folderPath, "*.json");

            foreach (var file in files)
            {
                Log.Information("Paso Mapper: Procesando archivo: {FileName}", file);
                string json = await File.ReadAllTextAsync(file);
                string fileName = Path.GetFileName(file).ToLower();

                IMapper? mapper = null;

                if (fileName.Contains("estaciones")) // CV (Comunidad Valenciana)
                {
                    mapper = new CVMapper();
                }
                else if (fileName.Contains("itv-cat")) // CAT (Cataluña)
                {
                    mapper = new CATMapper();
                }
                else if (fileName.Contains("estacions_itv")) // GAL (Galicia)
                {
                    mapper = new GALMapper();
                }
                else
                {
                    Log.Warning("Formato de archivo desconocido: {FileName}", fileName);
                }

                if (mapper != null)
                {
                    try
                    {
                        mapper.Map(json, unifiedList);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error durante el mapeo del archivo {FileName}", fileName);
                    }
                }
            }

            Log.Information("Mapeo de datos completado. Registros mapeados totales: {RecordCount}", unifiedList.Count);
            return unifiedList;
        }
    }
}
