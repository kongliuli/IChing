# Spec Execution Order 总览（已归档）

> **状态**：已全部完成（2026-07），存档参考。勿再按本目录开新实现任务。
> 本文档仅作历史顺序说明，详细 spec 见各子目录。

## 主线 6 个 Spec 的依赖与执行顺序

```
1. plugin-abstractions            (P1 抽象层 — 基础)
       ↓
2. refactor-inference-engine      (P2 拆分 Orchestrator + OnnxGenAiEngine)
       ↓                          ↘
3. wrap-chart-engines (P3)         4. externalize-prompt-templates (P4)
       ↓                          ↙
5. plugin-loader-and-di           (P5 加载器 + DI 集成)
       ↓
6. engine-plugins-three-modes     (P6 / P6.1 / P6.2 / P7 / P8 三模式引擎 + 降级链)
```

## 主线 Spec 一览

| # | Spec | 内容 | 状态 |
|---|------|------|------|
| 1 | [plugin-abstractions](./plugin-abstractions/spec.md) | 4 接口 + 共享 DTO + 版本校验 | 已完成 |
| 2 | [refactor-inference-engine](./refactor-inference-engine/spec.md) | OnnxGenAiEngine + Orchestrator + TemplateFallbackEngine | 已完成 |
| 3 | [wrap-chart-engines](./wrap-chart-engines/spec.md) | BaziEngine 等包装为 IChartEngine + DI | 已完成 |
| 4 | [externalize-prompt-templates](./externalize-prompt-templates/spec.md) | Scriban + prompts/*.txt + 热重载 | 已完成 |
| 5 | [plugin-loader-and-di](./plugin-loader-and-di/spec.md) | PluginLoadContext + PluginLoader + DI | 已完成 |
| 6 | [engine-plugins-three-modes](./engine-plugins-three-modes/spec.md) | LLamaSharp / Ollama / OpenAI + 降级链 | 已完成 |

## 后续补充 Spec（亦已完成）

| Spec | 内容 | 状态 |
|------|------|------|
| [expand-chart-algorithm-plugins](./expand-chart-algorithm-plugins/spec.md) | 排盘引擎扩展与 EngineMetadata | 已完成 |
| [algorithm-aware-prompt-templates](./algorithm-aware-prompt-templates/spec.md) | 按引擎元数据选 Prompt 模板 | 已完成 |
| [deepseek-engine-plugin](./deepseek-engine-plugin/spec.md) | DeepSeek 远程引擎插件 | 已完成 |
| [deprecate-desktop](./deprecate-desktop/spec.md) | 暂停 IChing.Desktop WPF shell | 已完成 |

## 完成判定（已满足）

- 三对象（排盘 / Prompt / 引擎）均可通过配置 + 拷贝 DLL 替换
- 三类 AI 调用模式（进程内 / 本地 HTTP / 远程 API）统一为 IInferenceEngine
- 降级链 A → B → C → 模板自动切换
- 健康检查端点可见所有引擎状态
