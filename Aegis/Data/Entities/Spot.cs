using Aegis.Data.Entities;

namespace Aegis.Data.Entities;

public class Spot
{
    public int Id { get; set; }
    public int ParkingId { get; set; }
    public string Number { get; set; } = string.Empty;
    public int SpotStatusId { get; set; }
    public int VehicleTypeId { get; set; }

    // Навигационные свойства
    public Parking? Parking { get; set; }
    public SpotStatus? SpotStatus { get; set; }
    public VehicleType? VehicleType { get; set; }
    public ParkingRecord? ParkingRecord { get; set; }
}