# IChing

命理平台 **调研 Lab**（.NET 10）。正式技术栈为 .NET，Java 目录为历史 Spike。

## 快速开始

```bash
# 固定 .NET 10（见 global.json）
cd src/IChing.Lab.Api
dotnet run
```

### 下载 Qwen3 ONNX 模型（可选，用于 /lab/interpret）

```bash
bash scripts/download-qwen-model.sh ./models/qwen3-0.6b-genai
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
| GET | `/lab/calendar/day` | 黄历日课（宜忌/吉神凶煞） |

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

## 模块

| 项目 | 说明 |
|------|------|
| `IChing.Lab.Core` | 八字/六爻/塔罗/黄历/合盘算法 |
| `IChing.Lab.Inference` | ONNX GenAI 解读 |
| `IChing.Lab.Api` | HTTP 探针 |

## 文档

- [技术路线](docs/tech-stack-dotnet.md)
- [算法调研](docs/research-paipan-algorithms.md)
- [ONNX 模型](docs/onnx-models-survey.md)

## 测试

```bash
cd src && dotnet test
```
