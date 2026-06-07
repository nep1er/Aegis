using Aegis.Models;

namespace Aegis.Services.Repositories;

public interface ISpotRepository
{
    Task<IEnumerable<SpotDisplayModel>> GetFreeSpotsAsync(int parkingId, int vehicleTypeId);
    Task<bool> IsSpotFreeAsync(int spotId);
}