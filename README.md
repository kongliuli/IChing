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
| POST | `/lab/bazi` | 八字四柱 + 真太阳时 + 大运（需 `gender`） |
| POST | `/lab/interpret` | 命盘 JSON → ONNX 短解读 |
| GET | `/lab/interpret/status` | 模型是否已加载 |
| POST | `/lab/liuyao/coin` | 六爻铜钱法 + 纳甲六亲 |
| POST | `/lab/liuyao/time` | 六爻时间卦 + 纳甲 |
| POST | `/lab/tarot/draw` | 塔罗抽牌（78 张 + Celtic Cross） |
| GET | `/lab/tarot/spreads` | 可用牌阵列表 |

### 八字示例

```bash
curl -X POST http://localhost:5xxx/lab/bazi \
  -H 'Content-Type: application/json' \
  -d '{"year":1990,"month":5,"day":20,"hour":12,"longitude":121.47,"gender":1}'
```

### 解读示例

```bash
curl -X POST http://localhost:5xxx/lab/interpret \
  -H 'Content-Type: application/json' \
  -d '{"chart":{"yearPillar":"庚午"},"focus":"事业"}'
```

## 模块

| 项目 | 说明 |
|------|------|
| `IChing.Lab.Core` | 八字/六爻/塔罗算法 |
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
