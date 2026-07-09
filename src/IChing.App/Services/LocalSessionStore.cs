using Microsoft.Data.Sqlite;
using System.Text.Json;
using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Readings;

namespace IChing.App.Services;

/// <summary>
/// App 本地 SQLite session 存储；FIFO 保留最近 10 个 session。
/// </summary>
public sealed class LocalSessionStore
{
    public const int MaxSessions = 10;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _connectionString;

    public LocalSessionStore()
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, "iching-sessions.db");
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ConnectionString;
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

    public FollowUpSessionSeed? GetFollowUpSeed(string sessionId)
    {
        using var conn = Open();
        using var sessionCmd = conn.CreateCommand();
        sessionCmd.CommandText =
            "SELECT domain, tier FROM sessions WHERE session_id = $id LIMIT 1;";
        sessionCmd.Parameters.AddWithValue("$id", sessionId);
        using var reader = sessionCmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var domain = reader.GetString(0);
        var tier = reader.GetInt32(1);
        reader.Close();

        using var exCmd = conn.CreateCommand();
        exCmd.CommandText =
            """
            SELECT exchange_id, input_json, output_json FROM exchanges
            WHERE session_id = $id AND mode = 'initial'
            ORDER BY created_at ASC LIMIT 1;
            """;
        exCmd.Parameters.AddWithValue("$id", sessionId);
        using var exReader = exCmd.ExecuteReader();
        if (!exReader.Read())
        {
            return null;
        }

        var initialId = exReader.GetString(0);
        var input = FollowUpExchangeBuilder.DeserializeInput(exReader.GetString(1));
        var outputJson = exReader.IsDBNull(2) ? null : exReader.GetString(2);
        exReader.Close();
        if (input is null)
        {
            return null;
        }

        using var lastCmd = conn.CreateCommand();
        lastCmd.CommandText =
            "SELECT exchange_id FROM exchanges WHERE session_id = $id ORDER BY created_at DESC LIMIT 1;";
        lastCmd.Parameters.AddWithValue("$id", sessionId);
        var lastId = lastCmd.ExecuteScalar() as string ?? initialId;

        return new FollowUpSessionSeed(domain, tier, input, outputJson, initialId, lastId);
    }

    public void AppendExchange(string sessionId, StoredExchange exchange) =>
        AppendExchange(Open(), exchange with { SessionId = sessionId });

    private static void InsertSession(
        SqliteConnection conn,
        string sessionId,
        string domain,
        int tier,
        string chartJson,
        string? factsJson)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO sessions(session_id, domain, tier, chart_json, facts_json, created_at)
            VALUES ($id, $domain, $tier, $chart, $facts, $created);
            """;
        cmd.Parameters.AddWithValue("$id", sessionId);
        cmd.Parameters.AddWithValue("$domain", domain);
        cmd.Parameters.AddWithValue("$tier", tier);
        cmd.Parameters.AddWithValue("$chart", chartJson);
        cmd.Parameters.AddWithValue("$facts", (object?)factsJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$created", DateTimeOffset.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    private static void AppendExchange(SqliteConnection conn, StoredExchange exchange)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO exchanges(exchange_id, session_id, parent_id, mode, tier, input_json, output_json, created_at)
            VALUES ($id, $session, $parent, $mode, $tier, $input, $output, $created);
            """;
        cmd.Parameters.AddWithValue("$id", exchange.ExchangeId);
        cmd.Parameters.AddWithValue("$session", exchange.SessionId);
        cmd.Parameters.AddWithValue("$parent", (object?)exchange.ParentExchangeId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$mode", exchange.Mode);
        cmd.Parameters.AddWithValue("$tier", exchange.Tier);
        cmd.Parameters.AddWithValue("$input", exchange.InputJson);
        cmd.Parameters.AddWithValue("$output", (object?)exchange.OutputJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$created", exchange.CreatedAt.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    private void EnsureSchema()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS sessions(
              session_id TEXT PRIMARY KEY,
              domain TEXT NOT NULL,
              tier INTEGER NOT NULL,
              chart_json TEXT NOT NULL,
              facts_json TEXT,
              created_at TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS exchanges(
              exchange_id TEXT PRIMARY KEY,
              session_id TEXT NOT NULL,
              parent_id TEXT,
              mode TEXT NOT NULL,
              tier INTEGER NOT NULL,
              input_json TEXT NOT NULL,
              output_json TEXT,
              created_at TEXT NOT NULL);
            """;
        cmd.ExecuteNonQuery();
        EnsureLabSessionColumn(conn);
    }

    private static void EnsureLabSessionColumn(SqliteConnection conn)
    {
        using var info = conn.CreateCommand();
        info.CommandText = "PRAGMA table_info(sessions);";
        using var reader = info.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), "lab_session_id", StringComparison.Ordinal))
            {
                return;
            }
        }

        using var alter = conn.CreateCommand();
        alter.CommandText = "ALTER TABLE sessions ADD COLUMN lab_session_id TEXT;";
        alter.ExecuteNonQuery();
    }

    private void TrimSessions(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            $"""
            DELETE FROM exchanges WHERE session_id IN (
              SELECT session_id FROM sessions ORDER BY created_at DESC LIMIT -1 OFFSET {MaxSessions}
            );
            DELETE FROM sessions WHERE session_id IN (
              SELECT session_id FROM sessions ORDER BY created_at DESC LIMIT -1 OFFSET {MaxSessions}
            );
            """;
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
    string InputJson,
    string? OutputJson,
    DateTimeOffset CreatedAt);

public sealed record FollowUpSessionSeed(
    string Domain,
    int Tier,
    ExchangeInput Input,
    string? InitialOutputJson,
    string InitialExchangeId,
    string LastExchangeId);

public sealed record FollowUpChatArgs(string Title, string Domain, string SessionId);
