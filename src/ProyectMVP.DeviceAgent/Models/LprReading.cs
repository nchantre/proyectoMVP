namespace ProyectMVP.DeviceAgent.Models;

public sealed record LprReading(
    string Plate,
    DateTime SeenAtUtc,
    decimal Latitude,
    decimal Longitude,
    decimal Confidence,
    string EvidenceUrl);
