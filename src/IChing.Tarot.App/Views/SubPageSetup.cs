namespace IChing.Tarot.App.Views;

/// <summary>子页顶栏 + 返回（PushAsync 栈 + 固定按钮，兼容 Android Tab Shell）。</summary>
public static class SubPageSetup
{
    private const string ChromeFlag = "__subpage_chrome__";

    public static void Configure(ContentPage page, string backTitle = "返回探索")
    {
        if (page.GetValue(ChromeAttachedProperty) is true)
        {
            return;
        }

        page.SetValue(ChromeAttachedProperty, true);
        InjectBackBar(page, backTitle);

        page.Appearing += (_, _) =>
        {
            Shell.SetTabBarIsVisible(page, false);
            Shell.SetNavBarIsVisible(page, true);
        };

        Shell.SetBackButtonBehavior(page, new BackButtonBehavior
        {
            IsVisible = true,
            TextOverride = "返回",
            Command = new Command(async () => await GoBackAsync(page))
        });
    }

    public static async Task GoBackAsync(ContentPage page)
    {
        if (page.Navigation?.NavigationStack?.Count > 1)
        {
            await page.Navigation.PopAsync();
            return;
        }

        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    public static async void OnBackClicked(object? sender, EventArgs e)
    {
        if (sender is BindableObject bo && bo.BindingContext is ContentPage cp)
        {
            await GoBackAsync(cp);
            return;
        }

        if (Shell.Current?.CurrentPage is ContentPage current)
        {
            await GoBackAsync(current);
        }
    }

    private static void InjectBackBar(ContentPage page, string backTitle)
    {
        var body = page.Content;
        if (body is null)
        {
            return;
        }

        var backRow = new Grid
        {
            Padding = new Thickness(16, 10, 16, 6),
            BackgroundColor = Color.FromArgb("#161022"),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            Children =
            {
                new Label
                {
                    Text = page.Title ?? string.Empty,
                    FontSize = 17,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#D4AF37"),
                    VerticalOptions = LayoutOptions.Center
                },
                new Button
                {
                    Text = $"← {backTitle}",
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    HeightRequest = 40,
                    MinimumWidthRequest = 120,
                    Padding = new Thickness(14, 6),
                    CornerRadius = 10,
                    TextColor = Color.FromArgb("#1A1208"),
                    BackgroundColor = Color.FromArgb("#D4AF37"),
                    BorderWidth = 0
                }
            }
        };

        var backButton = (Button)backRow.Children[1];
        backButton.Clicked += async (_, _) => await GoBackAsync(page);

        page.Content = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            },
            RowSpacing = 0,
            Children = { backRow, body }
        };

        Grid.SetRow(body, 1);
    }

    private static readonly BindableProperty ChromeAttachedProperty =
        BindableProperty.Create(ChromeFlag, typeof(bool), typeof(SubPageSetup), false);
}
