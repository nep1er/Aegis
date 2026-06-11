using Aegis.Data;
using Aegis.Models;
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
            pr.vehicle_id,
            v.license_plate,
            vt.type,
            s.number,
            t.price,
            pr.admission_date,
            COALESCE(vt.tow_fine, 0)
        FROM ""parkingrecords"" pr
        JOIN ""vehicles"" v ON pr.vehicle_id = v.id
        JOIN ""vehicletypes"" vt ON pr.vehicle_type_id = vt.id
        JOIN ""spots"" s ON pr.spot_id = s.id
        JOIN ""tariffs"" t ON t.parking_id = s.parking_id AND t.vehicle_type_id = vt.id
        WHERE s.parking_id = @parkingId 
          AND pr.vehicle_status_id = 1
        ORDER BY pr.admission_date",
            connection);
        command.Parameters.AddWithValue("parkingId", parkingId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            vehicles.Add(new ActiveVehicleModel
            {
                ParkingRecordId = reader.GetInt32(0),
                VehicleId = reader.GetInt32(1),
                LicensePlate = reader.GetString(2),
                VehicleType = reader.GetString(3),
                SpotNumber = reader.GetString(4),
                Tariff = reader.GetDecimal(5),
                AdmissionDate = reader.GetDateTime(6),
                TowFine = reader.GetDecimal(7)  // ← БЕРУ ИЗ VEHICLE TYPES!
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
            pr.vehicle_id,
            v.license_plate,
            vt.type,
            s.number,
            t.price,
            pr.admission_date,
            COALESCE(vt.tow_fine, 0)
        FROM ""parkingrecords"" pr
        JOIN ""vehicles"" v ON pr.vehicle_id = v.id
        JOIN ""vehicletypes"" vt ON pr.vehicle_type_id = vt.id
        JOIN ""spots"" s ON pr.spot_id = s.id
        JOIN ""tariffs"" t ON t.parking_id = s.parking_id AND t.vehicle_type_id = vt.id
        WHERE pr.id = @id",
            connection);
        command.Parameters.AddWithValue("id", parkingRecordId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ActiveVehicleModel
            {
                ParkingRecordId = reader.GetInt32(0),
                VehicleId = reader.GetInt32(1),
                LicensePlate = reader.GetString(2),
                VehicleType = reader.GetString(3),
                SpotNumber = reader.GetString(4),
                Tariff = reader.GetDecimal(5),
                AdmissionDate = reader.GetDateTime(6),
                TowFine = reader.GetDecimal(7)  // ← БЕРУ ИЗ VEHICLE TYPES!
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
            int vehicleId, spotId, tariffId;

            // 1. Получаем vehicle_id, spot_id И tariff_id из ParkingRecords + Tariffs
            using var getCmd = new NpgsqlCommand(
                @"SELECT 
                pr.vehicle_id, 
                s.id as spot_id,
                t.id as tariff_id
            FROM ""parkingrecords"" pr
            JOIN ""spots"" s ON pr.spot_id = s.id
            JOIN ""tariffs"" t ON t.parking_id = s.parking_id AND t.vehicle_type_id = pr.vehicle_type_id
            WHERE pr.id = @id",
                connection);
            getCmd.Parameters.AddWithValue("id", data.ParkingRecordId);

            using var getReader = await getCmd.ExecuteReaderAsync();
            await getReader.ReadAsync();
            vehicleId = getReader.GetInt32(0);
            spotId = getReader.GetInt32(1);
            tariffId = getReader.GetInt32(2);  // ← БЕРУ РЕАЛЬНЫЙ tariff_id!
            await getReader.CloseAsync();

            // 2. Обновляем Vehicles
            using var updateVehicleCmd = new NpgsqlCommand(
                @"UPDATE ""vehicles"" 
              SET vin = @vin,
                  brand = @brand,
                  model = @model
              WHERE id = @vehicleId",
                connection);
            updateVehicleCmd.Parameters.AddWithValue("vin", string.IsNullOrWhiteSpace(data.Vin) ? DBNull.Value : data.Vin);
            updateVehicleCmd.Parameters.AddWithValue("brand", string.IsNullOrWhiteSpace(data.Brand) ? DBNull.Value : data.Brand);
            updateVehicleCmd.Parameters.AddWithValue("model", string.IsNullOrWhiteSpace(data.Model) ? DBNull.Value : data.Model);
            updateVehicleCmd.Parameters.AddWithValue("vehicleId", vehicleId);
            await updateVehicleCmd.ExecuteNonQueryAsync();

            // 3. ReleaseHistory - с правильным tariff_id
            int releaseId;
            using var insertReleaseCmd = new NpgsqlCommand(
                @"INSERT INTO ""releasehistory"" 
              (parking_record_id, operator_id, document_type_id, document_number, 
               storage_fee, tow_fine, tariff_id, release_date)
              VALUES 
              (@parkingRecordId, @operatorId, @documentTypeId, @documentNumber,
               @storageFee, @towFine, @tariffId, @releaseDate)
              RETURNING id",
                connection);
            insertReleaseCmd.Parameters.AddWithValue("parkingRecordId", data.ParkingRecordId);
            insertReleaseCmd.Parameters.AddWithValue("operatorId", data.OperatorId);
            insertReleaseCmd.Parameters.AddWithValue("documentTypeId", data.DocumentTypeId);
            insertReleaseCmd.Parameters.AddWithValue("documentNumber", data.DocumentNumber);
            insertReleaseCmd.Parameters.AddWithValue("storageFee", data.StorageFee);
            insertReleaseCmd.Parameters.AddWithValue("towFine", data.TowFine);
            insertReleaseCmd.Parameters.AddWithValue("tariffId", tariffId);  // ← РЕАЛЬНЫЙ ID!
            insertReleaseCmd.Parameters.AddWithValue("releaseDate", data.ReleaseDate);

            releaseId = Convert.ToInt32(await insertReleaseCmd.ExecuteScalarAsync());

            // 4. Payments
            using var insertPaymentCmd = new NpgsqlCommand(
                @"INSERT INTO ""payments"" 
      (parking_record_id, amount, receipt_number, payment_date)
      VALUES 
      (@parkingRecordId, @amount, @receiptNumber, @paymentDate)",
                connection);
            insertPaymentCmd.Parameters.AddWithValue("parkingRecordId", data.ParkingRecordId);
            // УБРАЛ releaseId!
            insertPaymentCmd.Parameters.AddWithValue("amount", data.TotalAmount);
            insertPaymentCmd.Parameters.AddWithValue("receiptNumber", data.ReceiptNumber);
            insertPaymentCmd.Parameters.AddWithValue("paymentDate", data.ReleaseDate);
            await insertPaymentCmd.ExecuteNonQueryAsync();

            // 5. Обновляем статус
            using var updateRecordCmd = new NpgsqlCommand(
                @"UPDATE ""parkingrecords"" SET vehicle_status_id = 2 WHERE id = @id",
                connection);
            updateRecordCmd.Parameters.AddWithValue("id", data.ParkingRecordId);
            await updateRecordCmd.ExecuteNonQueryAsync();

            // 6. Освобождаем место
            using var updateSpotCmd = new NpgsqlCommand(
                @"UPDATE ""spots"" SET spot_status_id = 2 WHERE id = @id",
                connection);
            updateSpotCmd.Parameters.AddWithValue("id", spotId);
            await updateSpotCmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            return data.ParkingRecordId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}