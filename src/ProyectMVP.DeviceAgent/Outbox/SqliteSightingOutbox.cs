using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using ProyectMVP.DeviceAgent.Models;
using ProyectMVP.DeviceAgent.Options;

namespace ProyectMVP.DeviceAgent.Outbox;

public sealed class SqliteSightingOutbox : ISightingOutbox, IDisposable
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteSightingOutbox> _logger;

    public SqliteSightingOutbox(IOptions<DeviceAgentOptions> options, ILogger<SqliteSightingOutbox> logger)
    {
        _logger = logger;
        var path = options.Value.OutboxPath;
        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, path);
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ConnectionString;
        _logger.LogInformation("Cola local SQLite: {Path}", path);
    }

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = """
            CREATE TABLE IF NOT EXISTS PendingSighting (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Plate TEXT NOT NULL,
                SeenAtUtc TEXT NOT NULL,
                Latitude REAL NOT NULL,
                Longitude REAL NOT NULL,
                Confidence REAL NOT NULL,
                EvidenceUrl TEXT,
                EvidenceType TEXT NOT NULL DEFAULT 'IMAGE',
                Status TEXT NOT NULL DEFAULT 'pending',
                CreatedAtUtc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_PendingSighting_Status ON PendingSighting(Status);
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task EnqueueAsync(LprReading reading, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO PendingSighting
            (Plate, SeenAtUtc, Latitude, Longitude, Confidence, EvidenceUrl, EvidenceType, Status, CreatedAtUtc)
            VALUES ($plate, $seenAt, $lat, $lng, $confidence, $evidenceUrl, 'IMAGE', 'pending', $createdAt);
            """;
        command.Parameters.AddWithValue("$plate", reading.Plate);
        command.Parameters.AddWithValue("$seenAt", reading.SeenAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$lat", reading.Latitude);
        command.Parameters.AddWithValue("$lng", reading.Longitude);
        command.Parameters.AddWithValue("$confidence", reading.Confidence);
        command.Parameters.AddWithValue("$evidenceUrl", reading.EvidenceUrl);
        command.Parameters.AddWithValue("$createdAt", DateTime.UtcNow.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PendingSighting>> GetPendingAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var list = new List<PendingSighting>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Plate, SeenAtUtc, Latitude, Longitude, Confidence, EvidenceUrl, EvidenceType
            FROM PendingSighting
            WHERE Status = 'pending'
            ORDER BY Id
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", limit);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new PendingSighting(
                reader.GetInt64(0),
                reader.GetString(1),
                DateTime.Parse(reader.GetString(2)),
                (decimal)reader.GetDouble(3),
                (decimal)reader.GetDouble(4),
                (decimal)reader.GetDouble(5),
                reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                reader.GetString(7)));
        }

        return list;
    }

    public async Task MarkSentAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
        {
            return;
        }

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var id in idList)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE PendingSighting SET Status = 'sent' WHERE Id = $id;";
            command.Parameters.AddWithValue("$id", id);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<int> CountPendingAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM PendingSighting WHERE Status = 'pending';";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public void Dispose()
    {
    }
}
