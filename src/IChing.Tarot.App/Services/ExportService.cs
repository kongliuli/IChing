namespace IChing.Tarot.App.Services;

/// <summary>将 MAUI 视图截图为 PNG 并保存/分享。</summary>
public static class ExportService
{
    private const int ExportWidth = 1080;
    private const int MaxCaptureHeight = 12000;

    public static async Task<string?> CaptureAndSaveAsync(
        View content,
        string fileName,
        CancellationToken ct = default)
    {
        var shell = Shell.Current;
        if (shell is null)
        {
            return null;
        }

        try
        {
            var exportRoot = new VerticalStackLayout
            {
                WidthRequest = ExportWidth,
                BackgroundColor = Color.FromArgb("#0B0812"),
                Spacing = 0,
                VerticalOptions = LayoutOptions.Start
            };
            exportRoot.Add(content);

            var page = new ContentPage
            {
                BackgroundColor = Color.FromArgb("#0B0812"),
                Padding = 0,
                Content = exportRoot
            };

            await shell.Navigation.PushModalAsync(page, false);
            try
            {
                await WaitForLayoutAsync(exportRoot, ct);
                if (exportRoot.HeightRequest > MaxCaptureHeight)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Export] capture height capped: {exportRoot.HeightRequest}");
                }

                var result = await exportRoot.CaptureAsync();
                if (result is null)
                {
                    return null;
                }

                var dir = Path.Combine(FileSystem.CacheDirectory, "exports");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, $"{SanitizeFileName(fileName)}.png");

                await using (var stream = await result.OpenReadAsync())
                await using (var file = File.Create(path))
                {
                    await stream.CopyToAsync(file, ct);
                }

                var saved = await GallerySave.TrySaveAsync(path, ct);
                return saved ?? path;
            }
            finally
            {
                await shell.Navigation.PopModalAsync(false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Export] {ex}");
            return null;
        }
    }

    private static async Task WaitForLayoutAsync(VisualElement element, CancellationToken ct)
    {
        for (var i = 0; i < 24; i++)
        {
            ct.ThrowIfCancellationRequested();
            element.Measure(ExportWidth, double.PositiveInfinity);
            var h = element.DesiredSize.Height;
            if (h > 0)
            {
                element.WidthRequest = ExportWidth;
                element.HeightRequest = Math.Min(h, MaxCaptureHeight);
                await Task.Delay(80, ct);
                return;
            }

            await Task.Delay(50, ct);
        }
    }

    public static async Task ShareFileAsync(string path, string title = "分享长图")
    {
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = title,
            File = new ShareFile(path)
        });
    }

    public static View BuildHeader(string title, string? subtitle = null)
    {
        var stack = new VerticalStackLayout { Spacing = 6, Padding = new Thickness(24, 28, 24, 12) };
        stack.Add(new Label
        {
            Text = title,
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#D4AF37")
        });
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            stack.Add(new Label
            {
                Text = subtitle,
                FontSize = 14,
                TextColor = Color.FromArgb("#B8AEC9"),
                LineBreakMode = LineBreakMode.WordWrap
            });
        }

        stack.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#9A7B2C"), Margin = new Thickness(0, 8, 0, 0) });
        return stack;
    }

    public static View BuildTextBlock(string heading, string body, bool highlight = false)
    {
        return new Border
        {
            Padding = 16,
            Margin = new Thickness(20, 0, 20, 12),
            BackgroundColor = highlight ? Color.FromArgb("#2A1F3D") : Color.FromArgb("#161022"),
            Stroke = Color.FromArgb("#9A7B2C"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label
                    {
                        Text = heading,
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#B794F6")
                    },
                    new Label
                    {
                        Text = body,
                        FontSize = 15,
                        TextColor = Color.FromArgb("#F5F0E8"),
                        LineBreakMode = LineBreakMode.WordWrap
                    }
                }
            }
        };
    }

    public static View BuildFooter() =>
        new Label
        {
            Text = $"星轨塔罗 · v{AppInfo.Current.VersionString}",
            FontSize = 12,
            TextColor = Color.FromArgb("#6E6380"),
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 16, 0, 28)
        };

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "export" : cleaned[..Math.Min(cleaned.Length, 48)];
    }
}
