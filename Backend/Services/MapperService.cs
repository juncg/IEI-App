using Backend.Models;
using Backend.Services.Mappers;
using Serilog;

namespace Backend.Services
{
    public class MapperService
    {
        public async Task<MapResult> ExecuteMapping(string folderPath, bool validateExistingCoordinates, bool processCV, bool processGAL, bool processCAT)
        {
            Log.Information("Paso Mapper: Iniciando mapeo de datos en la carpeta: {FolderPath}", folderPath);
            var result = new MapResult();
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
                    Log.Information("");
                    Log.Information("------------------------------------------------");
                    try
                    {
                        var mapResult = mapper.Map(json, validateExistingCoordinates, processCV, processGAL, processCAT);
                        result.UnifiedData.AddRange(mapResult.UnifiedData);
                        result.RepairedRecords.AddRange(mapResult.RepairedRecords);
                        result.DiscardedRecords.AddRange(mapResult.DiscardedRecords);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error durante el mapeo del archivo {FileName}", fileName);
                    }
                    Log.Information("------------------------------------------------");
                    Log.Information("");
                }
            }

            Log.Information("Mapeo de datos completado. Registros mapeados totales: {RecordCount}", result.UnifiedData.Count);
            return result;
        }
    }
}
