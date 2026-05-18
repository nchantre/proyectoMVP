using System.ComponentModel.DataAnnotations;

namespace ProyectMVP.Api.Contracts;

public sealed class RegisterSightingBatchRequest
{
    [MinLength(1)]
    public List<RegisterSightingItemRequest> Items { get; set; } = [];
}

public sealed class RegisterSightingItemRequest
{
    [Required, MaxLength(20)]
    public string Plate { get; set; } = string.Empty;

    public DateTime SeenAtUtc { get; set; }

    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Range(-180, 180)]
    public decimal Longitude { get; set; }

    [Range(0, 100)]
    public decimal? Confidence { get; set; }

    [MaxLength(500)]
    public string? EvidenceUrl { get; set; }

    [MaxLength(20)]
    public string EvidenceType { get; set; } = "IMAGE";
}
