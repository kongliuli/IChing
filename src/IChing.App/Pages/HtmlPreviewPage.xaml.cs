namespace IChing.App.Pages;

public partial class HtmlPreviewPage : ContentPage
{
    private readonly string _title;

    public HtmlPreviewPage(string title, string html)
    {
        InitializeComponent();
        _title = title;
        TitleLabel.Text = title;
        PreviewWebView.Source = new HtmlWebViewSource { Html = html };
    }

    private async void OnExportClicked(object? sender, EventArgs e)
    {
        if (!Screenshot.Default.IsCaptureSupported)
        {
            await DisplayAlertAsync("无法导出", "当前平台不支持截图导出。", "好的");
            return;
        }

        var screenshot = await Screenshot.Default.CaptureAsync();
        await using var stream = await screenshot.OpenReadAsync();
        var path = Path.Combine(FileSystem.AppDataDirectory, $"{_title}-{DateTime.Now:yyyyMMdd-HHmmss}.png");
        await using var file = File.Create(path);
        await stream.CopyToAsync(file);
        await DisplayAlertAsync("已导出", path, "好的");
    }

    private async void OnCloseClicked(object? sender, EventArgs e) =>
        await Navigation.PopModalAsync();
}
