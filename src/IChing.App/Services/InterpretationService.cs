using IChing.Client.Shared;
using IChing.Client.Shared.Editions;
using IChing.Client.Shared.Providers;
using IChing.Client.Shared.Settings;
using IChing.Lab.Core.Integrations;
using IChing.Lab.Core.Readings;

namespace IChing.App.Services;

/// <summary>
/// 八字/六爻解读：经 CompositeInterpretationProvider（开发壳能力 = Lab + BYOK + 本地 ONNX）。
/// </summary>
public sealed class InterpretationService
{
    private readonly InterpretationFacade _facade;

    public InterpretationService()
    {
        var settings = ClientRuntimeSettingsAdapter.FromAppSettingsLike(
            () => App.Settings.Provider,
            () => App.Settings.BaseUrl,
            () => App.Settings.Model,
            () => App.Settings.ApiKey,
            () => App.Settings.LabApiUrl,
            () => App.Settings.UseLabApi,
            () => App.Settings.InterpretTier,
            () => App.Settings.AuthToken,
            () => App.Settings.Temperature,
            () => App.Settings.MaxTokens);
        _facade = new InterpretationFacade(EditionCapabilities.DevShell, settings);
    }

    public async Task<RemoteInterpretationResult> InterpretBaziAsync(
        AppSettings settings,
        object labReadBody,
        ReadingPromptPacket fallbackPacket,
        CancellationToken cancellationToken = default)
    {
        _ = settings;
        var result = await _facade.InterpretBaziAsync(labReadBody, fallbackPacket, cancellationToken: cancellationToken);
        return ToRemote(result);
    }

    public async Task<RemoteInterpretationResult> InterpretLiuyaoAsync(
        AppSettings settings,
        object labReadBody,
        ReadingPromptPacket fallbackPacket,
        CancellationToken cancellationToken = default)
    {
        _ = settings;
        var result = await _facade.InterpretLiuyaoAsync(labReadBody, fallbackPacket, cancellationToken: cancellationToken);
        return ToRemote(result);
    }

    public async Task<ConnectionTestResult> TestAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        _ = settings;
        return await _facade.TestAsync(cancellationToken);
    }

    private static RemoteInterpretationResult ToRemote(InterpretationResult result) =>
        new(result.Text, result.IsFallback, result.Error);
}
