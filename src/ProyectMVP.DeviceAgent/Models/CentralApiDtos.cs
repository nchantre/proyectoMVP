namespace ProyectMVP.DeviceAgent.Models;

public sealed class RegisterSightingBatchRequest
{
    public List<RegisterSightingItemRequest> Items { get; set; } = [];
}

public sealed class RegisterSightingItemRequest
{
    public string Plate { get; set; } = string.Empty;

    public DateTime SeenAtUtc { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public decimal? Confidence { get; set; }

    public string? EvidenceUrl { get; set; }

    public string EvidenceType { get; set; } = "IMAGE";
}

public sealed class RegisterSightingBatchResponse
{
    public int Accepted { get; set; }

    public int Duplicates { get; set; }
}
