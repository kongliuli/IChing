namespace IChing.Lab.Core.Bazi;

/// <summary>
/// ponytail: Spencer (1971) equation-of-time approximation, ±1 min — enough for 时辰边界.
/// </summary>
public static class TrueSolarTime
{
    private const double DefaultStandardLongitude = 120.0; // China standard meridian

    public static DateTime Apply(
        DateTime localWallClock,
        double longitudeEast,
        double standardLongitude = DefaultStandardLongitude)
    {
        var dayOfYear = localWallClock.DayOfYear;
        var b = 2 * Math.PI * (dayOfYear - 81) / 364.0;
        var eotMinutes = 9.87 * Math.Sin(2 * b) - 7.53 * Math.Cos(b) - 1.5 * Math.Sin(b);
        var longitudeCorrection = (longitudeEast - standardLongitude) * 4.0;
        return localWallClock.AddMinutes(longitudeCorrection + eotMinutes);
    }
}
