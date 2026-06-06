namespace Aegis.Models;

public class ParkingDisplayModel
{
    public int Id { get; set; }
    public string Address { get; set; } = string.Empty;  // Город, Улица, Дом
    public int ParkingId { get; set; }
}