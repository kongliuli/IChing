namespace IChing.App.Pages;

public partial class HistoryPage : ContentPage
{
    public HistoryPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadHistory();
    }

    private async void OnClearClicked(object? sender, EventArgs e)
    {
        if (!App.History.GetRecent().Any())
        {
            return;
        }

        var ok = await DisplayAlertAsync("清空历史", "确定删除本地历史记录吗？", "清空", "取消");
        if (!ok)
        {
            return;
        }

        App.History.Clear();
        LoadHistory();
    }

    private void LoadHistory()
    {
        var entries = App.History.GetRecent();
        HistoryView.ItemsSource = entries;
        EmptyLabel.IsVisible = entries.Count == 0;
        HistoryView.IsVisible = entries.Count > 0;
    }
}
