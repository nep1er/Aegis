using Aegis.Data;
using Aegis.Data.Entities;
using Npgsql;

namespace Aegis.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    public User? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;

    public AuthService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> LoginAsync(string login, string password)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(
            @"SELECT u.id, u.login, u.password, u.role_id, u.phone_number, u.full_name, r.name as role_name
              FROM ""users"" u
              JOIN ""roles"" r ON u.role_id = r.id
              WHERE u.login = @login",
            connection);
        command.Parameters.AddWithValue("login", login);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var user = new User
            {
                Id = reader.GetInt32(0),
                Login = reader.GetString(1),
                Password = reader.GetString(2),
                RoleId = reader.GetInt32(3),
                PhoneNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
                FullName = reader.IsDBNull(5) ? null : reader.GetString(5),
                Role = new Role
                {
                    Id = reader.GetInt32(3),
                    Name = reader.GetString(6)
                }
            };

            // Проверяем хеш пароля
            if (BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                CurrentUser = user;
                return user;
            }
        }

        return null;
    }

    public void Logout()
    {
        CurrentUser = null;
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }
}