using Aegis.Data.Entities;

namespace Aegis.Services;

public interface IAuthService
{
    Task<User?> LoginAsync(string login, string password);
    User? CurrentUser { get; }
    bool IsAuthenticated { get; }
    void Logout();
    string HashPassword(string password);
}