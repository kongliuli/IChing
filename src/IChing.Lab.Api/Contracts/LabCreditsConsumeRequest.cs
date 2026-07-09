namespace IChing.Lab.Api.Contracts;

public sealed record LabCreditsConsumeRequest(
    string ExchangeId,
    string? SessionId,
    string Domain,
    string Mode,
    int Tier = 1);
