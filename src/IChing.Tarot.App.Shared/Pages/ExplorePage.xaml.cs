using IChing.Client.Shared.Editions;
using IChing.Tarot.App.Models;
using IChing.Tarot.App.Services;

namespace IChing.Tarot.App.Pages;

public partial class ExplorePage : ContentPage
{
    private static readonly string[] DailyTips =
    [
        "今日宜静观其变，少做重大决定。",
        "今日宜沟通：把一件小事说清楚，胜过空想全局。",
        "今日宜收束：整理桌面与待办，给自己留白。",
        "今日宜行动：完成一个最小可交付的下一步。",
        "今日宜感恩：写下三件已经足够好的事。"
    ];

    public ExplorePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Shell.SetTabBarIsVisible(this, true);
        RenderDailyTip();
        _ = RenderModulesAsync();
    }

    private void RenderDailyTip()
    {
        DailyTipCard.IsVisible = EditionHost.Capabilities.ShowMonetizationSlots;
        if (!DailyTipCard.IsVisible)
        {
            return;
        }

        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var key = "daily_tip_" + today;
        var tip = Preferences.Default.Get(key, string.Empty);
        if (string.IsNullOrWhiteSpace(tip))
        {
            tip = DailyTips[DateTime.Today.DayOfYear % DailyTips.Length];
            Preferences.Default.Set(key, tip);
        }

        DailyTipLabel.Text = tip;
        DailyTipDateLabel.Text = $"今日提示 · {today}";
    }

    private async Task RenderModulesAsync()
    {
        var config = await ExploreModuleCatalog.LoadAsync();
        HeaderTitleLabel.Text = config.HeaderTitle;
        HeaderSubtitleLabel.Text = config.HeaderSubtitle;
        ModulesHost.Children.Clear();

        var editionKey = EditionKey(EditionHost.Capabilities);

        foreach (var section in config.Sections.Where(s => s.Enabled))
        {
            var items = section.Items
                .Where(i => i.Enabled && VisibleForEdition(i, editionKey))
                .ToList();
            if (items.Count == 0)
            {
                continue;
            }

            ModulesHost.Add(new Label
            {
                Text = section.Title,
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#D4AF37"),
                Margin = new Thickness(0, 8, 0, 0)
            });

            foreach (var item in items)
            {
                ModulesHost.Add(BuildModuleCard(item));
            }
        }
    }

    private static string EditionKey(EditionCapabilities caps) =>
        caps.ShowLabUrlSettings && caps.AllowRemoteByok ? "dev" :
        caps.Kind switch
        {
            EditionKind.Free => "free",
            EditionKind.Byok => "byok",
            EditionKind.Commercial => "commercial",
            _ => "dev"
        };

    private static bool VisibleForEdition(ExploreModuleItemConfig item, string editionKey) =>
        item.Editions is null
        || item.Editions.Count == 0
        || item.Editions.Any(e => e.Equals(editionKey, StringComparison.OrdinalIgnoreCase));

    private View BuildModuleCard(ExploreModuleItemConfig item)
    {
        var card = new Border
        {
            Stroke = Color.FromArgb("#4B3B61"),
            StrokeThickness = 1,
            BackgroundColor = Color.FromArgb("#21182D"),
            Padding = 16,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 }
        };

        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Add(new Label
        {
            Text = item.Title,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#B794F6"),
            FontSize = 18
        });

        if (!string.IsNullOrWhiteSpace(item.Description))
        {
            stack.Add(new Label
            {
                Text = item.Description,
                TextColor = Color.FromArgb("#B8AE9E"),
                FontSize = 13,
                LineBreakMode = LineBreakMode.WordWrap
            });
        }

        var button = new Button
        {
            Text = item.ButtonText,
            TextColor = Color.FromArgb("#1A1208"),
            BackgroundColor = Color.FromArgb("#D4AF37"),
            CornerRadius = 8,
            Padding = new Thickness(14, 10)
        };
        button.Clicked += async (_, _) =>
        {
            try
            {
                await ExploreModuleRouter.NavigateAsync(this, item);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("无法打开", ex.Message, "好的");
            }
        };
        stack.Add(button);
        card.Content = stack;
        return card;
    }
}
