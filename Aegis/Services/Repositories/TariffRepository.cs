using Aegis.Data;
using Npgsql;

namespace Aegis.Services.Repositories;

public class TariffRepository : ITariffRepository
{
    private readonly AppDbContext _dbContext;

    public TariffRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<decimal> GetTariffAsync(int parkingId, int vehicleTypeId)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"SELECT price FROM ""tariffs"" 
              WHERE parking_id = @parkingId AND vehicle_type_id = @vehicleTypeId",
            connection);
        command.Parameters.AddWithValue("parkingId", parkingId);
        command.Parameters.AddWithValue("vehicleTypeId", vehicleTypeId);

        var result = await command.ExecuteScalarAsync();
        return result != null ? Convert.ToDecimal(result) : 0;
    }

    public async Task<IEnumerable<TariffInfo>> GetTariffsForParkingAsync(int parkingId)
    {
        var tariffs = new List<TariffInfo>();

        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"SELECT t.vehicle_type_id, vt.type, t.price
              FROM ""tariffs"" t
              JOIN ""vehicletypes"" vt ON t.vehicle_type_id = vt.id
              WHERE t.parking_id = @parkingId",
            connection);
        command.Parameters.AddWithValue("parkingId", parkingId);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            tariffs.Add(new TariffInfo
            {
                VehicleTypeId = reader.GetInt32(0),
                VehicleType = reader.GetString(1),
                Price = reader.GetDecimal(2)
            });
        }

        return tariffs;
    }
}