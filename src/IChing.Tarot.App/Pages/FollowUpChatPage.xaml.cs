using IChing.Lab.Core.Integrations;
using IChing.Tarot.App.Services;
using Microsoft.Maui.Controls.Shapes;

namespace IChing.Tarot.App.Pages;

public partial class FollowUpChatPage : ContentPage
{
    private readonly RemoteInterpretationService _remote = new();
    private readonly string _systemPrompt;
    private readonly string _context;
    private string? _lastUser;
    private string? _lastAssistant;
    private int _rounds;

    public FollowUpChatPage(string systemPrompt, string context)
    {
        InitializeComponent();
        _systemPrompt = systemPrompt;
        _context = context;
        AddBubble("当前上下文", context, incoming: true);
    }

    private async void OnSendClicked(object? sender, EventArgs e)
    {
        var text = QuestionEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text) || _rounds >= 3)
        {
            return;
        }

        _rounds++;
        QuestionEntry.Text = string.Empty;
        AddBubble("你", text, incoming: false);

        SendButton.IsEnabled = false;
        var answer = AddBubble("继续解答", string.Empty, incoming: true);
        await foreach (var chunk in _remote.StreamAsync(App.Settings, BuildRequestMessages(text)))
        {
            answer.Text += chunk;
            await ChatScroll.ScrollToAsync(ChatHost, ScrollToPosition.End, false);
        }

        _lastUser = text;
        _lastAssistant = answer.Text;
        SendButton.IsEnabled = _rounds < 3;
        QuestionEntry.IsEnabled = _rounds < 3;
    }

    private IReadOnlyList<ChatTurn> BuildRequestMessages(string currentQuestion)
    {
        var messages = new List<ChatTurn>
        {
            new("system", _systemPrompt),
            new("assistant", _context)
        };

        if (!string.IsNullOrWhiteSpace(_lastUser) && !string.IsNullOrWhiteSpace(_lastAssistant))
        {
            messages.Add(new("user", _lastUser));
            messages.Add(new("assistant", _lastAssistant));
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
            Padding = 12,
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
                        TextColor = (Color)Application.Current.Resources["Gold"],
                        FontSize = 12
                    },
                    label
                }
            }
        });
        return label;
    }
}
