# 技术路线：.NET 后台（调研 Lab）

## 定位变更

| 项 | 决策 |
|----|------|
| **正式方向** | .NET 10 + ASP.NET Core（`src/IChing.Lab.*`） |
| **Java 单体** | 历史 Spike，仅作参考，**不作为成品** |
| **阶段目标** | 算法调研、接口原型、ONNX 接入验证 |

## 架构（调研期）

```
src/
├── IChing.Lab.Core/        # 八字、六爻、塔罗
├── IChing.Lab.Inference/   # ONNX GenAI 解读
├── IChing.Lab.Api/         # HTTP 探针
└── IChing.Lab.Tests/
```

SDK 锁定：`global.json` → .NET 10.0.301

## 算法依赖

| 模块 | 实现 |
|------|------|
| 八字 | lunar-csharp + 自研真太阳时 + `GetYun` 大运 |
| 六爻 | [IChingLibrary.SixLines](https://www.nuget.org/packages/IChingLibrary.SixLines) 纳甲/六亲/六神 |
| 塔罗 | 78 张牌 + Celtic Cross 等牌阵 |
| AI | Microsoft.ML.OnnxRuntimeGenAI + Qwen3-0.6B |

## Lab API

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/lab/bazi` | 四柱、`longitude` 真太阳时、`gender` 大运 |
| POST | `/lab/interpret` | 命盘 JSON → 短解读 |
| GET | `/lab/interpret/status` | ONNX 模型加载状态 |
| POST | `/lab/liuyao/coin` | 铜钱法 + 纳甲 |
| POST | `/lab/liuyao/time` | 时间卦 + 纳甲 |
| POST | `/lab/tarot/draw` | 抽牌（含 `celtic-cross`） |
| GET | `/lab/tarot/spreads` | 牌阵列表 |

## 本地运行

```bash
cd src/IChing.Lab.Api && dotnet run
bash scripts/download-qwen-model.sh   # 可选
```
