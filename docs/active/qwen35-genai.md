# Qwen3.5 端侧 GenAI 接入说明

> 编写模型: Claude Fable 5 (Cursor Agent) · 2026-07-16

## 结论（先看这个）

| 路径 | 能否直接给 `LocalOnnxProvider` / ORT GenAI 用 | 说明 |
|------|-----------------------------------------------|------|
| **现成可用** | ✅ `models/qwen2.5-1.5b-genai` | 已有 `genai_config.json`，App 下载器默认档 |
| HuggingFace `onnx-community/Qwen3.5-0.8B-ONNX*` | ❌ | Transformers.js / ORT **非 GenAI** 包，缺 `genai_config.json`，不能直接喂给 ORT GenAI |
| 社区超大 Qwen3.5-35B GenAI | ⚠️ 工程不可用 | 需 patched ORT GenAI + custom op，体积过大，不适配手机 |
| **自建 GenAI 包** | ✅ 推荐下一阶段 | 用官方 `onnxruntime_genai.models.builder` 从 HF 权重导出 INT4 |

免费版**不接**端侧 AI。DevShell / 有 `AllowLocalOnnx` 的版本才下载。

## 当前 App 怎么下 2.5（已接通）

设置页（DevShell）→「从 HuggingFace 下载」或「导入本地 models/」：

```powershell
# 与 App 下载器同一清单
.\scripts\download-qwen-15b-model.ps1 -Target .\models\qwen2.5-1.5b-genai
```

Repo：`tonythethompson/Qwen2.5-1.5B-Instruct-ONNX`

## 如何做出 Qwen3.5 的 ORT GenAI 包

前提：Python 3.10+、磁盘 ≥ 10GB、能访问 Hugging Face。

```powershell
# 1) 安装 builder
pip install onnxruntime-genai

# 2) 导出（以 0.8B 为例；模型 id 以 HF 上实际卡为准）
python -m onnxruntime_genai.models.builder `
  -m Qwen/Qwen3.5-0.8B `
  -o .\models\qwen3.5-0.8b-genai `
  -p int4 `
  -e cpu

# 3) 校验
Test-Path .\models\qwen3.5-0.8b-genai\genai_config.json
```

若 builder 报「不支持 Qwen3.5 / GatedDeltaNet」：

- 升级到最新 `onnxruntime-genai` nightly，或
- 等上游 Olive recipe（微软 `olive-recipes` 已有 Qwen3.5-27B CUDA 示例，手机侧仍等轻量 INT4 成熟）

导出成功后：

1. 在 `OnnxModelPackCatalog` 增加 pack 条目（文件列表用目录 `Get-ChildItem` 生成）
2. 把 `Qwen35ModelCatalog.DefaultDownloadId` 切到 `qwen3.5-0.8b-genai` 或 `2b`
3. DevShell 设置页下载 / 或 `LocalModelDownloader.TryImportFromDevRepo`
4. `dotnet run --project src/IChing.Lab.PromptTest -- --model ./models/qwen3.5-0.8b-genai` 做质量烟雾

辅助脚本骨架：`scripts/build-qwen35-genai.ps1`（本仓库已加）。

## 不要做的事

- 不要把 `onnx-community/Qwen3.5-*-ONNX` 当 GenAI 目录直接塞进 App（会缺 `genai_config.json`，引擎初始化失败后静默降级规则）
- 不要把 35B GenAI 打进手机包
