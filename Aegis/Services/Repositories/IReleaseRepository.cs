namespace Aegis.Services.Repositories;

public interface IReleaseRepository
{
    Task<IEnumerable<ActiveVehicleModel>> GetActiveVehiclesAsync(int parkingId);
    Task<ActiveVehicleModel?> GetActiveVehicleByIdAsync(int parkingRecordId);
    Task<int> CompleteReleaseAsync(ReleaseData data);
}

public class ActiveVehicleModel
{
    public int ParkingRecordId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public int VehicleTypeId { get; set; }
    public int SpotId { get; set; }
    public string SpotNumber { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public decimal Tariff { get; set; }
    public decimal TowFine { get; set; }
}

public class ReleaseData
{
    public int ParkingRecordId { get; set; }
    public int OperatorId { get; set; }
    public string OwnerFullName { get; set; } = string.Empty;
    public string Vin { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int DocumentTypeId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public decimal StorageFee { get; set; }
    public decimal TowFine { get; set; }
    public decimal TotalAmount { get; set; }
    public int TariffId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
}