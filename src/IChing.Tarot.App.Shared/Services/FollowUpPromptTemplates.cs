using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Tarot;

namespace IChing.Tarot.App.Services;

public static class FollowUpPromptTemplates
{
    public static ExchangeInput TarotExchangeInput(TarotReading reading, string? question) =>
        ExchangeInputBuilder.ForTarot(reading, question);
}
