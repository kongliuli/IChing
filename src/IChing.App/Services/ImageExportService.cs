namespace IChing.App.Services;

public static class ImageExportService
{
    public static async Task<string> ExportVisibleAsync(string prefix)
    {
        if (!Screenshot.Default.IsCaptureSupported)
        {
            throw new NotSupportedException("当前平台不支持截图导出。");
        }

        var screenshot = await Screenshot.Default.CaptureAsync();
        var path = Path.Combine(
            FileSystem.AppDataDirectory,
            $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}.png");

        await using (var input = await screenshot.OpenReadAsync())
        await using (var output = File.Create(path))
        {
            await input.CopyToAsync(output);
        }

        if (new FileInfo(path).Length == 0)
        {
            throw new IOException("导出的图片为空。");
        }

        return path;
    }
}
