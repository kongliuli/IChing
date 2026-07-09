using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Client;
using IChing.Lab.Core.Readings;

namespace IChing.App.Services;

public static class FollowUpSessionRegistrar
{
    public static async Task RegisterLabIfNeededAsync(
        AppSettings settings,
        string sessionId,
        string domain,
        int tier,
        ExchangeInput input,
        string? initialOutput,
        object chart)
    {
        var token = string.IsNullOrWhiteSpace(settings.AuthToken) ? null : settings.AuthToken;
        var labId = await ReadingSessionBridge.RegisterWithLabAsync(
            settings.LabApiUrl,
            settings.UseLabApi,
            token,
            domain,
            tier,
            input,
            initialOutput,
            chart);
        if (labId is not null)
        {
            App.Sessions.SetLabSessionId(sessionId, labId);
        }
    }
}
