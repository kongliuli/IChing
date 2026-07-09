# Reading 结果产出器 Catalog

> 对应 ADR：[reading-exchange.md](./reading-exchange.md) §6  
> 接口：`IChing.Lab.Core/Readings/Producers/IReadingResultProducer.cs`

## 核心三域

| ProducerId | CanProduce | ChartRef | Widgets（全自动） | Presenter |
|------------|------------|----------|-------------------|-----------|
| `core.bazi` | domain=bazi, mode≠entertainment | `BaziChart` | `pillarGrid`, `daYunTimeline` | `ReadingViewPresenter` |
| `core.liuyao` | domain=liuyao | `LiuyaoNajiaResult` | `hexagramLines`, `shiYingBadge` | `ReadingViewPresenter` |
| `core.tarot` | domain=tarot | `TarotReading` | `spreadTable` | `ReadingHtmlFormatter` + `ReadingViewPresenter` |

## Tier0（无 AI）

| ProducerId | 输入 | 说明 |
|------------|------|------|
| `core.bazi.tier0` | RuleDigest | 沿用 `ReadingSummaries.BuildBaziTier0Preview` |
| `core.liuyao.tier0` | RuleDigest | 沿用 `ReadingSummaries.BuildLiuyaoTier0Preview` |
| `core.tarot.tier0` | RuleDigest | 沿用 `ReadingSummaries.BuildTarotTier0Preview` |

## 娱乐测评（无 AI、无扣费）

| ProducerId | Mode | 输入 | Widgets |
|------------|------|------|---------|
| `entertainment.quiz` | entertainment | `QuizProducerInput` | `dimensionBars`, `typeCard` |
| `entertainment.quiz.mbti` | entertainment | scoring=mbti16 | 同上 |
| `entertainment.quiz.enneagram` | entertainment | scoring=enneagram9 | 同上 |
| `entertainment.quiz.holland` | entertainment | scoring=holland | 同上 |

桥接：`IChing.Tarot.App/Services/QuizReadingProducerBridge.cs` → `ReadingViewPresenter.ToDocument`

## ViewModel 形状

```csharp
ReadingViewModel(
  ProducerId, Domain, Title, Subject, Summary,
  Sections[],   // 来自 AI structured output
  Widgets[],    // Producer 规则表，AI 不可写
  Theme)
```

## 调用链

```
ExchangeOutput.Structured
  → ReadingResultProducerRegistry.Produce(exchange, chartRef)
  → ReadingViewModel
  → ReadingViewPresenter.ToDocument(vm, chartRef)
  → MAUI WebView / 分享 HTML
```

App 八字/六爻页：`HtmlReadingTemplate.Build*` 内部走 Producer + Presenter。
