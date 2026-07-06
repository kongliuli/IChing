namespace IChing.Lab.Core.Rules;

public sealed class RuleEngineOptions
{
    public int MinWeight { get; set; }

    public Dictionary<string, RulePluginOptions> Plugins { get; set; } = new(StringComparer.Ordinal);
}

public sealed class RulePluginOptions
{
    public bool? Enabled { get; set; }

    public int? Weight { get; set; }
}
