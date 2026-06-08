public class UserDetailsModel
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public List<string> ParkingAddresses { get; set; } = new();
    public List<int> ParkingIds { get; set; } = new();
}