using Aegis.Data;
using Aegis.Models;
using Npgsql;

namespace Aegis.Services.Repositories;

public class SpotRepository : ISpotRepository
{
    private readonly AppDbContext _dbContext;

    public SpotRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<SpotDisplayModel>> GetFreeSpotsAsync(int parkingId, int vehicleTypeId)
    {
        var spots = new List<SpotDisplayModel>();

        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"SELECT s.id, s.number, vt.type
              FROM ""spots"" s
              JOIN ""vehicletypes"" vt ON s.vehicle_type_id = vt.id
              JOIN ""spotstatuses"" ss ON s.spot_status_id = ss.id
              WHERE s.parking_id = @parkingId 
                AND s.vehicle_type_id = @vehicleTypeId
                AND ss.type = 'Свободна'
              ORDER BY s.number",
            connection);
        command.Parameters.AddWithValue("parkingId", parkingId);
        command.Parameters.AddWithValue("vehicleTypeId", vehicleTypeId);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            spots.Add(new SpotDisplayModel
            {
                Id = reader.GetInt32(0),
                Number = reader.GetString(1),
                VehicleType = reader.GetString(2),
                Status = "Свободна",
                IsOccupied = false
            });
        }

        return spots;
    }

    public async Task<bool> IsSpotFreeAsync(int spotId)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"SELECT ss.type FROM ""spots"" s
              JOIN ""spotstatuses"" ss ON s.spot_status_id = ss.id
              WHERE s.id = @spotId",
            connection);
        command.Parameters.AddWithValue("spotId", spotId);

        var result = await command.ExecuteScalarAsync();
        return result?.ToString() == "Свободна";
    }
}