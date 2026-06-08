namespace Aegis.Models;

public class MonthlyRevenue
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
}

public class MonthlyParkingRevenue
{
    public int ParkingId { get; set; }
    public string ParkingAddress { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class ParkingRevenue
{
    public int ParkingId { get; set; }
    public string ParkingAddress { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
}

public class VehicleTypeStatistics
{
    public int VehicleTypeId { get; set; }
    public string VehicleTypeName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ParkingStatistics
{
    public int ParkingId { get; set; }
    public string ParkingAddress { get; set; } = string.Empty;
    public int TotalReceived { get; set; }
    public int CurrentlyParked { get; set; }
    public int Released { get; set; }
}

public class CityStatistics
{
    public string City { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int TotalParkings { get; set; }
    public int TotalVehicles { get; set; }
}

public class CityParkingCount
{
    public string City { get; set; } = string.Empty;
    public int ParkingCount { get; set; }
}