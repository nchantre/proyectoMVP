namespace ProyectMVP.Domain.Entities;

public sealed class SightingEvidence
{
    public long SightingEvidenceId { get; set; }
    public long SightingId { get; set; }
    public string EvidenceType { get; set; } = "IMAGE";
    public string StorageUrl { get; set; } = string.Empty;
}
