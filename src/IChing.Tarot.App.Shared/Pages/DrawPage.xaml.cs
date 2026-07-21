using IChing.Client.Shared.Editions;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Tarot;
using IChing.Lab.Presentation;
using IChing.Tarot.App.Services;
using IChing.Tarot.App.Views;

namespace IChing.Tarot.App.Pages;

public partial class DrawPage : ContentPage
{
    private TarotReading? _currentReading;
    private List<Models.CardDisplayItem> _displayCards = [];
    private List<Models.InterpretationSectionItem> _interpretationSections = [];
    private string _engineId = "iching-tarot-built-in";
    private string _interpretationRaw = string.Empty;
    private readonly List<TarotSpread> _spreads = [];

    public DrawPage()
    {
        InitializeComponent();
        SizeChanged += (_, _) => UpdateResponsiveLayout();
        LoadSpreads();
        UpdateHistoryPanel();
        ApplyEditionChrome();
    }

    /// <summary>
    /// WinUI WebView 点击目录锚点会跳到 https://appdir/#sec-n 并 ERR_ACCESS_DENIED；拦截非 about/data 导航。
    /// </summary>
    private void OnInterpretationWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        var url = e.Url ?? string.Empty;
        if (url.Contains("appdir", StringComparison.OrdinalIgnoreCase)
            || url.Contains("#sec-", StringComparison.OrdinalIgnoreCase)
            || (url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                && !url.StartsWith("about:", StringComparison.OrdinalIgnoreCase)))
        {
            e.Cancel = true;
        }
    }

    private void ApplyEditionChrome()
    {
        var ai = EditionHost.Capabilities.AllowAiInterpretation;
        InterpretButton.IsVisible = ai;
        FollowUpButton.IsVisible = false;
        InterpretationPanel.IsVisible = false;
        if (!ai)
        {
            InterpretButton.IsEnabled = false;
            if (InterpretButton.Parent is Grid actionGrid)
            {
                var redraw = actionGrid.Children.OfType<Button>().FirstOrDefault(b => b != InterpretButton);
                if (redraw is not null)
                {
                    Grid.SetColumn(redraw, 0);
                    Grid.SetColumnSpan(redraw, 2);
                }
            }
        }

        EmptyStateHintLabel.Text = ai
            ? "选择单牌、三牌、六牌或凯尔特十字后，这里会展示牌位、正逆位、牌义摘要和 AI 解读入口。"
            : "选择牌阵后抽牌，即可查看牌面、正逆位与基本牌义。免费版不含 AI 解读。";
    }

    private void UpdateResponsiveLayout()
    {
        if (double.IsNaN(Width) || Width <= 0)
        {
            return;
        }

        var wide = Width >= 980;
        var margin = wide ? 96 : 32;
        PageBody.WidthRequest = Math.Min(wide ? 1360 : 680, Math.Max(320, Width - margin));

        ResponsiveGrid.ColumnDefinitions.Clear();
        ResponsiveGrid.RowDefinitions.Clear();
        ResponsiveGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        if (wide)
        {
            ResponsiveGrid.ColumnDefinitions[0] = new ColumnDefinition(new GridLength(420));
            ResponsiveGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            ResponsiveGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Grid.SetColumn(LeftColumn, 0);
            Grid.SetRow(LeftColumn, 0);
            Grid.SetColumn(RightColumn, 1);
            Grid.SetRow(RightColumn, 0);
            return;
        }

        ResponsiveGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        ResponsiveGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        Grid.SetColumn(LeftColumn, 0);
        Grid.SetRow(LeftColumn, 0);
        Grid.SetColumn(RightColumn, 0);
        Grid.SetRow(RightColumn, 1);
    }

    private void LoadSpreads()
    {
        _spreads.Clear();
        _spreads.AddRange(App.Tarot.ListSpreads());
        SpreadPicker.ItemsSource = _spreads.Select(s => s.TitleZh).ToList();
        SpreadPicker.SelectedIndex = _spreads.FindIndex(s => s.Id == "past-present-future");
        if (SpreadPicker.SelectedIndex < 0 && _spreads.Count > 0)
        {
            SpreadPicker.SelectedIndex = 0;
        }

        OnSpreadChanged(null, EventArgs.Empty);
    }

    private void OnSpreadChanged(object? sender, EventArgs e)
    {
        var idx = SpreadPicker.SelectedIndex;
        if (idx < 0 || idx >= _spreads.Count)
        {
            return;
        }

        var spread = _spreads[idx];
        SpreadDescriptionLabel.Text = $"{spread.Description} · {spread.CardCount} 张";
        SpreadDifficultyLabel.Text = spread.Difficulty switch
        {
            "easiest" => "入门",
            "intermediate" => "进阶",
            "advanced" => "高阶",
            _ => spread.Difficulty
        };
    }

    private void OnAdvancedToggleClicked(object? sender, EventArgs e)
    {
        AdvancedPanel.IsVisible = !AdvancedPanel.IsVisible;
        AdvancedToggleButton.Text = AdvancedPanel.IsVisible ? "隐藏高级选项" : "显示高级选项";
    }

    private void OnSpreadToggleClicked(object? sender, EventArgs e)
    {
        SpreadBoardPanel.IsVisible = !SpreadBoardPanel.IsVisible;
        SpreadToggleButton.Text = SpreadBoardPanel.IsVisible ? "隐藏牌阵" : "显示牌阵";
    }

    private async void OnDrawClicked(object? sender, EventArgs e)
    {
        var idx = SpreadPicker.SelectedIndex;
        if (idx < 0 || idx >= _spreads.Count)
        {
            return;
        }

        int? seed = int.TryParse(SeedEntry.Text, out var parsed) ? parsed : null;
        var spread = _spreads[idx];

        DrawButton.IsEnabled = false;
        var drawLabel = DrawButton.Text;
        DrawButton.Text = "洗牌中...";

        try
        {
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch
            {
                // ponytail: some desktop targets do not support haptics.
            }

            await Task.Delay(180);

            var result = App.Tarot.Draw(spread.Id, QuestionEntry.Text, seed);
            _currentReading = result.Reading;
            _engineId = result.EngineId;
            await ShowReadingAsync(_currentReading);
            App.History.Add(_currentReading, _engineId);
            UpdateHistoryPanel();
            InterpretationPanel.IsVisible = false;
            FollowUpButton.IsVisible = false;
            InterpretationBoardLayout.Render(InterpretationBoardHost, []);
            _interpretationRaw = string.Empty;
            InterpretButton.IsEnabled = EditionHost.Capabilities.AllowAiInterpretation;
            EmptyStatePanel.IsVisible = false;

            await MainScroll.ScrollToAsync(ReadingPanel, ScrollToPosition.Start, true);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("抽牌失败", UserFacingZh.Error(ex.Message), "好的");
        }
        finally
        {
            DrawButton.Text = drawLabel;
            DrawButton.IsEnabled = true;
        }
    }

    private void OnRedrawClicked(object? sender, EventArgs e) => OnDrawClicked(sender, e);

    private async Task ShowReadingAsync(TarotReading reading)
    {
        ReadingPanel.IsVisible = true;
        ReadingPanel.Opacity = 0;
        ReadingPanel.Scale = 0.96;
        ReadingTitleLabel.Text = reading.Question is { Length: > 0 } q
            ? $"《{q}》· {reading.SpreadTitleZh}"
            : reading.SpreadTitleZh;
        ReadingMetaLabel.Text =
            $"引擎 {UserFacingZh.EngineLabel(_engineId)} · 种子 {reading.Seed?.ToString() ?? "随机"} · {reading.Positions.Count} 张 · 牌义覆盖 {TarotReadingStats.CoveragePercent(reading)}%";

        var cards = await CardDisplayMapper.FromReadingAsync(reading);
        _displayCards = cards;
        SpreadBoardPanel.IsVisible = true;
        SpreadToggleButton.Text = "隐藏牌阵";
        SpreadBoardLayout.Render(SpreadBoardHost, reading.SpreadId, cards);

        await Task.WhenAll(
            ReadingPanel.FadeToAsync(1, 260),
            ReadingPanel.ScaleToAsync(1, 260, Easing.CubicOut));
    }

    private async void OnHistorySelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not HistoryEntry entry)
        {
            return;
        }

        HistoryCollection.SelectedItem = null;
        var index = App.History.GetRecent().ToList().IndexOf(entry);
        if (index < 0)
        {
            return;
        }

        await Navigation.PushAsync(new HistoryDetailPage(index));
    }

    private async void OnInterpretClicked(object? sender, EventArgs e)
    {
        if (_currentReading is null)
        {
            return;
        }

        if (!EditionHost.Capabilities.AllowAiInterpretation)
        {
            await DisplayAlertAsync("免费版", "免费版仅提供牌面与基本牌义，不含 AI 解读。", "好的");
            return;
        }

        // 端侧 ONNX 已导入时无需 API Key / Lab
        var canUseLocalOnnx = EditionHost.Capabilities.AllowLocalOnnx && App.Settings.HasLocalOnnxModel;
        if (!App.Settings.IsConfigured && !App.Settings.UseLabApi && !canUseLocalOnnx
            && EditionHost.Capabilities.ShowApiKeySettings)
        {
            await DisplayAlertAsync(
                "需要配置",
                "请先在「设置」填写 API Key、选择本机 Ollama，或导入端侧 ONNX 模型后再解读。",
                "去设置");
            await Shell.Current.GoToAsync("//settings");
            return;
        }

        if (EditionHost.Capabilities.Kind == EditionKind.Commercial
            && !App.Settings.IsLabConfigured)
        {
            await DisplayAlertAsync("需要服务地址", "请先在设置中填写 Lab 服务地址。", "去设置");
            await Shell.Current.GoToAsync("//settings");
            return;
        }

        InterpretButton.IsEnabled = false;
        var interpretLabel = InterpretButton.Text;
        InterpretButton.Text = "解读中...";
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        if (canUseLocalOnnx && !App.Settings.IsConfigured && !App.Settings.UseLabApi)
        {
            InterpretationStatusLabel.Text = "本地 ONNX 加载/推理中（首次较慢），界面应保持可点…";
            InterpretationStatusLabel.TextColor = Color.FromArgb("#E0A84C");
            InterpretationStatusLabel.IsVisible = true;
        }

        try
        {
            var result = await App.Interpretation.InterpretAsync(
                App.Settings,
                _currentReading,
                QuestionEntry.Text);

            var displayText = ReadingPromptProtocol.NormalizeOutput(result.Text);
            var sections = InterpretationSectionParser.Parse(displayText);
            _interpretationRaw = displayText;
            _interpretationSections = sections.ToList();
            if (string.IsNullOrWhiteSpace(displayText))
            {
                InterpretationPanel.IsVisible = false;
            }
            else
            {
                InterpretationPanel.IsVisible = true;
                InterpretationWebView.Source = new HtmlWebViewSource
                {
                    Html = ReadingHtmlFormatter.ToTarotDocument("塔罗解读报告", _currentReading.SpreadTitleZh, displayText, _currentReading)
                };
                InterpretationBoardLayout.Render(InterpretationBoardHost, sections, _currentReading);
            }

            if (result.IsFallback && EditionHost.Capabilities.Kind == EditionKind.Commercial)
            {
                InterpretationStatusLabel.Text = "服务暂不可用，已展示基本牌义";
            }
            else
            {
                InterpretationStatusLabel.Text = result.IsFallback
                    ? $"降级/部分失败：{(string.IsNullOrWhiteSpace(result.Error) ? "已使用规则摘要" : UserFacingZh.Error(result.Error))}"
                    : canUseLocalOnnx && string.IsNullOrWhiteSpace(App.Settings.ApiKey) && !App.Settings.UseLabApi
                        ? "解读成功 · 本地 ONNX"
                        : $"解读成功 · {UserFacingZh.ProviderLabel(App.Settings.Provider)} / {App.Settings.Model}";
            }
            InterpretationStatusLabel.TextColor = result.IsFallback
                ? Color.FromArgb("#E06C75")
                : Color.FromArgb("#7FD992");
            FollowUpButton.IsVisible = EditionHost.Capabilities.AllowFollowUp
                && !string.IsNullOrWhiteSpace(_interpretationRaw);

            if (!string.IsNullOrWhiteSpace(displayText))
            {
                App.History.UpdateLatestInterpretation(displayText, _currentReading.Seed);
            }

            // 广告位挂载点（NoOp 时高度为 0）
            MonetizationHost.IsVisible = EditionHost.Capabilities.ShowMonetizationSlots;
            if (MonetizationHost.IsVisible && MonetizationHost.Children.Count == 0)
            {
                foreach (var slot in EditionHost.MonetizationSlots)
                {
                    MonetizationHost.Add(new Views.MonetizationSlotView(slot));
                }
            }

            await MainScroll.ScrollToAsync(InterpretationPanel, ScrollToPosition.End, true);
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            InterpretButton.Text = interpretLabel;
            InterpretButton.IsEnabled = true;
        }
    }

    private async void OnFollowUpClicked(object? sender, EventArgs e)
    {
        if (_currentReading is null || string.IsNullOrWhiteSpace(_interpretationRaw))
        {
            return;
        }

        var input = FollowUpPromptTemplates.TarotExchangeInput(_currentReading, QuestionEntry.Text);
        var sessionId = App.Sessions.CreateSessionWithInitial(
            "tarot",
            App.Settings.InterpretTier,
            _currentReading,
            null,
            input,
            _interpretationRaw);
        await FollowUpSessionRegistrar.RegisterLabIfNeededAsync(
            App.Settings,
            sessionId,
            "tarot",
            App.Settings.InterpretTier,
            input,
            _interpretationRaw,
            _currentReading);
        await Navigation.PushAsync(new FollowUpChatPage(new FollowUpChatArgs("塔罗追问", "tarot", sessionId)));
    }

    private async void OnCopyInterpretationClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_interpretationRaw))
        {
            return;
        }

        await Clipboard.Default.SetTextAsync(_interpretationRaw);
        await DisplayAlertAsync("已复制", "解读内容已复制到剪贴板。", "好的");
    }

    private async void OnCopySpreadClicked(object? sender, EventArgs e)
    {
        if (_currentReading is null)
        {
            return;
        }

        var lines = _currentReading.Positions.Select(p =>
            $"[{p.PositionTitleZh}] {p.CardNameZh} · {(p.Reversed ? "逆位" : "正位")}\n{p.Meaning}");
        var text = string.Join("\n\n", lines);
        await Clipboard.Default.SetTextAsync(text);
        await DisplayAlertAsync("已复制", "牌阵内容已复制到剪贴板。", "好的");
    }

    private async void OnSaveSpreadImageClicked(object? sender, EventArgs e)
    {
        if (_currentReading is null || _displayCards.Count == 0)
        {
            return;
        }

        SaveSpreadButton.IsEnabled = false;
        try
        {
            var view = ReadingExportBuilder.BuildSpread(_currentReading, _displayCards);
            var path = await ExportService.CaptureAndSaveAsync(view, "牌阵");
            if (path is null)
            {
                await DisplayAlertAsync("保存失败", "无法生成牌阵长图。", "好的");
                return;
            }

            await DisplayAlertAsync("已保存", "牌阵长图已保存到相册或缓存目录。", "好的");
        }
        finally
        {
            SaveSpreadButton.IsEnabled = true;
        }
    }

    private async void OnSaveInterpretationImageClicked(object? sender, EventArgs e)
    {
        if (_currentReading is null || _interpretationSections.Count == 0)
        {
            return;
        }

        SaveInterpretButton.IsEnabled = false;
        try
        {
            var view = ReadingExportBuilder.BuildInterpretation(_currentReading, _interpretationSections);
            var path = await ExportService.CaptureAndSaveAsync(view, "解读");
            if (path is null)
            {
                await DisplayAlertAsync("保存失败", "无法生成解读长图。", "好的");
                return;
            }

            await DisplayAlertAsync("已保存", "解读长图已保存到相册或缓存目录。", "好的");
        }
        finally
        {
            SaveInterpretButton.IsEnabled = true;
        }
    }

    private void UpdateHistoryPanel()
    {
        var recent = App.History.GetRecent();
        HistoryPanel.IsVisible = recent.Count > 0;
        HistoryCollection.ItemsSource = recent;
    }

    private async void OnClearHistoryClicked(object? sender, EventArgs e)
    {
        if (!await DisplayAlertAsync("清空历史", "确定删除全部抽牌记录？", "清空", "取消"))
        {
            return;
        }

        App.History.Clear();
        UpdateHistoryPanel();
    }
}
