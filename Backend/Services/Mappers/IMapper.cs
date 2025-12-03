using Backend.Models;

namespace Backend.Services.Mappers
{
    public interface IMapper
    {
        void Map(string json, List<UnifiedData> list, bool validateExistingCoordinates, bool processCV, bool processGAL, bool processCAT);
    }
}
