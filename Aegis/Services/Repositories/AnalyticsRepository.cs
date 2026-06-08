using Aegis.Data;
using Aegis.Models;
using Npgsql;

namespace Aegis.Services.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly AppDbContext _dbContext;

    public AnalyticsRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<MonthlyParkingRevenue>> GetMonthlyRevenueByParkingIdAsync(int? parkingId, DateTime? dateFrom, DateTime? dateTo)
    {
        var revenues = new List<MonthlyParkingRevenue>();
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        var toDate = dateTo?.AddDays(1);

        string sql;

        if (parkingId.HasValue)
        {
            sql = @"
            SELECT 
                p.id,
                p.city || ', ' || p.street || ', ' || p.building,
                EXTRACT(YEAR FROM rh.release_date)::int,
                EXTRACT(MONTH FROM rh.release_date)::int,
                TO_CHAR(rh.release_date, 'Month'),
                ROUND(SUM(rh.storage_fee + rh.tow_fine)::numeric, 2)
            FROM ""releasehistory"" rh
            JOIN ""parkingrecords"" pr ON rh.parking_record_id = pr.id
            JOIN ""spots"" s ON pr.spot_id = s.id
            JOIN ""parkings"" p ON s.parking_id = p.id
            WHERE p.id = @parkingId
              AND (@dateFrom IS NULL OR rh.release_date >= @dateFrom)
              AND (@dateTo IS NULL OR rh.release_date < @dateTo)
            GROUP BY p.id, p.city, p.street, p.building, 
                     EXTRACT(YEAR FROM rh.release_date)::int, 
                     EXTRACT(MONTH FROM rh.release_date)::int, 
                     TO_CHAR(rh.release_date, 'Month')
            ORDER BY EXTRACT(YEAR FROM rh.release_date)::int, EXTRACT(MONTH FROM rh.release_date)::int";
        }
        else
        {
            sql = @"
            SELECT 
                0,
                'Все парковки',
                EXTRACT(YEAR FROM rh.release_date)::int,
                EXTRACT(MONTH FROM rh.release_date)::int,
                TO_CHAR(rh.release_date, 'Month'),
                ROUND(SUM(rh.storage_fee + rh.tow_fine)::numeric, 2)
            FROM ""releasehistory"" rh
            JOIN ""parkingrecords"" pr ON rh.parking_record_id = pr.id
            WHERE (@dateFrom IS NULL OR rh.release_date >= @dateFrom)
              AND (@dateTo IS NULL OR rh.release_date < @dateTo)
            GROUP BY EXTRACT(YEAR FROM rh.release_date)::int, 
                     EXTRACT(MONTH FROM rh.release_date)::int, 
                     TO_CHAR(rh.release_date, 'Month')
            ORDER BY EXTRACT(YEAR FROM rh.release_date)::int, EXTRACT(MONTH FROM rh.release_date)::int";
        }

        using var command = new NpgsqlCommand(sql, connection);

        if (parkingId.HasValue)
            command.Parameters.AddWithValue("parkingId", parkingId.Value);
        if (dateFrom.HasValue)
            command.Parameters.AddWithValue("dateFrom", dateFrom.Value);
        else
            command.Parameters.AddWithValue("dateFrom", DBNull.Value);
        if (toDate.HasValue)
            command.Parameters.AddWithValue("dateTo", toDate.Value);
        else
            command.Parameters.AddWithValue("dateTo", DBNull.Value);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            revenues.Add(new MonthlyParkingRevenue
            {
                ParkingId = reader.GetInt32(0),
                ParkingAddress = reader.GetString(1),
                Year = reader.GetInt32(2),
                Month = reader.GetInt32(3),
                MonthName = reader.GetString(4).Trim(),
                Revenue = reader.GetDecimal(5)
            });
        }

        return revenues;
    }

    public async Task<IEnumerable<ParkingRevenue>> GetRevenueByParkingAsync(DateTime? dateFrom, DateTime? dateTo)
    {
        var revenues = new List<ParkingRevenue>();
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        var toDate = dateTo?.AddDays(1);

        var sql = @"
            SELECT 
                p.id,
                p.city || ', ' || p.street || ', ' || p.building,
                ROUND(SUM(rh.storage_fee + rh.tow_fine)::numeric, 2)
            FROM ""releasehistory"" rh
            JOIN ""parkingrecords"" pr ON rh.parking_record_id = pr.id
            JOIN ""spots"" s ON pr.spot_id = s.id
            JOIN ""parkings"" p ON s.parking_id = p.id
            WHERE (@dateFrom IS NULL OR rh.release_date >= @dateFrom)
              AND (@dateTo IS NULL OR rh.release_date < @dateTo)
            GROUP BY p.id, p.city, p.street, p.building
            ORDER BY ROUND(SUM(rh.storage_fee + rh.tow_fine)::numeric, 2) DESC";

        using var command = new NpgsqlCommand(sql, connection);
        if (dateFrom.HasValue)
            command.Parameters.AddWithValue("dateFrom", dateFrom.Value);
        else
            command.Parameters.AddWithValue("dateFrom", DBNull.Value);
        if (toDate.HasValue)
            command.Parameters.AddWithValue("dateTo", toDate.Value);
        else
            command.Parameters.AddWithValue("dateTo", DBNull.Value);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            revenues.Add(new ParkingRevenue
            {
                ParkingId = reader.GetInt32(0),
                ParkingAddress = reader.GetString(1),
                TotalRevenue = reader.GetDecimal(2)
            });
        }

        return revenues;
    }

    public async Task<IEnumerable<VehicleTypeStatistics>> GetVehicleTypeStatisticsAsync(DateTime? dateFrom, DateTime? dateTo)
    {
        var statistics = new List<VehicleTypeStatistics>();
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        var toDate = dateTo?.AddDays(1);

        var sql = @"
            SELECT 
                vt.id,
                vt.type,
                COUNT(pr.id)
            FROM ""parkingrecords"" pr
            JOIN ""vehicletypes"" vt ON pr.vehicle_type_id = vt.id
            WHERE (@dateFrom IS NULL OR pr.admission_date >= @dateFrom)
              AND (@dateTo IS NULL OR pr.admission_date < @dateTo)
            GROUP BY vt.id, vt.type
            ORDER BY COUNT(pr.id) DESC";

        using var command = new NpgsqlCommand(sql, connection);
        if (dateFrom.HasValue)
            command.Parameters.AddWithValue("dateFrom", dateFrom.Value);
        else
            command.Parameters.AddWithValue("dateFrom", DBNull.Value);
        if (toDate.HasValue)
            command.Parameters.AddWithValue("dateTo", toDate.Value);
        else
            command.Parameters.AddWithValue("dateTo", DBNull.Value);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            statistics.Add(new VehicleTypeStatistics
            {
                VehicleTypeId = reader.GetInt32(0),
                VehicleTypeName = reader.GetString(1),
                Count = reader.GetInt32(2)
            });
        }

        return statistics;
    }

    public async Task<ParkingStatistics> GetParkingStatisticsAsync(int? parkingId)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        string sql;

        if (parkingId.HasValue)
        {
            sql = @"
                SELECT 
                    COALESCE(SUM(CASE WHEN pr.vehicle_status_id = 1 THEN 1 ELSE 0 END), 0),
                    COALESCE(SUM(CASE WHEN pr.vehicle_status_id = 2 THEN 1 ELSE 0 END), 0),
                    COUNT(*)
                FROM ""parkingrecords"" pr
                JOIN ""spots"" s ON pr.spot_id = s.id
                WHERE s.parking_id = @parkingId";
        }
        else
        {
            sql = @"
                SELECT 
                    COALESCE(SUM(CASE WHEN pr.vehicle_status_id = 1 THEN 1 ELSE 0 END), 0),
                    COALESCE(SUM(CASE WHEN pr.vehicle_status_id = 2 THEN 1 ELSE 0 END), 0),
                    COUNT(*)
                FROM ""parkingrecords"" pr";
        }

        using var command = new NpgsqlCommand(sql, connection);
        if (parkingId.HasValue)
            command.Parameters.AddWithValue("parkingId", parkingId.Value);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ParkingStatistics
            {
                CurrentlyParked = reader.GetInt32(0),
                Released = reader.GetInt32(1),
                TotalReceived = reader.GetInt32(2)
            };
        }

        return new ParkingStatistics();
    }

    public async Task<IEnumerable<CityStatistics>> GetCityStatisticsAsync(DateTime? dateFrom, DateTime? dateTo)
    {
        var statistics = new List<CityStatistics>();
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        var toDate = dateTo?.AddDays(1);

        var sql = @"
            SELECT 
                p.city,
                COUNT(DISTINCT p.id),
                COALESCE(ROUND(SUM(rh.storage_fee + rh.tow_fine)::numeric, 2), 0),
                COUNT(DISTINCT pr.id)
            FROM ""parkings"" p
            LEFT JOIN ""spots"" s ON p.id = s.parking_id
            LEFT JOIN ""parkingrecords"" pr ON s.id = pr.spot_id
            LEFT JOIN ""releasehistory"" rh ON pr.id = rh.parking_record_id
                AND (@dateFrom IS NULL OR rh.release_date >= @dateFrom)
                AND (@dateTo IS NULL OR rh.release_date < @dateTo)
            GROUP BY p.city
            ORDER BY COALESCE(ROUND(SUM(rh.storage_fee + rh.tow_fine)::numeric, 2), 0) DESC";

        using var command = new NpgsqlCommand(sql, connection);
        if (dateFrom.HasValue)
            command.Parameters.AddWithValue("dateFrom", dateFrom.Value);
        else
            command.Parameters.AddWithValue("dateFrom", DBNull.Value);
        if (toDate.HasValue)
            command.Parameters.AddWithValue("dateTo", toDate.Value);
        else
            command.Parameters.AddWithValue("dateTo", DBNull.Value);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            statistics.Add(new CityStatistics
            {
                City = reader.GetString(0),
                TotalParkings = reader.GetInt32(1),
                TotalRevenue = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2),
                TotalVehicles = reader.GetInt32(3)
            });
        }

        return statistics;
    }

    public async Task<IEnumerable<CityParkingCount>> GetCityParkingCountsAsync()
    {
        var counts = new List<CityParkingCount>();
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"SELECT city, COUNT(*) FROM ""parkings"" GROUP BY city ORDER BY COUNT(*) DESC",
            connection);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            counts.Add(new CityParkingCount
            {
                City = reader.GetString(0),
                ParkingCount = reader.GetInt32(1)
            });
        }

        return counts;
    }
}