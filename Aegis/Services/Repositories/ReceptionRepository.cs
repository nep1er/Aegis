using Aegis.Data;
using Npgsql;

namespace Aegis.Services.Repositories;

public class ReceptionRepository : IReceptionRepository
{
    private readonly AppDbContext _dbContext;

    public ReceptionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> CreateReceptionAsync(ReceptionData data)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1. Ищем или создаём Vehicle
            int vehicleId;

            using var findVehicleCmd = new NpgsqlCommand(
                @"SELECT id FROM ""vehicles"" WHERE license_plate = @licensePlate",
                connection);
            findVehicleCmd.Parameters.AddWithValue("licensePlate", data.LicensePlate);

            var existingVehicleId = await findVehicleCmd.ExecuteScalarAsync();

            if (existingVehicleId != null)
            {
                vehicleId = Convert.ToInt32(existingVehicleId);
            }
            else
            {
                using var insertVehicleCmd = new NpgsqlCommand(
                    @"INSERT INTO ""vehicles"" (license_plate, vehicle_type_id) 
                      VALUES (@licensePlate, @vehicleTypeId) 
                      RETURNING id",
                    connection);
                insertVehicleCmd.Parameters.AddWithValue("licensePlate", data.LicensePlate);
                insertVehicleCmd.Parameters.AddWithValue("vehicleTypeId", data.VehicleTypeId);

                vehicleId = Convert.ToInt32(await insertVehicleCmd.ExecuteScalarAsync());
            }

            // 2. Создаём запись в ParkingRecords
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

            // 3. Обновляем статус места на "Занята"
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