using Aegis.Models;

namespace Aegis.Services.Repositories;

public interface IAnalyticsRepository
{
    Task<IEnumerable<MonthlyParkingRevenue>> GetMonthlyRevenueByParkingIdAsync(int? parkingId, DateTime? dateFrom, DateTime? dateTo);
    Task<IEnumerable<ParkingRevenue>> GetRevenueByParkingAsync(DateTime? dateFrom, DateTime? dateTo);
    Task<IEnumerable<VehicleTypeStatistics>> GetVehicleTypeStatisticsAsync(DateTime? dateFrom, DateTime? dateTo);
    Task<ParkingStatistics> GetParkingStatisticsAsync(int? parkingId);
    Task<IEnumerable<CityStatistics>> GetCityStatisticsAsync(DateTime? dateFrom, DateTime? dateTo);
    Task<IEnumerable<CityParkingCount>> GetCityParkingCountsAsync();
}