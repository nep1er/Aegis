using Aegis.Data;
using Aegis.Models;
using Npgsql;

namespace Aegis.Services.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<UserDisplayModel>> GetAllUsersAsync()
    {
        var users = new List<UserDisplayModel>();

        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"SELECT 
                u.id,
                u.login,
                u.full_name,
                r.name as role_name,
                COALESCE(
                    string_agg(DISTINCT p.city || ', ' || p.street || ', ' || p.building, '; '),
                    'Не назначена'
                ) as parking_address
              FROM ""users"" u
              JOIN ""roles"" r ON u.role_id = r.id
              LEFT JOIN ""operators"" o ON u.id = o.user_id
              LEFT JOIN ""parkings"" p ON o.parking_id = p.id
              GROUP BY u.id, u.login, u.full_name, r.name
              ORDER BY u.full_name",
            connection);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            users.Add(new UserDisplayModel
            {
                Id = reader.GetInt32(0),
                Login = reader.GetString(1),
                FullName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                RoleName = reader.GetString(3),
                ParkingAddress = reader.IsDBNull(4) ? "" : reader.GetString(4)
            });
        }

        return users;
    }

    public async Task<UserDetailsModel?> GetUserDetailsAsync(int userId)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"SELECT 
            u.id,
            u.login,
            u.full_name,
            u.phone_number,
            u.role_id,
            r.name as role_name
          FROM ""users"" u
          JOIN ""roles"" r ON u.role_id = r.id
          WHERE u.id = @userId",
            connection);
        command.Parameters.AddWithValue("userId", userId);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var user = new UserDetailsModel
            {
                Id = reader.GetInt32(0),
                Login = reader.GetString(1),
                FullName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                PhoneNumber = reader.IsDBNull(3) ? "" : reader.GetString(3),
                RoleId = reader.GetInt32(4),
                RoleName = reader.GetString(5)
            };

            await reader.CloseAsync();

            // Получаем ID и адреса назначенных парковок
            using var parkingCmd = new NpgsqlCommand(
                @"SELECT o.parking_id, p.city || ', ' || p.street || ', ' || p.building as address
              FROM ""operators"" o
              JOIN ""parkings"" p ON o.parking_id = p.id
              WHERE o.user_id = @userId",
                connection);
            parkingCmd.Parameters.AddWithValue("userId", userId);

            using var parkingReader = await parkingCmd.ExecuteReaderAsync();
            while (await parkingReader.ReadAsync())
            {
                user.ParkingIds.Add(parkingReader.GetInt32(0));
                user.ParkingAddresses.Add(parkingReader.GetString(1));
            }

            return user;
        }

        return null;
    }

    public async Task<int> CreateUserAsync(CreateUserData userData)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // Хешируем пароль
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(userData.Password, workFactor: 12);

            int userId;
            using var insertCmd = new NpgsqlCommand(
                @"INSERT INTO ""users"" (login, password, full_name, phone_number, role_id) 
                  VALUES (@login, @password, @fullName, @phoneNumber, @roleId) 
                  RETURNING id",
                connection);
            insertCmd.Parameters.AddWithValue("login", userData.Login);
            insertCmd.Parameters.AddWithValue("password", hashedPassword);
            insertCmd.Parameters.AddWithValue("fullName", string.IsNullOrWhiteSpace(userData.FullName) ? DBNull.Value : userData.FullName);
            insertCmd.Parameters.AddWithValue("phoneNumber", string.IsNullOrWhiteSpace(userData.PhoneNumber) ? DBNull.Value : userData.PhoneNumber);
            insertCmd.Parameters.AddWithValue("roleId", userData.RoleId);

            userId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

            // Назначаем парковки
            foreach (var parkingId in userData.ParkingIds)
            {
                using var assignCmd = new NpgsqlCommand(
                    @"INSERT INTO ""operators"" (user_id, parking_id) 
                      VALUES (@userId, @parkingId)",
                    connection);
                assignCmd.Parameters.AddWithValue("userId", userId);
                assignCmd.Parameters.AddWithValue("parkingId", parkingId);
                await assignCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return userId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateUserAsync(UpdateUserData userData)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // Обновляем роль
            using var updateCmd = new NpgsqlCommand(
                @"UPDATE ""users"" SET role_id = @roleId WHERE id = @userId",
                connection);
            updateCmd.Parameters.AddWithValue("roleId", userData.RoleId);
            updateCmd.Parameters.AddWithValue("userId", userData.UserId);
            await updateCmd.ExecuteNonQueryAsync();

            // Удаляем старые назначения
            using var deleteCmd = new NpgsqlCommand(
                @"DELETE FROM ""operators"" WHERE user_id = @userId",
                connection);
            deleteCmd.Parameters.AddWithValue("userId", userData.UserId);
            await deleteCmd.ExecuteNonQueryAsync();

            // Добавляем новые назначения
            foreach (var parkingId in userData.ParkingIds)
            {
                using var assignCmd = new NpgsqlCommand(
                    @"INSERT INTO ""operators"" (user_id, parking_id) 
                      VALUES (@userId, @parkingId)",
                    connection);
                assignCmd.Parameters.AddWithValue("userId", userData.UserId);
                assignCmd.Parameters.AddWithValue("parkingId", parkingId);
                await assignCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteUserAsync(int userId)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // Удаляем назначения парковок
            using var deleteOperatorsCmd = new NpgsqlCommand(
                @"DELETE FROM ""operators"" WHERE user_id = @userId",
                connection);
            deleteOperatorsCmd.Parameters.AddWithValue("userId", userId);
            await deleteOperatorsCmd.ExecuteNonQueryAsync();

            // Удаляем пользователя
            using var deleteUserCmd = new NpgsqlCommand(
                @"DELETE FROM ""users"" WHERE id = @userId",
                connection);
            deleteUserCmd.Parameters.AddWithValue("userId", userId);
            await deleteUserCmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
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

    public async Task<IEnumerable<RoleModel>> GetAllRolesAsync()
    {
        var roles = new List<RoleModel>();

        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"SELECT id, name FROM ""roles"" ORDER BY id",
            connection);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            roles.Add(new RoleModel
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            });
        }

        return roles;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string newPassword)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);

        using var command = new NpgsqlCommand(
            @"UPDATE ""users"" SET password = @password WHERE id = @userId",
            connection);
        command.Parameters.AddWithValue("password", hashedPassword);
        command.Parameters.AddWithValue("userId", userId);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }
}