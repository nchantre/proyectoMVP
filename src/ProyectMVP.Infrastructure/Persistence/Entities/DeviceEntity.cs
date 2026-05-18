namespace ProyectMVP.Infrastructure.Persistence.Entities;

public sealed class DeviceEntity
{
    public Guid DeviceId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public int CountryId { get; set; }
    public int? CityId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsActive { get; set; }
    public string ApiKeyHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
}
