using Aegis.Models;

namespace Aegis.Services.Repositories;

public interface IParkingRepository
{
    Task<IEnumerable<ParkingDisplayModel>> GetParkingsForOperatorAsync(int operatorId);
    Task<IEnumerable<ParkingDisplayModel>> GetAllParkingsAsync();
    Task<IEnumerable<SpotDisplayModel>> GetSpotsForParkingAsync(int parkingId);
}