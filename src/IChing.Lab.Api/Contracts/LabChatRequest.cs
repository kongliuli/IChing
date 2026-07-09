namespace IChing.Lab.Api.Contracts;

using IChing.Lab.Abstractions.Readings;

public sealed record LabChatRequest(
    string Mode,
    string? Domain = null,
    int Tier = 1,
    string? SessionId = null,
    string? UserQuestion = null,
    int? MaxTokens = null,
    ExchangeInput? Input = null,
    string? InitialOutput = null,
    BaziReadRequest? Bazi = null,
    LiuyaoReadRequest? Liuyao = null,
    TarotReadRequest? Tarot = null);
