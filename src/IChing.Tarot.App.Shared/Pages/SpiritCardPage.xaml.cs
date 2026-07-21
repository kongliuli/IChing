using IChing.Lab.Core.Tarot;
using IChing.Tarot.App.Services;
using IChing.Tarot.App.Views;

namespace IChing.Tarot.App.Pages;

public partial class SpiritCardPage : ContentPage
{
    private TarotCard? _card;
    private ImageSource? _cardImage;

    public SpiritCardPage()
    {
        InitializeComponent();
        SubPageSetup.Configure(this, "返回探索");
        BirthdayPicker.Date = DateTime.Today.AddYears(-25);
    }

    protected override bool OnBackButtonPressed()
    {
        _ = SubPageSetup.GoBackAsync(this);
        return true;
    }

    private async void OnDiscoverClicked(object? sender, EventArgs e)
    {
        _card = FunToolsService.PickSpiritCard(BirthdayPicker.Date ?? DateTime.Today);
        _cardImage = await App.CardImages.GetImageAsync(_card.Name);

        CardTitleLabel.Text = $"{_card.NameZh} · {_card.Name}";
        CardImage.Source = _cardImage;
        CardImage.IsVisible = _cardImage is not null;
        CardMeaningLabel.Text = $"正位关键词：{_card.UprightMeaning}\n\n逆位提示：{_card.ReversedMeaning}";
        ResultPanel.IsVisible = true;
        await MainScroll.ScrollToAsync(ResultPanel, ScrollToPosition.Start, true);
    }

    private async void OnSaveImageClicked(object? sender, EventArgs e)
    {
        if (_card is null)
        {
            return;
        }

        var root = new VerticalStackLayout { Spacing = 0, BackgroundColor = Color.FromArgb("#0B0812") };
        root.Add(ExportService.BuildHeader("牌灵对应", (BirthdayPicker.Date ?? DateTime.Today).ToString("yyyy-MM-dd")));
        if (_cardImage is not null)
        {
            root.Add(new Image
            {
                Source = _cardImage,
                Aspect = Aspect.AspectFit,
                HeightRequest = 360,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(24, 8)
            });
        }

        root.Add(ExportService.BuildTextBlock(
            $"{_card.NameZh} · {_card.Name}",
            $"正位：{_card.UprightMeaning}\n\n逆位：{_card.ReversedMeaning}"));
        root.Add(ExportService.BuildFooter());

        await SaveExportAsync(root, "牌灵");
    }

    private static async Task SaveExportAsync(View content, string name)
    {
        var path = await ExportService.CaptureAndSaveAsync(content, name);
        if (path is null)
        {
            await Shell.Current.DisplayAlertAsync("保存失败", "无法生成长图，请稍后重试。", "好的");
            return;
        }

        var msg = path.StartsWith("content://", StringComparison.OrdinalIgnoreCase) ||
                  path.Contains("Pictures", StringComparison.OrdinalIgnoreCase)
            ? "长图已保存到相册。"
            : $"长图已保存：{path}";
        await Shell.Current.DisplayAlertAsync("已保存", msg, "好的");
    }
}
