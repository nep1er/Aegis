using Aegis.Data;
using Npgsql;

namespace Aegis.Services.Repositories;

public class ReleaseRepository : IReleaseRepository
{
    private readonly AppDbContext _dbContext;

    public ReleaseRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<ActiveVehicleModel>> GetActiveVehiclesAsync(int parkingId)
    {
        var vehicles = new List<ActiveVehicleModel>();

        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"SELECT 
            pr.id,
            v.license_plate,
            vt.type,
            vt.id,
            s.id,
            s.number,
            pr.admission_date,
            t.price,
            vt.tow_fine
          FROM ""parkingrecords"" pr
          JOIN ""spots"" s ON pr.spot_id = s.id
          JOIN ""vehicles"" v ON pr.vehicle_id = v.id
          JOIN ""vehicletypes"" vt ON pr.vehicle_type_id = vt.id
          JOIN ""tariffs"" t ON t.parking_id = s.parking_id AND t.vehicle_type_id = vt.id
          WHERE s.parking_id = @parkingId 
            AND pr.vehicle_status_id = 1
          ORDER BY pr.admission_date DESC",
            connection);
        command.Parameters.AddWithValue("parkingId", parkingId);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            vehicles.Add(new ActiveVehicleModel
            {
                ParkingRecordId = reader.GetInt32(0),
                LicensePlate = reader.GetString(1),
                VehicleType = reader.GetString(2),
                VehicleTypeId = reader.GetInt32(3),
                SpotId = reader.GetInt32(4),
                SpotNumber = reader.GetString(5),
                AdmissionDate = reader.GetDateTime(6),
                Tariff = reader.GetDecimal(7),
                TowFine = reader.GetDecimal(8)
            });
        }

        return vehicles;
    }

    public async Task<ActiveVehicleModel?> GetActiveVehicleByIdAsync(int parkingRecordId)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"SELECT 
            pr.id,
            v.license_plate,
            vt.type,
            vt.id,
            s.id,
            s.number,
            pr.admission_date,
            t.price,
            vt.tow_fine
          FROM ""parkingrecords"" pr
          JOIN ""spots"" s ON pr.spot_id = s.id
          JOIN ""vehicles"" v ON pr.vehicle_id = v.id
          JOIN ""vehicletypes"" vt ON pr.vehicle_type_id = vt.id
          JOIN ""tariffs"" t ON t.parking_id = s.parking_id AND t.vehicle_type_id = vt.id
          WHERE pr.id = @id AND pr.vehicle_status_id = 1",
            connection);
        command.Parameters.AddWithValue("id", parkingRecordId);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new ActiveVehicleModel
            {
                ParkingRecordId = reader.GetInt32(0),
                LicensePlate = reader.GetString(1),
                VehicleType = reader.GetString(2),
                VehicleTypeId = reader.GetInt32(3),
                SpotId = reader.GetInt32(4),
                SpotNumber = reader.GetString(5),
                AdmissionDate = reader.GetDateTime(6),
                Tariff = reader.GetDecimal(7),
                TowFine = reader.GetDecimal(8)
            };
        }

        return null;
    }

    public async Task<int> CompleteReleaseAsync(ReleaseData data)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1. Получаем данные ParkingRecord
            int vehicleId;
            int spotId;
            int vehicleTypeId;
            int parkingId;

            using var getRecordCmd = new NpgsqlCommand(
                @"SELECT pr.vehicle_id, pr.spot_id, pr.vehicle_type_id, s.parking_id
      FROM ""parkingrecords"" pr
      JOIN ""spots"" s ON pr.spot_id = s.id
      WHERE pr.id = @id",
                connection);
            getRecordCmd.Parameters.AddWithValue("id", data.ParkingRecordId);

            using var recordReader = await getRecordCmd.ExecuteReaderAsync();
            if (!await recordReader.ReadAsync())
                throw new Exception("Запись не найдена");

            var existingVehicleId = recordReader.IsDBNull(0) ? (int?)null : recordReader.GetInt32(0);
            spotId = recordReader.GetInt32(1);
            vehicleTypeId = recordReader.GetInt32(2);
            parkingId = recordReader.GetInt32(3);
            await recordReader.CloseAsync();

            // 2. Получаем правильный tariff_id
            int tariffId;
            using var getTariffCmd = new NpgsqlCommand(
                @"SELECT id FROM ""tariffs"" 
              WHERE parking_id = @parkingId AND vehicle_type_id = @vehicleTypeId",
                connection);
            getTariffCmd.Parameters.AddWithValue("parkingId", parkingId);
            getTariffCmd.Parameters.AddWithValue("vehicleTypeId", vehicleTypeId);

            var tariffResult = await getTariffCmd.ExecuteScalarAsync();
            if (tariffResult == null)
                throw new Exception("Тариф не найден для данной парковки и типа авто");

            tariffId = Convert.ToInt32(tariffResult);

            // 3. Обновляем или создаём Vehicle
            if (existingVehicleId.HasValue)
            {
                using var updateVehicleCmd = new NpgsqlCommand(
                    @"UPDATE ""vehicles"" 
                  SET vin = @vin, brand = @brand, model = @model 
                  WHERE id = @id",
                    connection);
                updateVehicleCmd.Parameters.AddWithValue("vin", string.IsNullOrWhiteSpace(data.Vin) ? DBNull.Value : data.Vin);
                updateVehicleCmd.Parameters.AddWithValue("brand", string.IsNullOrWhiteSpace(data.Brand) ? DBNull.Value : data.Brand);
                updateVehicleCmd.Parameters.AddWithValue("model", string.IsNullOrWhiteSpace(data.Model) ? DBNull.Value : data.Model);
                updateVehicleCmd.Parameters.AddWithValue("id", existingVehicleId.Value);
                await updateVehicleCmd.ExecuteNonQueryAsync();
                vehicleId = existingVehicleId.Value;
            }
            else
            {
                string licensePlate;
                using var getPlateCmd = new NpgsqlCommand(
                    @"SELECT license_plate FROM ""parkingrecords"" WHERE id = @id",
                    connection);
                getPlateCmd.Parameters.AddWithValue("id", data.ParkingRecordId);
                licensePlate = (await getPlateCmd.ExecuteScalarAsync())?.ToString() ?? "";

                using var insertVehicleCmd = new NpgsqlCommand(
                    @"INSERT INTO ""vehicles"" 
                  (license_plate, vin, brand, model, vehicle_type_id) 
                  VALUES (@licensePlate, @vin, @brand, @model, @vehicleTypeId) 
                  RETURNING id",
                    connection);
                insertVehicleCmd.Parameters.AddWithValue("licensePlate", licensePlate);
                insertVehicleCmd.Parameters.AddWithValue("vin", string.IsNullOrWhiteSpace(data.Vin) ? DBNull.Value : data.Vin);
                insertVehicleCmd.Parameters.AddWithValue("brand", string.IsNullOrWhiteSpace(data.Brand) ? DBNull.Value : data.Brand);
                insertVehicleCmd.Parameters.AddWithValue("model", string.IsNullOrWhiteSpace(data.Model) ? DBNull.Value : data.Model);
                insertVehicleCmd.Parameters.AddWithValue("vehicleTypeId", vehicleTypeId);
                vehicleId = Convert.ToInt32(await insertVehicleCmd.ExecuteScalarAsync());

                using var updateRecordCmd = new NpgsqlCommand(
                    @"UPDATE ""parkingrecords"" SET vehicle_id = @vehicleId WHERE id = @id",
                    connection);
                updateRecordCmd.Parameters.AddWithValue("vehicleId", vehicleId);
                updateRecordCmd.Parameters.AddWithValue("id", data.ParkingRecordId);
                await updateRecordCmd.ExecuteNonQueryAsync();
            }

            // 4. Обновляем статус ParkingRecord на "Выдана" (id=2)
            using var updateStatusCmd = new NpgsqlCommand(
                @"UPDATE ""parkingrecords"" 
              SET vehicle_status_id = 2 
              WHERE id = @id",
                connection);
            updateStatusCmd.Parameters.AddWithValue("id", data.ParkingRecordId);
            await updateStatusCmd.ExecuteNonQueryAsync();

            // 5. Освобождаем место (статус "Свободна" id=2)
            using var freeSpotCmd = new NpgsqlCommand(
                @"UPDATE ""spots"" SET spot_status_id = 2 WHERE id = @spotId",
                connection);
            freeSpotCmd.Parameters.AddWithValue("spotId", spotId);
            await freeSpotCmd.ExecuteNonQueryAsync();

            // 6. Создаём запись в ReleaseHistory с ПРАВИЛЬНЫМ tariff_id
            int releaseHistoryId;
            using var insertReleaseCmd = new NpgsqlCommand(
                @"INSERT INTO ""releasehistory"" 
              (parking_record_id, document_type_id, document_number, release_date, 
               tariff_id, storage_fee, tow_fine, operator_id) 
              VALUES (@parkingRecordId, @documentTypeId, @documentNumber, @releaseDate,
                      @tariffId, @storageFee, @towFine, @operatorId) 
              RETURNING id",
                connection);
            insertReleaseCmd.Parameters.AddWithValue("parkingRecordId", data.ParkingRecordId);
            insertReleaseCmd.Parameters.AddWithValue("documentTypeId", data.DocumentTypeId);
            insertReleaseCmd.Parameters.AddWithValue("documentNumber", data.DocumentNumber);
            insertReleaseCmd.Parameters.AddWithValue("releaseDate", data.ReleaseDate);
            insertReleaseCmd.Parameters.AddWithValue("tariffId", tariffId);  // ← ИСПРАВЛЕНО!
            insertReleaseCmd.Parameters.AddWithValue("storageFee", data.StorageFee);
            insertReleaseCmd.Parameters.AddWithValue("towFine", data.TowFine);
            insertReleaseCmd.Parameters.AddWithValue("operatorId", data.OperatorId);
            releaseHistoryId = Convert.ToInt32(await insertReleaseCmd.ExecuteScalarAsync());

            // 7. Создаём запись в Payments
            using var insertPaymentCmd = new NpgsqlCommand(
                @"INSERT INTO ""payments"" 
              (payment_date, receipt_number, amount, parking_record_id) 
              VALUES (@paymentDate, @receiptNumber, @amount, @parkingRecordId) 
              RETURNING id",
                connection);
            insertPaymentCmd.Parameters.AddWithValue("paymentDate", data.ReleaseDate);
            insertPaymentCmd.Parameters.AddWithValue("receiptNumber", string.IsNullOrWhiteSpace(data.ReceiptNumber) ? DBNull.Value : data.ReceiptNumber);
            insertPaymentCmd.Parameters.AddWithValue("amount", data.TotalAmount);
            insertPaymentCmd.Parameters.AddWithValue("parkingRecordId", data.ParkingRecordId);
            await insertPaymentCmd.ExecuteScalarAsync();

            await transaction.CommitAsync();
            return releaseHistoryId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}