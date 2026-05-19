using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProyectMVP.DeviceAgent.Models;
using ProyectMVP.DeviceAgent.Options;

namespace ProyectMVP.DeviceAgent.Sync;

public sealed class CentralApiClient : ICentralApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly DeviceAgentOptions _options;
    private readonly ILogger<CentralApiClient> _logger;

    public CentralApiClient(
        HttpClient httpClient,
        IOptions<DeviceAgentOptions> options,
        ILogger<CentralApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RegisterSightingBatchResponse?> SendBatchAsync(
        IReadOnlyList<PendingSighting> items,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            return new RegisterSightingBatchResponse();
        }

        var request = new RegisterSightingBatchRequest
        {
            Items = items.Select(i => new RegisterSightingItemRequest
            {
                Plate = i.Plate,
                SeenAtUtc = i.SeenAtUtc,
                Latitude = i.Latitude,
                Longitude = i.Longitude,
                Confidence = i.Confidence,
                EvidenceUrl = i.EvidenceUrl,
                EvidenceType = i.EvidenceType
            }).ToList()
        };

        var url = $"api/v1/devices/{_options.DeviceId}/sightings/batch";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Add("X-Api-Key", _options.ApiKey);
        httpRequest.Content = JsonContent.Create(request, options: JsonOptions);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Sync falló HTTP {Status}: {Body}",
                (int)response.StatusCode,
                body.Length > 200 ? body[..200] : body);
            return null;
        }

        var result = JsonSerializer.Deserialize<RegisterSightingBatchResponse>(body, JsonOptions);
        _logger.LogInformation(
            "Sync OK: {Accepted} aceptado(s), {Duplicates} duplicado(s)",
            result?.Accepted ?? 0,
            result?.Duplicates ?? 0);

        return result;
    }
}
