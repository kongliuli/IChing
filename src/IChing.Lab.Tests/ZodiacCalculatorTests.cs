using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Tests;

public class ZodiacCalculatorTests
{
    [Theory]
    [InlineData("2024-03-21", "白羊座")]
    [InlineData("2024-04-19", "白羊座")]
    [InlineData("2024-04-20", "金牛座")]
    [InlineData("2024-05-21", "双子座")]
    [InlineData("2024-06-22", "巨蟹座")]
    [InlineData("2024-07-23", "狮子座")]
    [InlineData("2024-08-23", "处女座")]
    [InlineData("2024-09-23", "天秤座")]
    [InlineData("2024-10-24", "天蝎座")]
    [InlineData("2024-11-23", "射手座")]
    [InlineData("2024-12-22", "摩羯座")]
    [InlineData("2024-12-31", "摩羯座")]
    [InlineData("2025-01-01", "摩羯座")]
    [InlineData("2025-01-19", "摩羯座")]
    [InlineData("2025-01-20", "水瓶座")]
    [InlineData("2025-02-19", "双鱼座")]
    [InlineData("2025-02-20", "双鱼座")]
    public void FromBirthday_ReturnsCorrectZodiac(string birthday, string expected)
    {
        var result = ZodiacCalculator.FromBirthday(birthday);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-date")]
    [InlineData("2024-02-30")]
    public void FromBirthday_InvalidInput_ReturnsNull(string? birthday)
    {
        var result = ZodiacCalculator.FromBirthday(birthday);
        Assert.Null(result);
    }

    [Fact]
    public void FromBirthday_LeapDay_DoesNotCrash()
    {
        var result = ZodiacCalculator.FromBirthday("2024-02-29");
        Assert.Equal("双鱼座", result);
    }
}
