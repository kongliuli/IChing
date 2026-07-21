using System.Diagnostics;
using System.Text;
using IChing.Lab.Abstractions.Engines;
using LabModels = IChing.Lab.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntimeGenAI;

namespace IChing.Lab.Inference.Engines;

/// <summary>
/// 基于 ONNX Runtime GenAI 的解读引擎实现，封装 Qwen2.5-1.5B 模型的加载与生成。
/// 所有 Model/Tokenizer 加载逻辑均在此类内完成，编排器不感知。
/// </summary>
public sealed class OnnxGenAiEngine : IInferenceEngine
{
    private const int PromptTokenBuffer = 2048;
    private const int ContextLimit = 32768;
    private readonly string _modelPath;
    private readonly ILogger<OnnxGenAiEngine> _logger;
    private Model? _model;
    private Tokenizer? _tokenizer;
    private readonly object _gate = new();

    public OnnxGenAiEngine(string modelPath, ILogger<OnnxGenAiEngine> logger)
    {
        _modelPath = modelPath;
        _logger = logger;
    }

    /// <inheritdoc />
    public string EngineId => "onnx-genai-qwen2.5-1.5b";

    /// <inheritdoc />
    public bool IsReady => _model is not null;

    /// <inheritdoc />
    public Task<LabModels.GenerationResult> GenerateAsync(
        string prompt,
        LabModels.GenerateOptions options,
        CancellationToken ct)
    {
        // 模型加载与推理均为 CPU 同步重活；必须离开 UI 线程，否则 MAUI 会「未响应」
        return Task.Run(() => GenerateCore(prompt, options, ct), ct);
    }

    private LabModels.GenerationResult GenerateCore(
        string prompt,
        LabModels.GenerateOptions options,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        EnsureModel();

        if (_model is null || _tokenizer is null)
        {
            return new LabModels.GenerationResult(
                EngineId,
                Text: string.Empty,
                IsFallback: true,
                FallbackReason: "model not loaded",
                ElapsedMs: 0);
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var text = RunGeneration(prompt, options.MaxTokens, ct);
            sw.Stop();
            return new LabModels.GenerationResult(
                EngineId,
                text,
                IsFallback: false,
                FallbackReason: null,
                sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return new LabModels.GenerationResult(
                EngineId,
                Text: string.Empty,
                IsFallback: true,
                FallbackReason: "cancelled",
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "ONNX inference failed");
            return new LabModels.GenerationResult(
                EngineId,
                Text: string.Empty,
                IsFallback: true,
                FallbackReason: ex.Message,
                sw.ElapsedMilliseconds);
        }
    }

    // 执行单次生成，流式拼接 token。
    private string RunGeneration(string prompt, int maxTokens, CancellationToken ct)
    {
        var sequences = _tokenizer!.Encode(prompt);
        using var parameters = new GeneratorParams(_model!);
        parameters.SetSearchOption("max_length", Math.Min(maxTokens + PromptTokenBuffer, ContextLimit));

        using var generator = new Generator(_model!, parameters);
        generator.AppendTokenSequences(sequences);

        using var stream = _tokenizer.CreateStream();
        var output = new StringBuilder();
        while (!generator.IsDone())
        {
            ct.ThrowIfCancellationRequested();
            generator.GenerateNextToken();
            output.Append(stream.Decode(generator.GetSequence(0)[^1]));
        }

        return output.ToString().Trim();
    }

    // 懒加载模型与分词器；路径或配置缺失时保持未加载状态。
    private void EnsureModel()
    {
        if (_model is not null)
        {
            return;
        }

        lock (_gate)
        {
            if (_model is not null)
            {
                return;
            }

            if (!Directory.Exists(_modelPath))
            {
                _logger.LogWarning("ONNX model path not found: {Path}", _modelPath);
                return;
            }

            if (!File.Exists(Path.Combine(_modelPath, "genai_config.json")))
            {
                _logger.LogWarning("genai_config.json missing under {Path}", _modelPath);
                return;
            }

            _model = new Model(_modelPath);
            _tokenizer = new Tokenizer(_model);
            _logger.LogInformation("Loaded ONNX GenAI model from {Path}", _modelPath);
        }
    }

    public void Dispose()
    {
        _tokenizer?.Dispose();
        _model?.Dispose();
    }
}
