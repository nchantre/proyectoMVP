namespace ProyectMVP.Infrastructure.Persistence.Entities;

public sealed class SightingEvidenceEntity
{
    public long SightingEvidenceId { get; set; }
    public long SightingId { get; set; }
    public string EvidenceType { get; set; } = string.Empty;
    public string StorageUrl { get; set; } = string.Empty;
    public string? Sha256 { get; set; }
    public DateTime? CapturedAtUtc { get; set; }
    public SightingEntity? Sighting { get; set; }
}
