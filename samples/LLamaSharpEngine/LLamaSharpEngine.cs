using System.Diagnostics;
using System.Text;
using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

#if LLAMA_AVAILABLE
using LLama;
using LLama.Common;
using LLama.Sampling;
#endif

namespace IChing.Lab.Engines.LLamaSharp;

/// <summary>
/// 基于 LLamaSharp 的进程内推理引擎（Spec 2 阶段一「模式 A」）。
/// <para>在首次调用 <see cref="GenerateAsync"/> 时按需加载本地 GGUF 模型（懒加载），
/// 通过 <see cref="InteractiveExecutor"/> + <see cref="ChatSession"/> 执行对话式生成。</para>
/// <para>当未定义 <c>LLAMA_AVAILABLE</c> 编译常量（即 LLamaSharp native 依赖未安装）时，
/// 自动回退为桩实现：<see cref="IsReady"/> 恒为 false、<see cref="GenerateAsync"/> 返回固定文本，
/// 以保证在缺 native 依赖的沙箱环境中仍可编译通过。</para>
/// </summary>
public sealed class LLamaSharpEngine : IInferenceEngine
{
    /// <summary>引擎标识：默认绑定 Qwen3-4B 本地模型。</summary>
    public string EngineId => "llama-sharp-qwen3-4b";

    private readonly IConfiguration _configuration;
    private readonly ILogger<LLamaSharpEngine> _logger;

    // 从配置读取的模型加载参数（约定键：LLamaSharp:ModelPath / LLamaSharp:GpuLayerCount / LLamaSharp:ContextSize）。
    private readonly string _modelPath;
    private readonly int _gpuLayerCount;
    private readonly uint _contextSize;

    // 懒加载锁：保证模型在并发首次调用时只加载一次。
    private readonly object _loadLock = new();
    private bool _loadFailed;
    private Exception? _loadException;

#if LLAMA_AVAILABLE
    // 模型懒加载后的运行时对象（首次 GenerateAsync 时初始化，Dispose 时释放）。
    private LLamaWeights? _model;
    private LLamaContext? _context;
    private InteractiveExecutor? _executor;
    private ChatSession? _session;
#endif

    /// <summary>
    /// 构造函数：从 <see cref="IConfiguration"/> 读取 modelPath / gpuLayerCount / contextSize。
    /// 简化处理读取 <c>LLamaSharp:ModelPath</c> 等键（主程序配置或环境变量均可注入）。
    /// </summary>
    /// <param name="configuration">主程序注入的配置。</param>
    /// <param name="logger">日志记录器。</param>
    public LLamaSharpEngine(IConfiguration configuration, ILogger<LLamaSharpEngine> logger)
    {
        _configuration = configuration;
        _logger = logger;

        _modelPath = configuration["LLamaSharp:ModelPath"] ?? string.Empty;
        _gpuLayerCount = int.TryParse(configuration["LLamaSharp:GpuLayerCount"], out var gpu) ? gpu : 0;
        _contextSize = uint.TryParse(configuration["LLamaSharp:ContextSize"], out var ctx) ? ctx : 2048;
    }

    /// <summary>
    /// 引擎是否就绪可立即提供服务。
    /// <para>真实模式下返回模型是否已加载（<c>_model is not null</c>）；
    /// 桩模式下恒为 false（native 依赖未安装，无法真正加载模型）。</para>
    /// </summary>
    public bool IsReady
    {
#if LLAMA_AVAILABLE
        get => _model is not null;
#else
        // 桩模式：LLamaSharp native 依赖未安装，引擎永不就绪。
        get => false;
#endif
    }

    /// <inheritdoc />
    public async Task<GenerationResult> GenerateAsync(string prompt, GenerateOptions options, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var sw = Stopwatch.StartNew();
        try
        {
#if LLAMA_AVAILABLE
            // 首次调用时懒加载模型（线程安全）。
            EnsureLoaded();

            // 构建推理参数：应用 MaxTokens，并在指定 Temperature/TopK/TopP 时配置采样管线。
            var inferenceParams = new InferenceParams
            {
                MaxTokens = options.MaxTokens,
                AntiPrompts = new List<string> { "\nUser:", "User:" },
            };

            if (options.Temperature.HasValue || options.TopK.HasValue || options.TopP.HasValue)
            {
                // DefaultSamplingPipeline 的 Temperature/TopK/TopP 为 init-only 属性，须在对象初始化器中赋值；
                // 调用方未指定的参数回退到 LLamaSharp 默认值（Temperature=0.8、TopK=40、TopP=0.95）。
                var pipeline = new DefaultSamplingPipeline
                {
                    Temperature = options.Temperature ?? 0.8f,
                    TopK = options.TopK ?? 40,
                    TopP = options.TopP ?? 0.95f,
                };
                inferenceParams.SamplingPipeline = pipeline;
            }

            // 以单轮对话形式送入 prompt（每次构建新的 ChatHistory，避免跨调用污染）。
            var history = new ChatHistory();
            history.AddMessage(AuthorRole.User, prompt);

            var sb = new StringBuilder();
            await foreach (var chunk in _session!.ChatAsync(history, inferenceParams, ct))
            {
                sb.Append(chunk);
            }

            sw.Stop();
            return new GenerationResult(
                EngineId: EngineId,
                Text: sb.ToString(),
                IsFallback: false,
                FallbackReason: null,
                ElapsedMs: sw.ElapsedMilliseconds);
#else
            // LLamaSharp native 依赖未安装，使用桩实现返回固定文本。
            await Task.CompletedTask;
            sw.Stop();
            return new GenerationResult(
                EngineId: EngineId,
                Text: "[stub] LLamaSharp native 依赖未安装，无法执行真实推理。",
                IsFallback: true,
                FallbackReason: "LLamaSharp 包未安装或 native 依赖缺失",
                ElapsedMs: sw.ElapsedMilliseconds);
#endif
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "LLamaSharpEngine（{Id}）生成失败。", EngineId);
            return new GenerationResult(
                EngineId: EngineId,
                Text: string.Empty,
                IsFallback: true,
                FallbackReason: ex.Message,
                ElapsedMs: sw.ElapsedMilliseconds);
        }
    }

#if LLAMA_AVAILABLE
    /// <summary>
    /// 懒加载模型：首次调用时用 <see cref="LLamaWeights.LoadFromFile"/> 加载权重，
    /// 创建 <see cref="LLamaContext"/> / <see cref="InteractiveExecutor"/> / <see cref="ChatSession"/>。
    /// 加载失败会标记 <see cref="_loadFailed"/>，后续调用直接抛出原异常，避免重复尝试。
    /// </summary>
    private void EnsureLoaded()
    {
        if (_model is not null)
        {
            return;
        }

        lock (_loadLock)
        {
            if (_model is not null)
            {
                return;
            }

            if (_loadFailed)
            {
                throw new InvalidOperationException(
                    $"LLamaSharpEngine（{EngineId}）模型此前加载失败，已禁止重试。", _loadException);
            }

            if (string.IsNullOrWhiteSpace(_modelPath) || !File.Exists(_modelPath))
            {
                _loadFailed = true;
                _loadException = new FileNotFoundException(
                    $"模型文件未找到或未配置 LLamaSharp:ModelPath（当前值=\"{_modelPath}\"）。", _modelPath);
                throw _loadException;
            }

            _logger.LogInformation(
                "LLamaSharpEngine 正在加载模型 {Path}（GpuLayerCount={Gpu}, ContextSize={Ctx}）。",
                _modelPath, _gpuLayerCount, _contextSize);

            var parameters = new ModelParams(_modelPath)
            {
                ContextSize = _contextSize,
                GpuLayerCount = _gpuLayerCount,
            };

            _model = LLamaWeights.LoadFromFile(parameters);
            _context = _model.CreateContext(parameters, _logger);
            _executor = new InteractiveExecutor(_context, _logger);
            _session = new ChatSession(_executor);

            _logger.LogInformation("LLamaSharpEngine（{Id}）模型加载完成，已就绪。", EngineId);
        }
    }
#endif

    /// <inheritdoc />
    public void Dispose()
    {
#if LLAMA_AVAILABLE
        _session = null;
        // InteractiveExecutor 本身未实现 IDisposable（见 CS1061），其持有的 context 由下方显式释放。
        _executor = null;
        // LLamaContext / LLamaWeights 均为 IDisposable，native 句柄释放幂等，释放顺序：context → weights。
        _context?.Dispose();
        _context = null;
        _model?.Dispose();
        _model = null;
#endif
    }
}
