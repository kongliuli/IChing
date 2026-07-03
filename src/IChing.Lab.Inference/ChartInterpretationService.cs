using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntimeGenAI;

namespace IChing.Lab.Inference;

public sealed class ChartInterpretationService : IDisposable
{
    private readonly string _modelPath;
    private readonly ILogger<ChartInterpretationService> _logger;
    private Model? _model;
    private Tokenizer? _tokenizer;
    private readonly object _gate = new();

    public ChartInterpretationService(string modelPath, ILogger<ChartInterpretationService> logger)
    {
        _modelPath = modelPath;
        _logger = logger;
    }

    public bool IsModelLoaded => _model is not null;

    public InterpretResult Interpret(object chartJson, string? focus = null, int maxTokens = 256)
    {
        EnsureModel();
        if (_model is null || _tokenizer is null)
        {
            return TemplateFallback(chartJson, focus, "model not loaded");
        }

        var chart = JsonSerializer.Serialize(chartJson);
        var prompt = $"""
            <|im_start|>user
            以下命盘由系统计算，请勿修改干支数据。请用中文写一段不超过200字的解读。
            关注角度：{focus ?? "综合"}
            命盘JSON：
            {chart}
            
            <|im_start|>assistant
            """;

        try
        {
            var sequences = _tokenizer.Encode(prompt);
            using var parameters = new GeneratorParams(_model);
            parameters.SetSearchOption("max_length", maxTokens);

            using var generator = new Generator(_model, parameters);
            generator.AppendTokenSequences(sequences);

            using var stream = _tokenizer.CreateStream();
            var output = new StringBuilder();
            while (!generator.IsDone())
            {
                generator.GenerateNextToken();
                output.Append(stream.Decode(generator.GetSequence(0)[^1]));
            }

            return new InterpretResult("qwen3-0.6b-onnx-genai", output.ToString().Trim(), false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ONNX inference failed, using template fallback");
            return TemplateFallback(chartJson, focus, ex.Message);
        }
    }

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

            _model = new Model(_modelPath);
            _tokenizer = new Tokenizer(_model);
            _logger.LogInformation("Loaded ONNX GenAI model from {Path}", _modelPath);
        }
    }

    private static InterpretResult TemplateFallback(object chartJson, string? focus, string reason)
    {
        var text = $"[模板解读·{focus ?? "综合"}] 系统已记录命盘结构。当前 ONNX 模型未就绪（{reason}），请运行 scripts/download-qwen-model.sh 后重试。";
        return new InterpretResult("template-fallback", text, true);
    }

    public void Dispose()
    {
        _tokenizer?.Dispose();
        _model?.Dispose();
    }
}

public record InterpretResult(string Engine, string Text, bool IsFallback);
