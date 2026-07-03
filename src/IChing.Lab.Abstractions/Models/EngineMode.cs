namespace IChing.Lab.Abstractions.Models;

/// <summary>
/// 解读引擎运行模式。
/// </summary>
public enum EngineMode
{
    /// <summary>进程内直接调用（例如本地 ONNX 模型）。</summary>
    InProcess,

    /// <summary>本地 HTTP 服务调用（例如本地推理服务器）。</summary>
    LocalHttp,

    /// <summary>远程 API 调用（例如云端模型 API）。</summary>
    RemoteApi
}
