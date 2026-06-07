namespace Aegis.Models;

public class HistoryItemModel
{
    public int ParkingRecordId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public DateTime OperationDate { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string SpotNumber { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
}