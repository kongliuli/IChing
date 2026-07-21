using IChing.Tarot.App.Models;
using IChing.Tarot.App.Pages;

namespace IChing.Tarot.App.Services;

public static class ExploreModuleRouter
{
    public static async Task NavigateAsync(ContentPage page, ExploreModuleItemConfig item)
    {
        var param = item.ActionParam ?? item.Id;
        Page? next = item.Action switch
        {
            "personality-quiz" => new PersonalityQuizPage(param),
            "spirit-card" => new SpiritCardPage(),
            "element-quiz" => new ElementQuizPage(),
            "daily-color" => new DailyColorPage(),
            _ => null
        };

        if (next is null)
        {
            await page.DisplayAlertAsync("未安装模块", $"未知 action：{item.Action}", "好的");
            return;
        }

        await page.Navigation.PushAsync(next);
    }
}
