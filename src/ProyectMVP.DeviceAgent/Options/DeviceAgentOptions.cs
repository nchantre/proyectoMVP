namespace ProyectMVP.DeviceAgent.Options;

public sealed class DeviceAgentOptions
{
    public const string SectionName = "DeviceAgent";

    public string ApiBaseUrl { get; set; } = "http://localhost:5290";

    public Guid DeviceId { get; set; } = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public string ApiKey { get; set; } = "DEMO_KEY";

    public double BaseLatitude { get; set; } = 6.244203;

    public double BaseLongitude { get; set; } = -75.581212;

    public double LocationJitterDegrees { get; set; } = 0.012;

    public int CaptureIntervalSeconds { get; set; } = 25;

    public int SyncIntervalSeconds { get; set; } = 10;

    public int SyncBatchSize { get; set; } = 5;

    /// <summary>Si false, solo encola localmente (simula sin GSM).</summary>
    public bool SyncEnabled { get; set; } = true;

    public string[] Plates { get; set; } = ["ABC123", "XYZ789", "DEF456"];

    public string EvidenceUrlTemplate { get; set; } =
        "https://storage.demo/evidence/{plate}_{timestamp}.jpg";

    public string OutboxPath { get; set; } = "data/device-outbox.db";
}
