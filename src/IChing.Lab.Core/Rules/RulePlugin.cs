namespace IChing.Lab.Core.Rules;

public sealed record RulePlugin(
    string Id,
    string Domain,
    string Title,
    string Description,
    int Weight,
    bool EnabledByDefault,
    Func<RuleContext, IEnumerable<RuleDigestItem>> Apply);

public sealed record RuleContext(object Chart, string? Question, string? Focus);

public sealed record RuleDigestItem(string PluginId, string Title, string Text, int Weight);

public sealed record RulePluginStatus(
    string Id,
    string Domain,
    string Title,
    string Description,
    bool Enabled,
    int Weight,
    bool EnabledByDefault,
    int DefaultWeight);
