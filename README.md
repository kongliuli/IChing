# IChing

命理平台 **调研 Lab**（.NET 10）。正式技术栈为 .NET，Java 目录为历史 Spike。

## 快速开始

```bash
# 固定 .NET 10（见 global.json）
cd src/IChing.Lab.Api
dotnet run
```

### 下载 Qwen2.5-1.5B ONNX 模型（可选，用于解读层）

```bash
bash scripts/download-qwen-15b-model.sh ./models/qwen2.5-1.5b-genai
```

### 提示词 / 模型试跑（无需启动 API）

```bash
cd src
dotnet run --project IChing.Lab.PromptTest -- --dry-run
dotnet run --project IChing.Lab.PromptTest -- --model ../models/qwen2.5-1.5b-genai --fixture tarot-tier1-en
```

## Lab API

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/lab/bazi` | 八字四柱 + 格局用神/破格 + 流年流月（节气起止+流日）+ 小运 |
| POST | `/lab/bazi/interpret` | 排盘 + ONNX 解读一步完成 |
| POST | `/lab/bazi/hepan` | 双人合盘（纳音、用神互补） |
| GET | `/lab/bazi/cities` | 城市经度查找表 |
| POST | `/lab/interpret` | 命盘 JSON → ONNX 短解读 |
| GET | `/lab/interpret/status` | 模型是否已加载 |
| POST | `/lab/liuyao/coin` | 六爻铜钱法 + 纳甲六亲伏神神煞 + 变卦世应六亲对照 |
| POST | `/lab/liuyao/time` | 六爻时间卦 + 纳甲 |
| POST | `/lab/tarot/draw` | 塔罗抽牌（78 张 + Celtic Cross / Horseshoe） |
| POST | `/lab/tarot/interpret` | 抽牌 + Layer1 叙事摘要 |
| GET | `/lab/tarot/spreads` | 可用牌阵列表 |
| GET | `/lab/calendar/day` | 黄历日课（响应 `{ day, engine }`） |
| GET | `/health/chart-engines` | 排盘引擎探活（各 domain 最小 Calculate） |

### 八字示例

```bash
curl -X POST http://localhost:5xxx/lab/bazi \
  -H 'Content-Type: application/json' \
  -d '{"year":1990,"month":5,"day":20,"hour":10,"gender":1,"flowYear":2026,"flowMonth":1}'
```

### 合盘示例

```bash
curl -X POST http://localhost:5xxx/lab/bazi/hepan \
  -H 'Content-Type: application/json' \
  -d '{"personA":{"year":1990,"month":5,"day":20,"hour":10,"gender":1},"personB":{"year":1992,"month":8,"day":15,"hour":14,"gender":0}}'
```

### 解读流水线示例

```bash
curl -X POST http://localhost:5xxx/lab/bazi/interpret \
  -H 'Content-Type: application/json' \
  -d '{"year":1990,"month":5,"day":20,"hour":12,"gender":1,"focus":"事业"}'
```

### 黄历示例

```bash
curl "http://localhost:5xxx/lab/calendar/day?year=2026&month=1&day=1"
# => { "day": { ...HuangLiDay... }, "engine": { "paipan": "lunar-csharp-1.6.8" } }
```

## Sidecar 样板

```bash
# 启动 HTTP sidecar（5001 八字 + 5004 六爻，供桥接插件联调）
scripts/run-chart-sidecar.cmd
```

详见 [samples/sidecars/IChing.ChartSidecar/README.md](samples/sidecars/IChing.ChartSidecar/README.md)

## 星轨塔罗（MAUI 单机）

```bash
# Windows
scripts/run-tarot-app.cmd
# 或
cd src/IChing.Tarot.App
dotnet run -f net10.0-windows10.0.19041.0
```

- **抽牌**：本地 `IChing.Lab.Core` + Deckaura 牌义 enrich（无需联网）
- **解读**：设置页填入 API Key（DeepSeek / OpenAI 兼容），调用远程 chat/completions
- **插件**：与 Lab 共用 `samples/TarotEngines` 项目，供其他单机客户端引用


| 项目 | 说明 |
|------|------|
| `IChing.Lab.Core` | 八字/六爻/塔罗/黄历/合盘算法 |
| `IChing.Lab.Inference` | ONNX GenAI 解读 |
| `IChing.Lab.Api` | HTTP 探针 |
| `IChing.Desktop` | WPF 桌面端 (deprecated, paused) |

## 文档

- [技术路线](docs/tech-stack-dotnet.md)
- [算法调研](docs/research-paipan-algorithms.md)
- [ONNX 模型](docs/onnx-models-survey.md)
- [推理层产品设计（分层 / Layer1 / 部署 / 提示词测试）](docs/inference-layer-design.md)

## 测试

```bash
cd src && dotnet test
```
