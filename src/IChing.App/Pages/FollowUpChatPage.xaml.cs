using IChing.App.Services;
using Microsoft.Maui.Controls.Shapes;

namespace IChing.App.Pages;

public partial class FollowUpChatPage : ContentPage
{
    private readonly List<ChatTurn> _messages;
    private int _rounds;

    public FollowUpChatPage(string title, string systemPrompt, string context)
    {
        InitializeComponent();
        Title = title;
        _messages =
        [
            new("system", systemPrompt),
            new("assistant", context)
        ];
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
        _messages.Add(new("user", text));

        SendButton.IsEnabled = false;
        var answer = AddBubble("继续解答", string.Empty, incoming: true);
        await foreach (var chunk in App.Remote.StreamAsync(App.Settings, _messages))
        {
            answer.Text += chunk;
            await ChatScroll.ScrollToAsync(ChatHost, ScrollToPosition.End, false);
        }

        _messages.Add(new("assistant", answer.Text));
        SendButton.IsEnabled = _rounds < 3;
        QuestionEntry.IsEnabled = _rounds < 3;
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
                    new Label { Text = title, FontAttributes = FontAttributes.Bold, TextColor = (Color)Application.Current.Resources[incoming ? "Jade" : "Cinnabar"], FontSize = 12 },
                    label
                }
            }
        });
        return label;
    }
}
