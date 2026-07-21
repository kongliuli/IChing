using IChing.Tarot.App.Services;
using IChing.Tarot.App.Views;

namespace IChing.Tarot.App.Pages;

public partial class DailyColorPage : ContentPage
{
    private (string Name, string Hex, string Hint) _today;

    public DailyColorPage()
    {
        InitializeComponent();
        SubPageSetup.Configure(this, "返回探索");
        _today = FunToolsService.DailyColor(DateTime.Today);
        ColorSwatch.Color = Color.FromArgb(_today.Hex);
        ColorNameLabel.Text = $"{_today.Name}  {_today.Hex}";
        ColorHintLabel.Text = _today.Hint;
    }

    protected override bool OnBackButtonPressed()
    {
        _ = SubPageSetup.GoBackAsync(this);
        return true;
    }

    private async void OnSaveImageClicked(object? sender, EventArgs e)
    {
        var root = new VerticalStackLayout { Spacing = 0, BackgroundColor = Color.FromArgb("#0B0812") };
        root.Add(ExportService.BuildHeader("今日能量色", DateTime.Today.ToString("yyyy-MM-dd")));
        root.Add(new BoxView
        {
            Color = Color.FromArgb(_today.Hex),
            HeightRequest = 160,
            Margin = new Thickness(24, 8),
            CornerRadius = 8
        });
        root.Add(ExportService.BuildTextBlock(_today.Name, _today.Hint, highlight: true));
        root.Add(ExportService.BuildFooter());

        var path = await ExportService.CaptureAndSaveAsync(root, "今日能量色");
        if (path is null)
        {
            await DisplayAlertAsync("保存失败", "无法生成长图。", "好的");
            return;
        }

        await DisplayAlertAsync("已保存", "长图已保存到相册或缓存目录。", "好的");
    }
}
