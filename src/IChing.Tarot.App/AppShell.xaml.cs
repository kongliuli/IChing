namespace IChing.Tarot.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("history-detail", typeof(Pages.HistoryDetailPage));
    }
}
