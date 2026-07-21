namespace IChing.Client.Shared.Monetization;

/// <summary>
/// 商业版广告/支付占位；不接具体 SDK。
/// </summary>
public interface IMonetizationSlot
{
    string SlotId { get; }
    bool IsEnabled { get; }
    Task ShowAsync(CancellationToken cancellationToken = default);
}

public sealed class NoOpMonetizationSlot : IMonetizationSlot
{
    public NoOpMonetizationSlot(string slotId) => SlotId = slotId;

    public string SlotId { get; }
    public bool IsEnabled => false;

    public Task ShowAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
