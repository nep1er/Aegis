using Aegis.Models;

namespace Aegis.Services.Repositories;

public interface IUserRepository
{
    Task<IEnumerable<UserDisplayModel>> GetAllUsersAsync();
    Task<UserDetailsModel?> GetUserDetailsAsync(int userId);
    Task<int> CreateUserAsync(CreateUserData userData);
    Task UpdateUserAsync(UpdateUserData userData);
    Task DeleteUserAsync(int userId);
    Task<IEnumerable<ParkingDisplayModel>> GetAllParkingsAsync();
    Task<IEnumerable<RoleModel>> GetAllRolesAsync();
    Task<bool> ChangePasswordAsync(int userId, string newPassword);
}

public class UserDisplayModel
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string ParkingAddress { get; set; } = string.Empty;
}


public class CreateUserData
{
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int RoleId { get; set; }
    public List<int> ParkingIds { get; set; } = new();
}

public class UpdateUserData
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public List<int> ParkingIds { get; set; } = new();
}

public class RoleModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}