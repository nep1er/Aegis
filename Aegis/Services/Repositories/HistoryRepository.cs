using Aegis.Data;
using Npgsql;
using Aegis.Models;
namespace Aegis.Services.Repositories;

public class HistoryRepository : IHistoryRepository
{
    private readonly AppDbContext _dbContext;

    public HistoryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<HistoryItemModel>> GetHistoryAsync(HistoryFilter filter)
    {
        var items = new List<HistoryItemModel>();

        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        var sql = new System.Text.StringBuilder(@"
            SELECT 
                pr.id,
                CASE WHEN pr.vehicle_status_id = 1 THEN 'Приёмка' ELSE 'Выдача' END as operation_type,
                CASE WHEN pr.vehicle_status_id = 1 THEN pr.admission_date ELSE rh.release_date END as operation_date,
                v.license_plate,
                vt.type as vehicle_type,
                s.number as spot_number,
                u.full_name as operator_name,
                rh.storage_fee + rh.tow_fine as amount
            FROM ""parkingrecords"" pr
            JOIN ""spots"" s ON pr.spot_id = s.id
            JOIN ""vehicles"" v ON pr.vehicle_id = v.id
            JOIN ""vehicletypes"" vt ON pr.vehicle_type_id = vt.id
            JOIN ""users"" u ON pr.operator_id = u.id
            LEFT JOIN ""releasehistory"" rh ON rh.parking_record_id = pr.id
            WHERE 1=1");

        if (!string.IsNullOrWhiteSpace(filter.LicensePlate))
        {
            sql.Append(" AND v.license_plate ILIKE @licensePlate");
        }

        if (!string.IsNullOrWhiteSpace(filter.Vin))
        {
            sql.Append(" AND v.vin ILIKE @vin");
        }

        if (filter.OperatorId.HasValue)
        {
            sql.Append(" AND u.id = @operatorId");
        }

        if (!string.IsNullOrWhiteSpace(filter.DocumentNumber))
        {
            sql.Append(" AND rh.document_number ILIKE @documentNumber");
        }

        if (filter.DocumentTypeId.HasValue)
        {
            sql.Append(" AND rh.document_type_id = @documentTypeId");
        }

        if (filter.DateFrom.HasValue)
        {
            sql.Append(" AND COALESCE(pr.admission_date, rh.release_date) >= @dateFrom");
        }

        if (filter.DateTo.HasValue)
        {
            sql.Append(" AND COALESCE(pr.admission_date, rh.release_date) <= @dateTo");
        }

        sql.Append(" ORDER BY operation_date DESC");

        using var command = new NpgsqlCommand(sql.ToString(), connection);

        if (!string.IsNullOrWhiteSpace(filter.LicensePlate))
            command.Parameters.AddWithValue("licensePlate", $"%{filter.LicensePlate}%");
        if (!string.IsNullOrWhiteSpace(filter.Vin))
            command.Parameters.AddWithValue("vin", $"%{filter.Vin}%");
        if (filter.OperatorId.HasValue)
            command.Parameters.AddWithValue("operatorId", filter.OperatorId.Value);
        if (!string.IsNullOrWhiteSpace(filter.DocumentNumber))
            command.Parameters.AddWithValue("documentNumber", $"%{filter.DocumentNumber}%");
        if (filter.DocumentTypeId.HasValue)
            command.Parameters.AddWithValue("documentTypeId", filter.DocumentTypeId.Value);
        if (filter.DateFrom.HasValue)
            command.Parameters.AddWithValue("dateFrom", filter.DateFrom.Value);
        if (filter.DateTo.HasValue)
            command.Parameters.AddWithValue("dateTo", filter.DateTo.Value);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            items.Add(new HistoryItemModel
            {
                ParkingRecordId = reader.GetInt32(0),
                OperationType = reader.GetString(1),
                OperationDate = reader.GetDateTime(2),
                LicensePlate = reader.GetString(3),
                VehicleType = reader.GetString(4),
                SpotNumber = reader.GetString(5),
                OperatorName = reader.IsDBNull(6) ? "" : reader.GetString(6),
                Amount = reader.IsDBNull(7) ? (decimal?)null : reader.GetDecimal(7)
            });
        }

        return items;
    }

    public async Task<HistoryDetailsModel?> GetHistoryDetailsAsync(int parkingRecordId)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(@"
            SELECT 
                pr.id,
                v.license_plate,
                v.vin,
                v.brand,
                v.model,
                vt.type,
                s.number,
                p.city || ', ' || p.street || ', ' || p.building,
                pr.admission_date,
                u1.full_name,
                rh.release_date,
                u2.full_name,
                dt.type,
                rh.document_number,
                rh.storage_fee,
                rh.tow_fine,
                rh.storage_fee + rh.tow_fine,
                pm.receipt_number
            FROM ""parkingrecords"" pr
            JOIN ""spots"" s ON pr.spot_id = s.id
            JOIN ""parkings"" p ON s.parking_id = p.id
            JOIN ""vehicles"" v ON pr.vehicle_id = v.id
            JOIN ""vehicletypes"" vt ON pr.vehicle_type_id = vt.id
            JOIN ""users"" u1 ON pr.operator_id = u1.id
            LEFT JOIN ""releasehistory"" rh ON rh.parking_record_id = pr.id
            LEFT JOIN ""users"" u2 ON rh.operator_id = u2.id
            LEFT JOIN ""documenttypes"" dt ON rh.document_type_id = dt.id
            LEFT JOIN ""payments"" pm ON pm.parking_record_id = pr.id
            WHERE pr.id = @id",
            connection);

        command.Parameters.AddWithValue("id", parkingRecordId);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new HistoryDetailsModel
            {
                ParkingRecordId = reader.GetInt32(0),
                LicensePlate = reader.GetString(1),
                Vin = reader.IsDBNull(2) ? null : reader.GetString(2),
                Brand = reader.IsDBNull(3) ? null : reader.GetString(3),
                Model = reader.IsDBNull(4) ? null : reader.GetString(4),
                VehicleType = reader.GetString(5),
                SpotNumber = reader.GetString(6),
                ParkingAddress = reader.GetString(7),
                AdmissionDate = reader.GetDateTime(8),
                AdmissionOperator = reader.IsDBNull(9) ? "" : reader.GetString(9),
                ReleaseDate = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10),
                ReleaseOperator = reader.IsDBNull(11) ? null : reader.GetString(11),
                DocumentType = reader.IsDBNull(12) ? null : reader.GetString(12),
                DocumentNumber = reader.IsDBNull(13) ? null : reader.GetString(13),
                StorageFee = reader.IsDBNull(14) ? (decimal?)null : reader.GetDecimal(14),
                TowFine = reader.IsDBNull(15) ? (decimal?)null : reader.GetDecimal(15),
                TotalAmount = reader.IsDBNull(16) ? (decimal?)null : reader.GetDecimal(16),
                ReceiptNumber = reader.IsDBNull(17) ? null : reader.GetString(17)
            };
        }

        return null;
    }
}