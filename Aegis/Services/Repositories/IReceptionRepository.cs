using Aegis.Models;

namespace Aegis.Services.Repositories;

public interface IReceptionRepository
{
    Task<int> CreateReceptionAsync(ReceptionData data);
}

public class ReceptionData
{
    public string LicensePlate { get; set; } = string.Empty;
    public int SpotId { get; set; }
    public int VehicleTypeId { get; set; }
    public int OperatorId { get; set; }
    public int? VehicleId { get; set; }
    public int VehicleStatusId { get; set; }
    public DateTime AdmissionDate { get; set; }
}