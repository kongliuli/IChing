using Microsoft.Data.Sqlite;
using System.Text.Json;
using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Readings;

namespace IChing.Client.Shared.Sessions;

/// <summary>
/// App 本地 SQLite session；数据库路径由宿主注入（避免依赖 MAUI FileSystem）。
/// </summary>
public sealed class LocalSessionStore
{
    public const int MaxSessions = 10;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _connectionString;

    public LocalSessionStore(string databasePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ConnectionString;
        EnsureSchema();
    }

    public string CreateSessionWithInitial(
        string domain,
        int tier,
        object chart,
        object? facts,
        ExchangeInput input,
        string? initialOutput)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var chartJson = JsonSerializer.Serialize(chart, JsonOptions);
        var factsJson = facts is null ? null : JsonSerializer.Serialize(facts, JsonOptions);
        using var conn = Open();
        InsertSession(conn, sessionId, domain, tier, chartJson, factsJson);
        AppendExchange(conn, new StoredExchange(
            Guid.NewGuid().ToString("N"),
            sessionId,
            null,
            "initial",
            tier,
            FollowUpExchangeBuilder.SerializeInput(input),
            initialOutput,
            DateTimeOffset.UtcNow));
        TrimSessions(conn);
        return sessionId;
    }

    public object? GetSessionChart(string sessionId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT domain, chart_json FROM sessions WHERE session_id = $id LIMIT 1;";
        cmd.Parameters.AddWithValue("$id", sessionId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return SessionChartLoader.Deserialize(reader.GetString(0), reader.GetString(1));
    }

    public void SetLabSessionId(string sessionId, string labSessionId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE sessions SET lab_session_id = $lab WHERE session_id = $id;";
        cmd.Parameters.AddWithValue("$lab", labSessionId);
        cmd.Parameters.AddWithValue("$id", sessionId);
        cmd.ExecuteNonQuery();
    }

    public string? GetLabSessionId(string sessionId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT lab_session_id FROM sessions WHERE session_id = $id LIMIT 1;";
        cmd.Parameters.AddWithValue("$id", sessionId);
        return cmd.ExecuteScalar() as string;
    }

    public FollowUpSessionSeed? GetFollowUpSeed(string sessionId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT s.domain, s.tier, e.input_json, e.output_text
            FROM sessions s
            JOIN exchanges e ON e.session_id = s.session_id
            WHERE s.session_id = $id AND e.mode = 'initial'
            ORDER BY e.created_at ASC
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("$id", sessionId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var input = FollowUpExchangeBuilder.DeserializeInput(reader.GetString(2))
            ?? new ExchangeInput(
                null,
                null,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<ExchangePluginContext>());
        return new FollowUpSessionSeed(
            sessionId,
            reader.GetString(0),
            reader.GetInt32(1),
            input,
            reader.IsDBNull(3) ? null : reader.GetString(3));
    }

    public void AppendExchange(string sessionId, StoredExchange exchange)
    {
        using var conn = Open();
        AppendExchange(conn, exchange with { SessionId = sessionId });
    }

    private void EnsureSchema()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS sessions (
              session_id TEXT PRIMARY KEY,
              domain TEXT NOT NULL,
              tier INTEGER NOT NULL,
              chart_json TEXT NOT NULL,
              facts_json TEXT,
              lab_session_id TEXT,
              created_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS exchanges (
              exchange_id TEXT PRIMARY KEY,
              session_id TEXT NOT NULL,
              parent_exchange_id TEXT,
              mode TEXT NOT NULL,
              tier INTEGER NOT NULL,
              input_json TEXT,
              output_text TEXT,
              created_at TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private static void InsertSession(
        SqliteConnection conn,
        string sessionId,
        string domain,
        int tier,
        string chartJson,
        string? factsJson)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO sessions(session_id, domain, tier, chart_json, facts_json, created_at)
            VALUES($id, $domain, $tier, $chart, $facts, $at);
            """;
        cmd.Parameters.AddWithValue("$id", sessionId);
        cmd.Parameters.AddWithValue("$domain", domain);
        cmd.Parameters.AddWithValue("$tier", tier);
        cmd.Parameters.AddWithValue("$chart", chartJson);
        cmd.Parameters.AddWithValue("$facts", (object?)factsJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    private static void AppendExchange(SqliteConnection conn, StoredExchange exchange)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO exchanges(exchange_id, session_id, parent_exchange_id, mode, tier, input_json, output_text, created_at)
            VALUES($id, $sid, $parent, $mode, $tier, $input, $output, $at);
            """;
        cmd.Parameters.AddWithValue("$id", exchange.ExchangeId);
        cmd.Parameters.AddWithValue("$sid", exchange.SessionId);
        cmd.Parameters.AddWithValue("$parent", (object?)exchange.ParentExchangeId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$mode", exchange.Mode);
        cmd.Parameters.AddWithValue("$tier", exchange.Tier);
        cmd.Parameters.AddWithValue("$input", (object?)exchange.InputJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$output", (object?)exchange.OutputText ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$at", exchange.CreatedAt.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    private static void TrimSessions(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            DELETE FROM exchanges WHERE session_id IN (
              SELECT session_id FROM sessions ORDER BY created_at DESC LIMIT -1 OFFSET $keep);
            DELETE FROM sessions WHERE session_id IN (
              SELECT session_id FROM sessions ORDER BY created_at DESC LIMIT -1 OFFSET $keep);
            """;
        cmd.Parameters.AddWithValue("$keep", MaxSessions);
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }
}

public sealed record StoredExchange(
    string ExchangeId,
    string SessionId,
    string? ParentExchangeId,
    string Mode,
    int Tier,
    string? InputJson,
    string? OutputText,
    DateTimeOffset CreatedAt);

public sealed record FollowUpSessionSeed(
    string SessionId,
    string Domain,
    int Tier,
    ExchangeInput Input,
    string? InitialOutput);

public sealed record FollowUpChatArgs(
    string SessionId,
    string Domain,
    int Tier,
    ExchangeInput Input,
    string? InitialOutput,
    object? Chart);
