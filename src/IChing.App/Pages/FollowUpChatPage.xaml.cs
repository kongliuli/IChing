using IChing.App.Services;
using IChing.Lab.Client;
using IChing.Lab.Core.Integrations;
using Microsoft.Maui.Controls.Shapes;

namespace IChing.App.Pages;

public partial class FollowUpChatPage : ContentPage
{
    private readonly FollowUpChatArgs _args;
    private readonly List<DialogueTurnState> _history = [];
    private int _rounds;

    public FollowUpChatPage(FollowUpChatArgs args)
    {
        InitializeComponent();
        _args = args;
        Title = args.Title;
        AddBubble("当前上下文", args.Context, incoming: true);
    }

    public FollowUpChatPage(string title, string systemPrompt, string context)
        : this(new FollowUpChatArgs(title, "unknown", Guid.NewGuid().ToString("N"), systemPrompt, context))
    {
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

        SendButton.IsEnabled = false;
        var answer = AddBubble("继续解答", string.Empty, incoming: true);
        await foreach (var chunk in App.Remote.StreamAsync(App.Settings, BuildRequestMessages(text)))
        {
            answer.Text += chunk;
            await ChatScroll.ScrollToAsync(ChatHost, ScrollToPosition.End, false);
        }

        _history.Add(new DialogueTurnState("user", text));
        _history.Add(new DialogueTurnState("assistant", answer.Text));
        App.Sessions.AppendExchange(_args.SessionId, new StoredExchange(
            exchangeId,
            null,
            "followup",
            1,
            "{}",
            answer.Text,
            DateTimeOffset.UtcNow));

        SendButton.IsEnabled = _rounds < 3;
        QuestionEntry.IsEnabled = _rounds < 3;
    }

    private IReadOnlyList<ChatTurn> BuildRequestMessages(string currentQuestion)
    {
        var messages = new List<ChatTurn>
        {
            new("system", _args.SystemPrompt),
            new("assistant", _args.Context)
        };

        foreach (var turn in _history.TakeLast(4))
        {
            messages.Add(new(turn.Role, turn.Content));
        }

        messages.Add(new("user", currentQuestion));
        return messages;
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

    private sealed record DialogueTurnState(string Role, string Content);
}
