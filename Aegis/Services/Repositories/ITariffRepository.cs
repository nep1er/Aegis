namespace Aegis.Services.Repositories;

public interface ITariffRepository
{
    Task<decimal> GetTariffAsync(int parkingId, int vehicleTypeId);
    Task<IEnumerable<TariffInfo>> GetTariffsForParkingAsync(int parkingId);
}

public class TariffInfo
{
    public int VehicleTypeId { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public decimal Price { get; set; }
}