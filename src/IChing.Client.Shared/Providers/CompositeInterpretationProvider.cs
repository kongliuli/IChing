using IChing.Client.Shared.Editions;
using IChing.Client.Shared.Settings;
using IChing.Lab.Core.Integrations;

namespace IChing.Client.Shared.Providers;

/// <summary>
/// 按 EditionCapabilities 编排：Tier0→规则；否则按版本优先链尝试。
/// </summary>
public sealed class CompositeInterpretationProvider : IInterpretationProvider
{
    private readonly EditionCapabilities _edition;
    private readonly IClientRuntimeSettings _settings;
    private readonly RuleOnlyProvider _rules = new();
    private readonly ByokRemoteProvider _byok;
    private readonly CommercialLabProvider _lab;
    private readonly LocalOnnxProvider _onnx;

    public CompositeInterpretationProvider(
        EditionCapabilities edition,
        IClientRuntimeSettings settings,
        LocalOnnxProvider? onnx = null)
    {
        _edition = edition;
        _settings = settings;
        _byok = new ByokRemoteProvider(settings);
        _lab = new CommercialLabProvider(settings);
        _onnx = onnx ?? new LocalOnnxProvider(settings);
    }

    public string ProviderId => $"composite:{_edition.Kind}";
    public bool SupportsFollowUp => _edition.AllowFollowUp;

    public async Task<InterpretationResult> InterpretAsync(
        InterpretationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Tier <= 0 || (!_edition.AllowRemoteByok && !_edition.AllowLabCommercial && !_edition.AllowLocalOnnx))
        {
            return await _rules.InterpretAsync(request, cancellationToken);
        }

        // 商业版：仅 Lab（不走 BYOK）
        if (_edition.Kind == EditionKind.Commercial)
        {
            var lab = await _lab.InterpretAsync(request, cancellationToken);
            if (!lab.IsFallback || !string.IsNullOrWhiteSpace(lab.Text))
            {
                return lab;
            }

            return await _rules.InterpretAsync(request, cancellationToken);
        }

        // 开发壳 / 带 Lab 开关：优先 Lab
        if (_edition.AllowLabCommercial && _settings.UseLabApi && _settings.IsLabConfigured)
        {
            var lab = await _lab.InterpretAsync(request, cancellationToken);
            if (!string.IsNullOrWhiteSpace(lab.Text) && (lab.Error is null || !lab.IsFallback || lab.Text.Length > 20))
            {
                return lab;
            }
        }

        // 端侧 ONNX：已配置模型且无远程 Key 时优先（避免仅因 localhost 被判 IsConfigured 抢走）
        var preferOnnx = _edition.AllowLocalOnnx
                         && string.IsNullOrWhiteSpace(_settings.ApiKey)
                         && !string.IsNullOrWhiteSpace(_settings.LocalOnnxModelPath)
                         && Directory.Exists(_settings.LocalOnnxModelPath);
        if (preferOnnx)
        {
            return await _onnx.InterpretAsync(request, cancellationToken);
        }

        // 自助版 / 开发壳 BYOK
        if (_edition.AllowRemoteByok && _settings.IsConfigured)
        {
            return await _byok.InterpretAsync(request, cancellationToken);
        }

        if (_edition.AllowLocalOnnx)
        {
            return await _onnx.InterpretAsync(request, cancellationToken);
        }

        return await _rules.InterpretAsync(request, cancellationToken);
    }

    public Task<ConnectionTestResult> TestAsync(CancellationToken cancellationToken = default)
    {
        if (_edition.Kind == EditionKind.Free || _edition.AllowLocalOnnx && !_edition.AllowRemoteByok && !_settings.UseLabApi)
        {
            return _onnx.TestAsync(cancellationToken);
        }

        if (_edition.AllowLabCommercial && (_edition.Kind == EditionKind.Commercial || _settings.UseLabApi))
        {
            return _lab.TestAsync(cancellationToken);
        }

        if (_edition.AllowRemoteByok)
        {
            return _byok.TestAsync(cancellationToken);
        }

        return _rules.TestAsync(cancellationToken);
    }

    public IAsyncEnumerable<string> StreamFollowUpAsync(
        IReadOnlyList<ChatTurn> messages,
        CancellationToken cancellationToken = default)
    {
        if (!_edition.AllowFollowUp)
        {
            return EmptyAsync();
        }

        // 商业版追问走 StreamLabFollowUpAsync(sessionId)，不走裸 messages。
        if (_edition.Kind == EditionKind.Commercial)
        {
            return EmptyAsync();
        }

        if (_edition.AllowRemoteByok && _settings.IsConfigured)
        {
            return _byok.StreamFollowUpAsync(messages, cancellationToken);
        }

        return EmptyAsync();
    }

    /// <summary>
    /// 是否应走 Lab <c>/lab/chat</c> followup（需要已登记的 labSessionId）。
    /// </summary>
    public bool ShouldUseLabFollowUp(string? labSessionId) =>
        _edition.AllowFollowUp
        && _edition.AllowLabCommercial
        && !string.IsNullOrWhiteSpace(labSessionId)
        && (_edition.Kind == EditionKind.Commercial || _settings.UseLabApi)
        && _settings.IsLabConfigured;

    /// <summary>商业/DevShell Lab 追问：非流式一次返回，拆成单 chunk 供 UI 复用。</summary>
    public async IAsyncEnumerable<string> StreamLabFollowUpAsync(
        string labSessionId,
        string userQuestion,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!ShouldUseLabFollowUp(labSessionId))
        {
            yield break;
        }

        var result = await _lab.FollowUpAsync(labSessionId, userQuestion, cancellationToken: cancellationToken);
        if (!string.IsNullOrWhiteSpace(result.Text))
        {
            yield return result.Text;
        }
        else if (!string.IsNullOrWhiteSpace(result.Error))
        {
            yield return $"（{result.Error}）";
        }
    }

    private static async IAsyncEnumerable<string> EmptyAsync()
    {
        await Task.CompletedTask;
        yield break;
    }
}
