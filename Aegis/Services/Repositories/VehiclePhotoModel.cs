
public class VehiclePhotoModel
{
    public int Id { get; set; }
    public byte[] PhotoData { get; set; } = Array.Empty<byte>();
    public string? Description { get; set; }
}

