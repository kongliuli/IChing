# ONNX 大模型调研（可直接使用）

> 面向 IChing **解读层**（报告文案、对话、塔罗 Layer2），**不用于**八字干支计算。  
> 调研日期：2026-07-03

---

## 选型维度

| 维度 | 建议 |
|------|------|
| 任务 | 中文命理解读 / 对话 / 叙事合成 |
| 部署 | 本地 CPU 优先（调研 Lab），后续 GPU |
| 运行时 | **ONNX Runtime GenAI**（.NET 可用 [Microsoft.ML.OnnxRuntime.GenAI](https://www.nuget.org/packages/Microsoft.ML.OnnxRuntime.GenAI)） |
| 量化 | INT4 默认；质量敏感用 INT8 |

---

## 推荐模型清单（可直接下载）

### Tier 1：轻量调研 / 边缘 CPU（优先试）

| 模型 | HuggingFace | 参数量 | 特点 | .NET 接入 |
|------|-------------|--------|------|-----------|
| **Qwen3-0.6B-ONNX** | [onnx-community/Qwen3-0.6B-ONNX](https://huggingface.co/onnx-community/Qwen3-0.6B-ONNX) | 0.6B | 中文对话、体积极小、Transformers.js 同款权重 | ORT GenAI / Transformers.js |
| **Qwen2.5-0.5B-Instruct** | [Qwen/Qwen2.5-0.5B-Instruct](https://huggingface.co/Qwen/Qwen2.5-0.5B-Instruct) | 0.5B | 需 Olive 转 ONNX；中文尚可 | Olive → ORT GenAI |
| **Phi-3-mini-4k-instruct-onnx** | [microsoft/Phi-3-mini-4k-instruct-onnx](https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-onnx) | 3.8B | 官方 ONNX 包，CPU INT4 开箱即用 | `hf download` + ORT GenAI |

### Tier 2：质量更好（调研机 8GB+ 内存）

| 模型 | HuggingFace | 参数量 | 特点 |
|------|-------------|--------|------|
| **Phi-3.5-mini-instruct-onnx** | [microsoft/Phi-3.5-mini-instruct-onnx](https://huggingface.co/microsoft/Phi-3.5-mini-instruct-onnx) | ~3.8B | 微软官方 ONNX，推理优化好 |
| **Phi-4-mini-instruct-onnx** | [microsoft/Phi-4-mini-instruct-onnx](https://huggingface.co/microsoft/Phi-4-mini-instruct-onnx) | ~3.8B | 新一代小模型，英文强；中文需实测 |
| **Qwen2.5-1.5B-Instruct-Olive-Onnx** | [khanuckaeff/Qwen2.5-1.5B-Instruct-Olive-Onnx](https://huggingface.co/khanuckaeff/Qwen2.5-1.5B-Instruct-Olive-Onnx) | 1.5B | Olive 已转好，中文指令跟随较好 |

### Tier 3：多模态（若要做手相/牌面图）

| 模型 | 说明 |
|------|------|
| **Qwen2.5-VL-3B-Instruct** | 可用 [obeaver](https://github.com/microsoft/obeaver) 转 VL ONNX INT4 |
| **Qwen3-VL-2B-Instruct** | 更轻 VL，适合调研 |

---

## .NET 接入路径

### 路径 A：ONNX Runtime GenAI（推荐后台）

```bash
# 示例：下载 Phi-3 mini INT4
hf download microsoft/Phi-3-mini-4k-instruct-onnx \
  --include "cpu_and_mobile/cpu-int4-rtn-block-32/*" \
  --local-dir ./models/phi3-mini-int4
```

```xml
<PackageReference Include="Microsoft.ML.OnnxRuntime.GenAI" Version="0.*" />
```

配合 [ElBruno.LocalLLMs](https://github.com/elbruno/ElBruno.LocalLLMs) 可参考 C# 调用模式。

### 路径 B：自转 ONNX（任意 HF 模型）

使用 [Microsoft Olive](https://github.com/microsoft/Olive)：

```bash
olive auto-opt --model_name Qwen/Qwen2.5-0.5B-Instruct --device cpu --precision int4
```

### 路径 C：浏览器内（H5 调研）

[Transformers.js](https://github.com/huggingface/transformers.js) + `onnx-community/Qwen3-0.6B-ONNX`

---

## 针对命理场景的用法建议

### Prompt 结构（避免模型算干支）

```text
[System] 你是命理解读助手。以下命盘由系统计算，请勿修改干支数据。
[User] 命盘 JSON: {...}  请从事业运角度解读，200字内。
```

### 塔罗 Layer2

```text
[System] 根据已选牌位与模板释义 synthesize，不要编造未出现的牌。
[User] positions: [{slot:"past", card:"Tower", reversed:false, snippet:"..."}, ...]
```

### 不建议

- 让 0.6B 模型直接「心算」四柱 — 幻觉率高
- 未量化 FP16 大模型上生产 CPU — 延迟不可接受

---

## 调研优先级（建议试跑顺序）

1. **Qwen3-0.6B-ONNX** — 最快验证中文解读管线
2. **Phi-3-mini-4k-instruct-onnx INT4** — 验证 ORT GenAI .NET 集成
3. **Qwen2.5-1.5B-Instruct-Olive-Onnx** — 质量与体积平衡点

---

## 工具链

| 工具 | 用途 |
|------|------|
| [obeaver](https://github.com/microsoft/obeaver) | 下载/转换/试跑 ONNX GenAI |
| [Olive](https://github.com/microsoft/Olive) | HF → ONNX 量化 |
| [Optimum](https://github.com/huggingface/optimum) | 导出 ONNX |

---

## Lab 后续任务

> 推理层完整设计见 [`inference-layer-design.md`](../../active/inference-layer-design.md)

- [x] `IChing.Lab.Inference` 项目：封装 ORT GenAI
- [x] `POST /lab/interpret`：命盘 JSON → 短解读
- [x] PromptTest 控制台 + fixtures
- [ ] 切换默认模型至 Qwen2.5-1.5B 并完成试跑
- [ ] `POST /lab/{domain}/read?tier=` 统一解读 API
- [ ] Layer1 规则引擎 + Tier 0 免费模板
