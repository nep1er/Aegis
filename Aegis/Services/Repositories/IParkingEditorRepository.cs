using Aegis.Models;

namespace Aegis.Services.Repositories;

public interface IParkingEditorRepository
{
    Task<IEnumerable<ParkingDisplayModel>> GetAllParkingsAsync();
    Task<ParkingDetailsModel?> GetParkingDetailsAsync(int parkingId);
    Task<int> CreateParkingAsync(CreateParkingData data);
    Task UpdateParkingAsync(UpdateParkingData data);
    Task DeleteParkingAsync(int parkingId);
    Task<IEnumerable<VehicleTypeModel>> GetAllVehicleTypesAsync();
    Task SetTariffAsync(int parkingId, int vehicleTypeId, decimal price);
    Task AddSpotAsync(int parkingId, string number, int vehicleTypeId);
    Task DeleteSpotAsync(int spotId);
    Task<bool> HasOccupiedSpotsAsync(int parkingId);
    Task<bool> IsSpotOccupiedAsync(int spotId);
    Task<IEnumerable<SpotModel>> GetParkingSpotsAsync(int parkingId);
}

public class ParkingDetailsModel
{
    public int Id { get; set; }
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public string Address => $"{City}, {Street}, {Building}";
    public List<TariffModel> Tariffs { get; set; } = new();
    public List<SpotModel> Spots { get; set; } = new();
}

public class TariffModel
{
    public int Id { get; set; }
    public int VehicleTypeId { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class SpotModel
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public int VehicleTypeId { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsOccupied { get; set; }
}

public class CreateParkingData
{
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public List<TariffInput> Tariffs { get; set; } = new();
    public List<SpotInput> Spots { get; set; } = new();
}

public class UpdateParkingData
{
    public int ParkingId { get; set; }
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
}

public class TariffInput
{
    public int VehicleTypeId { get; set; }
    public decimal Price { get; set; }
}

public class SpotInput
{
    public string Number { get; set; } = string.Empty;
    public int VehicleTypeId { get; set; }
}

public class VehicleTypeModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}