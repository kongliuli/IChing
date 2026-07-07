namespace IChing.Tarot.App.Models;

public sealed class ExploreModulesConfig
{
    public int Version { get; init; } = 1;
    public string HeaderTitle { get; init; } = "趣味探索";
    public string HeaderSubtitle { get; init; } = string.Empty;
    public List<ExploreSectionConfig> Sections { get; init; } = [];
}

public sealed class ExploreSectionConfig
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public bool Enabled { get; init; } = true;
    public List<ExploreModuleItemConfig> Items { get; init; } = [];
}

public sealed class ExploreModuleItemConfig
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string Description { get; init; } = string.Empty;
    public bool Enabled { get; init; } = true;
    public required string Action { get; init; }
    public string? ActionParam { get; init; }
    public string ButtonText { get; init; } = "开始 →";
}
