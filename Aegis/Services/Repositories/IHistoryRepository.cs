using Aegis.Models;

public interface IHistoryRepository
{
    Task<IEnumerable<HistoryItemModel>> GetHistoryAsync(HistoryFilter filter);
    Task<HistoryDetailsModel?> GetHistoryDetailsAsync(int parkingRecordId);
    Task<IEnumerable<VehiclePhotoModel>> GetVehiclePhotosAsync(int parkingRecordId);
}

