using IChing.App.Services;
using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Client;
using IChing.Lab.Core.Integrations;
using IChing.Lab.Core.Readings;
using IChing.Lab.Presentation;
using Microsoft.Maui.Controls.Shapes;

namespace IChing.App.Pages;

public partial class FollowUpChatPage : ContentPage
{
    private readonly FollowUpChatArgs _args;
    private readonly FollowUpSessionSeed _seed;
    private readonly object? _chart;
    private readonly List<DialogueTurn> _history = [];
    private ReadingStructuredOutput? _initialStructured;
    private int _rounds;

    public FollowUpChatPage(FollowUpChatArgs args)
    {
        InitializeComponent();
        _args = args;
        Title = args.Title;
        var seed = App.Sessions.GetFollowUpSeed(args.SessionId)
                   ?? throw new InvalidOperationException($"session not found: {args.SessionId}");
        _seed = seed;
        _chart = App.Sessions.GetSessionChart(args.SessionId);
        _initialStructured = ReadingOutputParser.TryParseStructured(seed.InitialOutputJson, seed.Domain);
        var preview = ExchangeContextCompactor.BuildFollowUpContext(seed.Input, _initialStructured, [], null);
        AddBubble("当前上下文", preview, incoming: true);
    }

    private async void OnSendClicked(object? sender, EventArgs e)
    {
        var text = QuestionEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text) || _rounds >= 3)
        {
            return;
        }

        var exchangeId = Guid.NewGuid().ToString("N");
        if (App.Settings.UseLabApi)
        {
            var token = string.IsNullOrWhiteSpace(App.Settings.AuthToken) ? null : App.Settings.AuthToken;
            var (ok, status, error) = await LabApiClient.ConsumeCreditsAsync(
                App.Settings.LabApiUrl,
                exchangeId,
                _args.Domain,
                "followup",
                token,
                _args.SessionId);
            if (!ok)
            {
                await DisplayAlertAsync("额度不足", $"HTTP {status}: {error}", "好的");
                return;
            }
        }

        _rounds++;
        QuestionEntry.Text = string.Empty;
        AddBubble("你", text, incoming: false);

        var exchange = ReadingExchangeFactory.CreateFollowUp(
            _seed.Input,
            _seed.Domain,
            _seed.Tier,
            _args.SessionId,
            _seed.LastExchangeId,
            text,
            _history,
            _initialStructured);

        var packet = ExchangePromptAdapter.ToFollowUpPacket(exchange, _initialStructured, _seed.InitialOutputJson);
        var messages = new List<ChatTurn>
        {
            new("system", ReadingPromptProtocol.BuildSystemPrompt(packet)),
            new("user", ReadingPromptProtocol.BuildUserMessage(packet))
        };

        SendButton.IsEnabled = false;
        var answer = AddBubble("继续解答", string.Empty, incoming: true);
        var raw = new System.Text.StringBuilder();
        await foreach (var chunk in App.Remote.StreamAsync(App.Settings, messages))
        {
            raw.Append(chunk);
            answer.Text = ReadingPromptProtocol.NormalizeOutput(raw.ToString());
            await ChatScroll.ScrollToAsync(ChatHost, ScrollToPosition.End, false);
        }

        var body = raw.ToString();
        UpgradeBubbleToHtml(answer, FollowUpReadingPresenter.ToDocument(_args.Domain, _seed.Tier, _seed.Input, _chart, body));
        _history.Add(new DialogueTurn("user", text));
        _history.Add(new DialogueTurn("assistant", body));
        App.Sessions.AppendExchange(_args.SessionId, new StoredExchange(
            exchangeId,
            _args.SessionId,
            _seed.LastExchangeId,
            "followup",
            _seed.Tier,
            FollowUpExchangeBuilder.SerializeInput(_seed.Input),
            body,
            DateTimeOffset.UtcNow));

        SendButton.IsEnabled = _rounds < 3;
        QuestionEntry.IsEnabled = _rounds < 3;
    }

    private static void UpgradeBubbleToHtml(Label label, string html)
    {
        if (label.Parent is not VerticalStackLayout layout)
        {
            return;
        }

        label.IsVisible = false;
        layout.Children.Add(new WebView
        {
            HeightRequest = 420,
            Source = new HtmlWebViewSource { Html = html }
        });
    }

    private Label AddBubble(string title, string text, bool incoming)
    {
        var label = new Label
        {
            Text = text,
            TextColor = (Color)Application.Current!.Resources["TextPrimary"],
            FontSize = 13
        };
        ChatHost.Add(new Border
        {
            Padding = 14,
            BackgroundColor = (Color)Application.Current.Resources[incoming ? "Surface" : "SurfaceAlt"],
            Stroke = (Color)Application.Current.Resources["StrokeSoft"],
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label
                    {
                        Text = title,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = (Color)Application.Current.Resources[incoming ? "Jade" : "Cinnabar"],
                        FontSize = 12
                    },
                    label
                }
            }
        });
        return label;
    }
}
