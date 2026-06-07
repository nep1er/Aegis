namespace Aegis.Models;

public class SpotDisplayModel
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? LicensePlate { get; set; }  // Гос номер если занято
    public bool IsOccupied { get; set; }
}