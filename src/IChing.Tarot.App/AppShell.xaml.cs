namespace IChing.Tarot.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("history-detail", typeof(Pages.HistoryDetailPage));
        Routing.RegisterRoute("spirit-card", typeof(Pages.SpiritCardPage));
        Routing.RegisterRoute("element-quiz", typeof(Pages.ElementQuizPage));
        Routing.RegisterRoute("daily-color", typeof(Pages.DailyColorPage));
    }
}
