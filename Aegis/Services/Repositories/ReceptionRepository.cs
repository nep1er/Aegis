using Aegis.Data;
using Aegis.Models;
using Npgsql;

namespace Aegis.Services.Repositories;

public class ReceptionRepository : IReceptionRepository
{
    private readonly AppDbContext _dbContext;

    public ReceptionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> AddVehiclePhotoAsync(int parkingRecordId, byte[] photoData, string? description)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"INSERT INTO ""vehiclephotos"" (parking_record_id, photo, description) 
              VALUES (@parkingRecordId, @photo, @description)
              RETURNING id",
            connection);

        command.Parameters.AddWithValue("parkingRecordId", parkingRecordId);
        command.Parameters.AddWithValue("photo", photoData);
        command.Parameters.AddWithValue("description", string.IsNullOrWhiteSpace(description) ? DBNull.Value : description);

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task<int> CreateReceptionAsync(ReceptionData data)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // ВСЕГДА создаём НОВУЮ запись в Vehicles
            int vehicleId;
            using var insertCmd = new NpgsqlCommand(
                @"INSERT INTO ""vehicles"" 
                  (license_plate, vehicle_type_id) 
                  VALUES (@licensePlate, @vehicleTypeId) 
                  RETURNING id",
                connection);
            insertCmd.Parameters.AddWithValue("licensePlate", data.LicensePlate);
            insertCmd.Parameters.AddWithValue("vehicleTypeId", data.VehicleTypeId);
            vehicleId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

            // Создаём запись в ParkingRecords (БЕЗ tow_fine!)
            int parkingRecordId;
            using var insertRecordCmd = new NpgsqlCommand(
                @"INSERT INTO ""parkingrecords"" 
                  (spot_id, admission_date, operator_id, vehicle_type_id, vehicle_id, vehicle_status_id) 
                  VALUES (@spotId, @admissionDate, @operatorId, @vehicleTypeId, @vehicleId, @vehicleStatusId) 
                  RETURNING id",
                connection);
            insertRecordCmd.Parameters.AddWithValue("spotId", data.SpotId);
            insertRecordCmd.Parameters.AddWithValue("admissionDate", data.AdmissionDate);
            insertRecordCmd.Parameters.AddWithValue("operatorId", data.OperatorId);
            insertRecordCmd.Parameters.AddWithValue("vehicleTypeId", data.VehicleTypeId);
            insertRecordCmd.Parameters.AddWithValue("vehicleId", vehicleId);
            insertRecordCmd.Parameters.AddWithValue("vehicleStatusId", data.VehicleStatusId);

            parkingRecordId = Convert.ToInt32(await insertRecordCmd.ExecuteScalarAsync());

            // Обновляем статус места
            using var updateSpotCmd = new NpgsqlCommand(
                @"UPDATE ""spots"" SET spot_status_id = 1 WHERE id = @spotId",
                connection);
            updateSpotCmd.Parameters.AddWithValue("spotId", data.SpotId);
            await updateSpotCmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            return parkingRecordId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}