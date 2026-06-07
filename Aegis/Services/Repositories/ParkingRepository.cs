using Aegis.Data;
using Aegis.Models;
using Npgsql;

namespace Aegis.Services.Repositories;

public class ParkingRepository : IParkingRepository
{
    private readonly AppDbContext _dbContext;

    public ParkingRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<ParkingDisplayModel>> GetParkingsForOperatorAsync(int operatorId)
    {
        var parkings = new List<ParkingDisplayModel>();

        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"SELECT p.id, p.city, p.street, p.building
              FROM ""parkings"" p
              JOIN ""operators"" o ON p.id = o.parking_id
              WHERE o.user_id = @operatorId
              ORDER BY p.city, p.street",
            connection);
        command.Parameters.AddWithValue("operatorId", operatorId);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            parkings.Add(new ParkingDisplayModel
            {
                ParkingId = reader.GetInt32(0),
                Address = $"{reader.GetString(1)}, {reader.GetString(2)}, {reader.GetString(3)}"
            });
        }

        return parkings;
    }

    public async Task<IEnumerable<ParkingDisplayModel>> GetAllParkingsAsync()
    {
        var parkings = new List<ParkingDisplayModel>();

        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"SELECT id, city, street, building
              FROM ""parkings""
              ORDER BY city, street",
            connection);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            parkings.Add(new ParkingDisplayModel
            {
                ParkingId = reader.GetInt32(0),
                Address = $"{reader.GetString(1)}, {reader.GetString(2)}, {reader.GetString(3)}"
            });
        }

        return parkings;
    }

    public async Task<IEnumerable<SpotDisplayModel>> GetSpotsForParkingAsync(int parkingId)
    {
        var spots = new List<SpotDisplayModel>();

        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"SELECT s.id, s.number, vt.type, ss.type as status, v.license_plate
              FROM ""spots"" s
              JOIN ""vehicletypes"" vt ON s.vehicle_type_id = vt.id
              JOIN ""spotstatuses"" ss ON s.spot_status_id = ss.id
              LEFT JOIN ""parkingrecords"" pr ON s.id = pr.spot_id AND pr.vehicle_status_id = 1
              LEFT JOIN ""vehicles"" v ON pr.vehicle_id = v.id
              WHERE s.parking_id = @parkingId
              ORDER BY s.number",
            connection);
        command.Parameters.AddWithValue("parkingId", parkingId);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var status = reader.GetString(3);
            var licensePlate = reader.IsDBNull(4) ? null : reader.GetString(4);

            spots.Add(new SpotDisplayModel
            {
                Id = reader.GetInt32(0),
                Number = reader.GetString(1),
                VehicleType = reader.GetString(2),
                Status = status,
                LicensePlate = licensePlate,
                IsOccupied = status == "Занята"
            });
        }

        return spots;
    }
}