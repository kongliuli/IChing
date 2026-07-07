using IChing.Lab.Core.Rules.Plugins;

namespace IChing.Lab.Core.Rules;

public sealed class RuleEngine
{
    public static readonly RuleEngine Default = new();

    private readonly IReadOnlyList<RulePlugin> _plugins;
    private readonly RuleEngineOptions _options;

    public RuleEngine(RuleEngineOptions? options = null)
    {
        _options = options ?? new RuleEngineOptions();
        _plugins =
        [
            .. LiuyaoRulePlugins.All,
            .. BaziRulePlugins.All,
            .. TarotRulePlugins.All
        ];
    }

    public IReadOnlyList<RulePluginStatus> ListPlugins() =>
        _plugins
            .Select(p =>
            {
                var configured = _options.Plugins.GetValueOrDefault(p.Id);
                return new RulePluginStatus(
                    p.Id,
                    p.Domain,
                    p.Title,
                    p.Description,
                    configured?.Enabled ?? p.EnabledByDefault,
                    configured?.Weight ?? p.Weight,
                    p.EnabledByDefault,
                    p.Weight);
            })
            .OrderBy(p => p.Domain)
            .ThenByDescending(p => p.Weight)
            .ThenBy(p => p.Id)
            .ToList();

    public bool ConfigurePlugin(string id, bool? enabled, int? weight)
    {
        if (_plugins.All(p => p.Id != id))
        {
            return false;
        }

        var current = _options.Plugins.GetValueOrDefault(id) ?? new RulePluginOptions();
        current.Enabled = enabled ?? current.Enabled;
        current.Weight = weight ?? current.Weight;
        _options.Plugins[id] = current;
        return true;
    }

    public RuleEngineOptions SnapshotOptions() => new()
    {
        MinWeight = _options.MinWeight,
        Plugins = _options.Plugins.ToDictionary(
            p => p.Key,
            p => new RulePluginOptions { Enabled = p.Value.Enabled, Weight = p.Value.Weight },
            StringComparer.Ordinal)
    };

    public RuleEngineResult Run(string domain, object chart, string? question = null, string? focus = null)
    {
        var context = new RuleContext(chart, question, focus);
        var items = new List<RuleDigestItem>();
        var active = new List<string>();

        foreach (var plugin in _plugins.Where(p => p.Domain == domain))
        {
            var configured = _options.Plugins.GetValueOrDefault(plugin.Id);
            var enabled = configured?.Enabled ?? plugin.EnabledByDefault;
            var weight = configured?.Weight ?? plugin.Weight;
            if (!enabled || weight < _options.MinWeight)
            {
                continue;
            }

            var produced = plugin.Apply(context)
                .Select(i => i with { Weight = weight })
                .ToList();
            if (produced.Count == 0)
            {
                continue;
            }

            active.Add(plugin.Id);
            items.AddRange(produced);
        }

        return new RuleEngineResult(active, items);
    }
}

public sealed record RuleEngineResult(
    IReadOnlyList<string> ActivePlugins,
    IReadOnlyList<RuleDigestItem> Items);
