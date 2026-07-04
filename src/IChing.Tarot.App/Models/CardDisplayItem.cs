namespace IChing.Tarot.App.Models;

public sealed class CardDisplayItem
{
    public required string PositionTitle { get; init; }
    public required string CardLine { get; init; }
    public required string Meaning { get; init; }
    public required Color SuitAccent { get; init; }
    public required string Abbrev { get; init; }
    public required bool IsReversed { get; init; }
    public double CardRotation => IsReversed ? 180d : 0d;
    public ImageSource? CardImage { get; init; }
    public bool HasImage => CardImage is not null;
    public bool HasPlaceholder => CardImage is null;
}
