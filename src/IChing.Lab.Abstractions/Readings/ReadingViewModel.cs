namespace IChing.Lab.Abstractions.Readings;

public sealed record ReadingSectionVm(string Key, string Title, string Body);

public sealed record ReadingWidgetVm(string Kind, string Title, string PayloadJson);

public sealed record ReadingViewModel(
    string ProducerId,
    string Domain,
    string Title,
    string Subject,
    string Summary,
    IReadOnlyList<ReadingSectionVm> Sections,
    IReadOnlyList<ReadingWidgetVm> Widgets,
    string Theme = "tarot");
