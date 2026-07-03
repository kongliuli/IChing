using IChing.Lab.Core.Liuyao;

namespace IChing.Lab.Tests;

public class LiuyaoCoinDistributionTests
{
    [Fact]
    public void CoinValues_AreOnlyValidYaoSums()
    {
        var counts = new Dictionary<int, int> { [6] = 0, [7] = 0, [8] = 0, [9] = 0 };
        for (var i = 0; i < 6000; i++)
        {
            var line = LiuyaoEngine.CoinToss(i).Lines[0];
            var sum = line switch
            {
                { Yang: false, Moving: true } => 6,
                { Yang: true, Moving: false } => 7,
                { Yang: false, Moving: false } => 8,
                { Yang: true, Moving: true } => 9,
                _ => -1
            };
            counts[sum]++;
        }

        Assert.True(counts[6] > 0 && counts[7] > 0 && counts[8] > 0 && counts[9] > 0);
    }
}
