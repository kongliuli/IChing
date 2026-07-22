using IChing.Client.Shared.Editions;
using IChing.Client.Shared.Providers;
using IChing.Client.Shared.Settings;

namespace IChing.Lab.Tests;

public class CommercialLabFollowUpRoutingTests
{
    [Theory]
    [InlineData(true, "sess-1", true)]
    [InlineData(true, null, false)]
    [InlineData(true, "", false)]
    [InlineData(false, "sess-1", false)]
    public void Commercial_ShouldUseLabFollowUp_RequiresSession(bool labConfigured, string? sessionId, bool expected)
    {
        var settings = new MutableClientRuntimeSettings
        {
            LabApiUrl = labConfigured ? "http://localhost:5000" : " ",
            UseLabApi = true
        };
        var composite = new CompositeInterpretationProvider(EditionCapabilities.Commercial, settings);
        Assert.Equal(expected, composite.ShouldUseLabFollowUp(sessionId));
    }

    [Fact]
    public void Byok_NeverUsesLabFollowUp()
    {
        var settings = new MutableClientRuntimeSettings { LabApiUrl = "http://localhost:5000", UseLabApi = true };
        var composite = new CompositeInterpretationProvider(EditionCapabilities.Byok, settings);
        Assert.False(composite.ShouldUseLabFollowUp("sess"));
    }

    [Fact]
    public void DevShell_UsesLabFollowUp_WhenUseLabApiAndSession()
    {
        var on = new MutableClientRuntimeSettings { LabApiUrl = "http://localhost:5000", UseLabApi = true };
        var off = new MutableClientRuntimeSettings { LabApiUrl = "http://localhost:5000", UseLabApi = false };
        Assert.True(new CompositeInterpretationProvider(EditionCapabilities.DevShell, on).ShouldUseLabFollowUp("s"));
        Assert.False(new CompositeInterpretationProvider(EditionCapabilities.DevShell, off).ShouldUseLabFollowUp("s"));
    }

    [Fact]
    public async Task Commercial_StreamFollowUp_MessagesPath_IsEmpty()
    {
        var settings = new MutableClientRuntimeSettings { LabApiUrl = "http://localhost:5000", UseLabApi = true };
        var composite = new CompositeInterpretationProvider(EditionCapabilities.Commercial, settings);
        var chunks = new List<string>();
        await foreach (var c in composite.StreamFollowUpAsync([new("user", "hi")]))
        {
            chunks.Add(c);
        }

        Assert.Empty(chunks);
    }
}
