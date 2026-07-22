using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Readings;

namespace IChing.App.Services;

public static class FollowUpPromptTemplates
{
    public static ExchangeInput BaziExchangeInput(BaziChart chart, BaziRuleDigest digest, string? focus) =>
        ExchangeInputBuilder.ForBazi(chart, digest, focus);

    public static ExchangeInput LiuyaoExchangeInput(
        LiuyaoNajiaResult chart,
        LiuyaoRuleDigest digest,
        string? question,
        string? focus) =>
        ExchangeInputBuilder.ForLiuyao(chart, digest, question, focus);
}
