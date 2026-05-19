namespace ProyectMVP.DeviceAgent.Models;

public sealed record PendingSighting(
    long Id,
    string Plate,
    DateTime SeenAtUtc,
    decimal Latitude,
    decimal Longitude,
    decimal Confidence,
    string EvidenceUrl,
    string EvidenceType);
