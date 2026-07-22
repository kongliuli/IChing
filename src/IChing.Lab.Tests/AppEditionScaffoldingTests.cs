using IChing.Client.Shared.Editions;

namespace IChing.Lab.Tests;

public class AppEditionScaffoldingTests
{
    [Fact]
    public void AppEditionCapabilities_Matrix()
    {
        Assert.False(EditionCapabilities.Free.AllowAiInterpretation);
        Assert.True(EditionCapabilities.Byok.AllowRemoteByok);
        Assert.True(EditionCapabilities.Commercial.AllowLabCommercial);
        Assert.True(EditionCapabilities.DevShell.AllowFollowUp);
    }
}
