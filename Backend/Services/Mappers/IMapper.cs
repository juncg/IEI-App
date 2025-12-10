using Backend.Models;

namespace Backend.Services.Mappers
{
    public interface IMapper
    {
        MapResult Map(string json, bool validateExistingCoordinates, bool processCV, bool processGAL, bool processCAT);
    }
}
