namespace IChing.Lab.Api.Contracts;

public record BaziRequest(
    int Year, int Month, int Day, int Hour,
    int Minute = 0, int Second = 0,
    double? Longitude = null,
    string? City = null,
    int? Gender = null,
    int Sect = 1,
    int? FlowYear = null,
    int? FlowMonth = null,
    int? FlowCalendarMonth = null,
    int? FlowDay = null);

public record BaziReadRequest(
    int Year, int Month, int Day, int Hour,
    int Minute = 0, int Second = 0,
    double? Longitude = null,
    string? City = null,
    int? Gender = null,
    int Sect = 1,
    int? FlowYear = null,
    int? FlowMonth = null,
    int? FlowCalendarMonth = null,
    int? FlowDay = null,
    string? Focus = null,
    int? MaxTokens = null);

public record BaziInterpretRequest(
    int Year, int Month, int Day, int Hour,
    int Minute = 0, int Second = 0,
    double? Longitude = null,
    string? City = null,
    int? Gender = null,
    int Sect = 1,
    int? FlowYear = null,
    int? FlowMonth = null,
    int? FlowCalendarMonth = null,
    int? FlowDay = null,
    string? Focus = null,
    int? MaxTokens = null);

public record HePanRequest(BaziRequest PersonA, BaziRequest PersonB);

public record InterpretRequest(object Chart, string? Focus, int? MaxTokens);

public record TarotDrawRequest(string? SpreadId, string? Question, int? Seed);

public record LiuyaoReadRequest(
    string? Method,
    DateTimeOffset? At,
    int? Seed,
    string? Question,
    string? Focus,
    int? MaxTokens);

public record TarotReadRequest(string? SpreadId, string? Question, int? Seed, int? MaxTokens);

public record EngineHealthStatus(string EngineId, bool IsReady, bool IsDefault);

public record ChartEngineHealthStatus(string Domain, string EngineId, bool IsReady, bool IsDefault);
