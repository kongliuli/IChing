using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Readings;
using IChing.Lab.Presentation;

namespace IChing.Lab.Tests;

public class FollowUpReadingPresenterTests
{
    [Fact]
    public void ToDocument_ParsesJsonSummaryIntoHtml()
    {
        var input = new ExchangeInput(null, "综合", ["fact"], ["rule"], []);
        var html = FollowUpReadingPresenter.ToDocument(
            "bazi",
            1,
            input,
            null,
            """{"schema":"reading-output.v2","summary":"今年宜稳","sections":[],"warnings":[]}""");

        Assert.Contains("今年宜稳", html);
        Assert.Contains("<html", html, StringComparison.OrdinalIgnoreCase);
    }
}
