using Aegis.Models;

namespace Aegis.Services.Repositories;

public interface IHistoryRepository
{
    Task<IEnumerable<HistoryItemModel>> GetHistoryAsync(HistoryFilter filter);
    Task<HistoryDetailsModel?> GetHistoryDetailsAsync(int parkingRecordId);
}