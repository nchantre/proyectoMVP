namespace ProyectMVP.Infrastructure.Persistence.Entities;

public sealed class SightingEntity
{
    public long SightingId { get; set; }
    public Guid DeviceId { get; set; }
    public long VehicleId { get; set; }
    public DateTime SeenAtUtc { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int CountryId { get; set; }
    public int? CityId { get; set; }
    public decimal? Confidence { get; set; }
    public bool IsPotentialMatch { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public ICollection<SightingEvidenceEntity> Evidences { get; set; } = new List<SightingEvidenceEntity>();
}
