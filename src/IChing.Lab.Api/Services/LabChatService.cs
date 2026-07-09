using System.Collections.Concurrent;
using IChing.Lab.Abstractions.Models;
using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Api.Contracts;
using IChing.Lab.Core.Readings;
using IChing.Lab.Inference;
using Microsoft.AspNetCore.Mvc;

namespace IChing.Lab.Api.Services;

public sealed class LabChatService
{
    private const int MaxSessions = 100;
    private readonly ConcurrentDictionary<string, ServerChatSession> _sessions = new();
    private readonly LabReadService _reads;
    private readonly ChartInterpretationOrchestrator _orchestration;
    private readonly AccountsCreditsGateway _accounts;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LabChatService(
        LabReadService reads,
        ChartInterpretationOrchestrator orchestration,
        AccountsCreditsGateway accounts,
        IHttpContextAccessor httpContextAccessor)
    {
        _reads = reads;
        _orchestration = orchestration;
        _accounts = accounts;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> ExecuteAsync(LabChatRequest req, CancellationToken cancellationToken)
    {
        if (string.Equals(req.Mode, "register", StringComparison.OrdinalIgnoreCase))
        {
            return RegisterSession(req);
        }

        if (string.Equals(req.Mode, "initial", StringComparison.OrdinalIgnoreCase))
        {
            return req.Domain.ToLowerInvariant() switch
            {
                "bazi" when req.Bazi is not null => await _reads.ExecuteBaziRead(req.Tier, req.Bazi, cancellationToken),
                "liuyao" when req.Liuyao is not null => await _reads.ExecuteLiuyaoRead(req.Tier, req.Liuyao, cancellationToken),
                "tarot" when req.Tarot is not null => await _reads.ExecuteTarotRead(req.Tier, req.Tarot, cancellationToken),
                _ => new BadRequestObjectResult(new { error = "initial chat requires domain read body" })
            };
        }

        return await FollowUpAsync(req, cancellationToken);
    }

    private IActionResult RegisterSession(LabChatRequest req)
    {
        if (req.Input is null || string.IsNullOrWhiteSpace(req.Domain))
        {
            return new BadRequestObjectResult(new { error = "register requires domain and input" });
        }

        TrimSessions();
        var sessionId = Guid.NewGuid().ToString("N");
        var structured = ReadingOutputParser.TryParseStructured(req.InitialOutput, req.Domain);
        _sessions[sessionId] = new ServerChatSession(
            sessionId,
            req.Domain,
            req.Tier <= 0 ? 1 : req.Tier,
            req.Input,
            req.InitialOutput,
            structured,
            Guid.NewGuid().ToString("N"),
            [],
            DateTimeOffset.UtcNow);
        return new OkObjectResult(new { sessionId });
    }

    private async Task<IActionResult> FollowUpAsync(LabChatRequest req, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(req.SessionId) || string.IsNullOrWhiteSpace(req.UserQuestion))
        {
            return new BadRequestObjectResult(new { error = "followup requires sessionId and userQuestion" });
        }

        if (!_sessions.TryGetValue(req.SessionId!, out var session))
        {
            return new NotFoundObjectResult(new { error = "session not found" });
        }

        var auth = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        var exchangeId = Guid.NewGuid().ToString("N");
        var credits = await _accounts.TryConsumeAsync(auth, req.Tier <= 0 ? 1 : req.Tier, exchangeId, cancellationToken);
        if (!credits.Allowed)
        {
            var status = credits.Error?.Contains("Token", StringComparison.OrdinalIgnoreCase) == true ? 401 : 402;
            return new ObjectResult(new { error = credits.Error }) { StatusCode = status };
        }

        var history = session.History.ToList();
        var exchange = ReadingExchangeFactory.CreateFollowUp(
            session.Input,
            session.Domain,
            session.Tier,
            session.SessionId,
            session.LastExchangeId,
            req.UserQuestion!,
            history,
            session.InitialStructured);

        var (system, user) = FollowUpExchangeBuilder.BuildRemotePrompt(exchange, session.InitialStructured, session.InitialRaw);
        var prompt = ReadingJsonOutputContract.Append(session.Domain, $"{system}\n\n{user}", "core-followup-json");
        var gen = await _orchestration.GenerateWithFallbackAsync(
            prompt,
            new GenerateOptions(MaxTokens: req.MaxTokens ?? 512),
            cancellationToken);

        history.Add(new DialogueTurn("user", req.UserQuestion!));
        history.Add(new DialogueTurn("assistant", gen.Text));
        _sessions[session.SessionId] = session with
        {
            LastExchangeId = exchange.Meta.ExchangeId,
            History = history
        };

        var output = ReadingOutputParser.BuildExchangeOutput(
            session.Domain, gen.Text, null, gen.EngineId, gen.IsFallback, gen.FallbackReason);
        return new OkObjectResult(new
        {
            schema = ReadingSchemas.EnvelopeV2,
            sessionId = session.SessionId,
            exchange = exchange with { Output = output },
            isFallback = gen.IsFallback
        });
    }

    private void TrimSessions()
    {
        while (_sessions.Count > MaxSessions)
        {
            var oldest = _sessions.MinBy(kv => kv.Value.CreatedAt).Key;
            _sessions.TryRemove(oldest, out _);
        }
    }

    private sealed record ServerChatSession(
        string SessionId,
        string Domain,
        int Tier,
        ExchangeInput Input,
        string? InitialRaw,
        ReadingStructuredOutput? InitialStructured,
        string LastExchangeId,
        IReadOnlyList<DialogueTurn> History,
        DateTimeOffset CreatedAt);
}
