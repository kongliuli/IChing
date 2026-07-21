using System.Net.Http;

namespace IChing.Client.Shared.Onnx;

/// <summary>
/// 端侧模型下载器：HuggingFace resolve/main + 本地仓库导入。
/// </summary>
public sealed class LocalModelDownloader
{
    private readonly HttpClient _http;

    public LocalModelDownloader(HttpClient? http = null)
    {
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromHours(2) };
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("IChingTarot/1.0 (local onnx model downloader)");
        }
    }

    public async Task<string> EnsurePackAsync(
        OnnxModelPack pack,
        string targetDirectory,
        IProgress<ModelDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(targetDirectory);
        var total = pack.Files.Count;
        for (var i = 0; i < pack.Files.Count; i++)
        {
            var fileName = pack.Files[i];
            var dest = Path.Combine(targetDirectory, fileName);
            if (File.Exists(dest) && new FileInfo(dest).Length > 0)
            {
                progress?.Report(new ModelDownloadProgress(fileName, i + 1, total, "skip", 1));
                continue;
            }

            var url = $"https://huggingface.co/{pack.HuggingFaceRepo}/resolve/main/{Uri.EscapeDataString(fileName).Replace("%2F", "/")}";
            progress?.Report(new ModelDownloadProgress(fileName, i + 1, total, "download", 0));
            await DownloadFileAsync(url, dest, fraction =>
            {
                progress?.Report(new ModelDownloadProgress(fileName, i + 1, total, "download", fraction));
            }, cancellationToken);
            progress?.Report(new ModelDownloadProgress(fileName, i + 1, total, "done", 1));
        }

        var config = Path.Combine(targetDirectory, "genai_config.json");
        if (!File.Exists(config))
        {
            throw new InvalidOperationException($"模型目录缺少 genai_config.json: {targetDirectory}");
        }

        return targetDirectory;
    }

    /// <summary>开发机：把仓库 models/ 整包复制到 AppData（跳过已存在文件）。</summary>
    public static string? TryImportFromDevRepo(string modelId, string appModelsRoot, IProgress<string>? progress = null)
    {
        var source = OnnxModelPackCatalog.DevRepoModelCandidates(modelId)
            .FirstOrDefault(p => File.Exists(Path.Combine(p, "genai_config.json")));
        if (source is null)
        {
            return null;
        }

        var dest = OnnxModelPackCatalog.CombineModelDirectory(appModelsRoot, modelId);
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.EnumerateFiles(source))
        {
            var name = Path.GetFileName(file);
            var target = Path.Combine(dest, name);
            if (File.Exists(target) && new FileInfo(target).Length > 0)
            {
                progress?.Report($"skip {name}");
                continue;
            }

            progress?.Report($"copy {name}");
            File.Copy(file, target, overwrite: true);
        }

        return dest;
    }

    public async Task<string> EnsureModelAsync(
        string targetDirectory,
        IReadOnlyList<Uri> fileUrls,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(targetDirectory);
        foreach (var url in fileUrls)
        {
            var fileName = Path.GetFileName(url.LocalPath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                continue;
            }

            var dest = Path.Combine(targetDirectory, fileName);
            if (File.Exists(dest) && new FileInfo(dest).Length > 0)
            {
                progress?.Report($"skip {fileName}");
                continue;
            }

            progress?.Report($"download {fileName}");
            await DownloadFileAsync(url.ToString(), dest, null, cancellationToken);
        }

        var config = Path.Combine(targetDirectory, "genai_config.json");
        if (!File.Exists(config))
        {
            throw new InvalidOperationException($"模型目录缺少 genai_config.json: {targetDirectory}");
        }

        return targetDirectory;
    }

    private async Task DownloadFileAsync(
        string url,
        string dest,
        Action<double>? onFraction,
        CancellationToken cancellationToken)
    {
        var temp = dest + ".partial";
        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = File.Create(temp);
        var buffer = new byte[1024 * 256];
        long readTotal = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            readTotal += read;
            if (total is > 0)
            {
                onFraction?.Invoke(Math.Clamp(readTotal / (double)total.Value, 0, 1));
            }
        }

        await output.FlushAsync(cancellationToken);
        if (File.Exists(dest))
        {
            File.Delete(dest);
        }

        File.Move(temp, dest);
    }
}

public sealed record ModelDownloadProgress(
    string FileName,
    int Index,
    int Total,
    string Phase,
    double FileFraction);
