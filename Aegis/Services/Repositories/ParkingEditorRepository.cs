using Aegis.Data;
using Aegis.Models;
using Npgsql;

namespace Aegis.Services.Repositories;

public class ParkingEditorRepository : IParkingEditorRepository
{
    private readonly AppDbContext _dbContext;

    public ParkingEditorRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
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

    public async Task<ParkingDetailsModel?> GetParkingDetailsAsync(int parkingId)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"SELECT id, city, street, building FROM ""parkings"" WHERE id = @id",
            connection);
        command.Parameters.AddWithValue("id", parkingId);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var parking = new ParkingDetailsModel
            {
                Id = reader.GetInt32(0),
                City = reader.GetString(1),
                Street = reader.GetString(2),
                Building = reader.GetString(3)
            };

            await reader.CloseAsync();

            // Загружаем тарифы
            using var tariffCmd = new NpgsqlCommand(
                @"SELECT t.id, t.vehicle_type_id, vt.type, t.price
                  FROM ""tariffs"" t
                  JOIN ""vehicletypes"" vt ON t.vehicle_type_id = vt.id
                  WHERE t.parking_id = @parkingId",
                connection);
            tariffCmd.Parameters.AddWithValue("parkingId", parkingId);

            using var tariffReader = await tariffCmd.ExecuteReaderAsync();
            while (await tariffReader.ReadAsync())
            {
                parking.Tariffs.Add(new TariffModel
                {
                    Id = tariffReader.GetInt32(0),
                    VehicleTypeId = tariffReader.GetInt32(1),
                    VehicleType = tariffReader.GetString(2),
                    Price = tariffReader.GetDecimal(3)
                });
            }

            await tariffReader.CloseAsync();

            // Загружаем места
            using var spotCmd = new NpgsqlCommand(
                @"SELECT s.id, s.number, s.vehicle_type_id, vt.type, ss.type
                  FROM ""spots"" s
                  JOIN ""vehicletypes"" vt ON s.vehicle_type_id = vt.id
                  JOIN ""spotstatuses"" ss ON s.spot_status_id = ss.id
                  WHERE s.parking_id = @parkingId
                  ORDER BY s.number",
                connection);
            spotCmd.Parameters.AddWithValue("parkingId", parkingId);

            using var spotReader = await spotCmd.ExecuteReaderAsync();
            while (await spotReader.ReadAsync())
            {
                parking.Spots.Add(new SpotModel
                {
                    Id = spotReader.GetInt32(0),
                    Number = spotReader.GetString(1),
                    VehicleTypeId = spotReader.GetInt32(2),
                    VehicleType = spotReader.GetString(3),
                    Status = spotReader.GetString(4)
                });
            }

            return parking;
        }

        return null;
    }

    public async Task<int> CreateParkingAsync(CreateParkingData data)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            int parkingId;

            using var insertCmd = new NpgsqlCommand(
                @"INSERT INTO ""parkings"" (city, street, building, parking_status_id) 
                  VALUES (@city, @street, @building, 1)
                  RETURNING id",
                connection);
            insertCmd.Parameters.AddWithValue("city", data.City);
            insertCmd.Parameters.AddWithValue("street", data.Street);
            insertCmd.Parameters.AddWithValue("building", data.Building);

            parkingId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

            foreach (var tariff in data.Tariffs)
            {
                using var tariffCmd = new NpgsqlCommand(
                    @"INSERT INTO ""tariffs"" (parking_id, vehicle_type_id, price) 
                      VALUES (@parkingId, @vehicleTypeId, @price)",
                    connection);
                tariffCmd.Parameters.AddWithValue("parkingId", parkingId);
                tariffCmd.Parameters.AddWithValue("vehicleTypeId", tariff.VehicleTypeId);
                tariffCmd.Parameters.AddWithValue("price", tariff.Price);
                await tariffCmd.ExecuteNonQueryAsync();
            }

            foreach (var spot in data.Spots)
            {
                using var spotCmd = new NpgsqlCommand(
                    @"INSERT INTO ""spots"" (parking_id, number, vehicle_type_id, spot_status_id) 
                      VALUES (@parkingId, @number, @vehicleTypeId, 2)",
                    connection);
                spotCmd.Parameters.AddWithValue("parkingId", parkingId);
                spotCmd.Parameters.AddWithValue("number", spot.Number);
                spotCmd.Parameters.AddWithValue("vehicleTypeId", spot.VehicleTypeId);
                await spotCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return parkingId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateParkingAsync(UpdateParkingData data)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"UPDATE ""parkings"" 
              SET city = @city, street = @street, building = @building 
              WHERE id = @id",
            connection);
        command.Parameters.AddWithValue("city", data.City);
        command.Parameters.AddWithValue("street", data.Street);
        command.Parameters.AddWithValue("building", data.Building);
        command.Parameters.AddWithValue("id", data.ParkingId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteParkingAsync(int parkingId)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            using var deleteTariffsCmd = new NpgsqlCommand(
                @"DELETE FROM ""tariffs"" WHERE parking_id = @id",
                connection);
            deleteTariffsCmd.Parameters.AddWithValue("id", parkingId);
            await deleteTariffsCmd.ExecuteNonQueryAsync();

            using var deleteSpotsCmd = new NpgsqlCommand(
                @"DELETE FROM ""spots"" WHERE parking_id = @id",
                connection);
            deleteSpotsCmd.Parameters.AddWithValue("id", parkingId);
            await deleteSpotsCmd.ExecuteNonQueryAsync();

            using var deleteParkingCmd = new NpgsqlCommand(
                @"DELETE FROM ""parkings"" WHERE id = @id",
                connection);
            deleteParkingCmd.Parameters.AddWithValue("id", parkingId);
            await deleteParkingCmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<VehicleTypeModel>> GetAllVehicleTypesAsync()
    {
        var types = new List<VehicleTypeModel>();

        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"SELECT id, type FROM ""vehicletypes"" ORDER BY id",
            connection);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            types.Add(new VehicleTypeModel
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            });
        }

        return types;
    }

    public async Task SetTariffAsync(int parkingId, int vehicleTypeId, decimal price)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"INSERT INTO ""tariffs"" (parking_id, vehicle_type_id, price) 
              VALUES (@parkingId, @vehicleTypeId, @price)
              ON CONFLICT (parking_id, vehicle_type_id) 
              DO UPDATE SET price = EXCLUDED.price",
            connection);
        command.Parameters.AddWithValue("parkingId", parkingId);
        command.Parameters.AddWithValue("vehicleTypeId", vehicleTypeId);
        command.Parameters.AddWithValue("price", price);

        await command.ExecuteNonQueryAsync();
    }

    public async Task AddSpotAsync(int parkingId, string number, int vehicleTypeId)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"INSERT INTO ""spots"" (parking_id, number, vehicle_type_id, spot_status_id) 
              VALUES (@parkingId, @number, @vehicleTypeId, 2)",
            connection);
        command.Parameters.AddWithValue("parkingId", parkingId);
        command.Parameters.AddWithValue("number", number);
        command.Parameters.AddWithValue("vehicleTypeId", vehicleTypeId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteSpotAsync(int spotId)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"DELETE FROM ""spots"" WHERE id = @id",
            connection);
        command.Parameters.AddWithValue("id", spotId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> HasOccupiedSpotsAsync(int parkingId)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"SELECT COUNT(*) > 0
              FROM ""spots""
              WHERE parking_id = @parkingId AND spot_status_id = 1",
            connection);
        command.Parameters.AddWithValue("parkingId", parkingId);

        return Convert.ToBoolean(await command.ExecuteScalarAsync());
    }

    public async Task<bool> IsSpotOccupiedAsync(int spotId)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"SELECT COUNT(*) > 0
              FROM ""parkingrecords""
              WHERE spot_id = @spotId AND vehicle_status_id = 1",
            connection);
        command.Parameters.AddWithValue("spotId", spotId);

        return Convert.ToBoolean(await command.ExecuteScalarAsync());
    }

    public async Task<IEnumerable<SpotModel>> GetParkingSpotsAsync(int parkingId)
    {
        var spots = new List<SpotModel>();

        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"SELECT s.id, s.number, s.vehicle_type_id, vt.type, ss.type, s.spot_status_id
          FROM ""spots"" s
          JOIN ""vehicletypes"" vt ON s.vehicle_type_id = vt.id
          JOIN ""spotstatuses"" ss ON s.spot_status_id = ss.id
          WHERE s.parking_id = @parkingId
          ORDER BY s.number",
            connection);
        command.Parameters.AddWithValue("parkingId", parkingId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            spots.Add(new SpotModel
            {
                Id = reader.GetInt32(0),
                Number = reader.GetString(1),
                VehicleTypeId = reader.GetInt32(2),
                VehicleType = reader.GetString(3),
                Status = reader.GetString(4)
            });
        }

        await reader.CloseAsync();

        // Проверяем занятость каждого места
        foreach (var spot in spots)
        {
            spot.IsOccupied = await IsSpotOccupiedAsync(spot.Id);
        }

        return spots;
    }
}