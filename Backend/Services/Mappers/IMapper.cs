using Backend.Models;

namespace Backend.Services.Mappers
{
    public interface IMapper
    {
        /// <summary>
        /// Transforma datos de estaciones ITV desde un formato específico al modelo unificado
        /// </summary>
        /// <param name="json">Cadena JSON con los datos de estaciones</param>
        /// <param name="validateExistingCoordinates">Si true, valida coordenadas con Selenium</param>
        /// <param name="processCV">Si true, procesa datos de Comunidad Valenciana (según implementación)</param>
        /// <param name="processGAL">Si true, procesa datos de Galicia (según implementación)</param>
        /// <param name="processCAT">Si true, procesa datos de Cataluña (según implementación)</param>
        /// <returns>Objeto MapResult con datos unificados</returns>
        MapResult Map(string json, bool validateExistingCoordinates, bool processCV, bool processGAL, bool processCAT);
    }
}
