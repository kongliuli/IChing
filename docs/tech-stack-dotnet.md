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
| 八字 | lunar-csharp + 真太阳时 + 大运 + 流年流月（十二节精确起止+流日）+ 格局/破格 + 小运 |
| 六爻 | [IChingLibrary.SixLines](https://www.nuget.org/packages/IChingLibrary.SixLines) 纳甲/六亲/六神/伏神/神煞/卦性 + 变卦对照表 |
| 塔罗 | 78 张牌 + 9 个基础牌阵 + Layer1 叙事 + `/read` 分层解读 |
| 合盘 | 日主生克 + 纳音 + 用神互补 + 五行/地支 |
| 黄历 | lunar-csharp 日宜忌/吉神凶煞 |
| AI | Microsoft.ML.OnnxRuntimeGenAI + **Qwen2.5-1.5B**（见 [inference-layer-design.md](./inference-layer-design.md)） |

## Lab API

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/lab/bazi` | 四柱、格局用神/破格、`flowYear` 流月含节气起止、`flowMonth` 含节内流日 |
| POST | `/lab/bazi/interpret` | 排盘 + ONNX 解读流水线 |
| POST | `/lab/bazi/hepan` | 双人合盘（纳音、用神互补） |
| GET | `/lab/bazi/cities` | 城市经度表 |
| POST | `/lab/interpret` | 命盘 JSON → 短解读 |
| GET | `/lab/interpret/status` | ONNX 模型加载状态 |
| POST | `/lab/liuyao/coin` | 铜钱法 + 纳甲/伏神/神煞 + 变卦 `comparison` 世应六亲对照 |
| POST | `/lab/liuyao/time` | 时间卦 + 纳甲/伏神/神煞 |
| POST | `/lab/tarot/draw` | 抽牌（`celtic-cross` / `horseshoe`） |
| POST | `/lab/tarot/interpret` | 抽牌 + Layer1 叙事 |
| GET | `/lab/tarot/spreads` | 牌阵列表 |
| GET | `/lab/calendar/day` | 黄历日课 |

## 本地运行

```bash
cd src/IChing.Lab.Api && dotnet run
bash scripts/download-qwen-model.sh   # 可选
```
