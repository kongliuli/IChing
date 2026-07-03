# Spec Execution Order 总览

> 本文档仅作顺序说明，详细 spec 见各子目录。

## 6 个 Spec 的依赖与执行顺序

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

## 顺序执行建议

| # | Spec | 内容 | 依赖 | 可并行 |
|---|------|------|------|--------|
| 1 | [plugin-abstractions](./plugin-abstractions/spec.md) | 4 接口 + 共享 DTO + 版本校验 | 无 | — |
| 2 | [refactor-inference-engine](./refactor-inference-engine/spec.md) | ChartInterpretationService 拆为 OnnxGenAiEngine + Orchestrator + TemplateFallbackEngine | 1 | — |
| 3 | [wrap-chart-engines](./wrap-chart-engines/spec.md) | BaziEngine 等包装为 IChartEngine + DI 注册 | 1 | 与 2 并行 |
| 4 | [externalize-prompt-templates](./externalize-prompt-templates/spec.md) | Scriban + prompts/*.txt + 热重载 | 1, 2 | 与 3 并行 |
| 5 | [plugin-loader-and-di](./plugin-loader-and-di/spec.md) | PluginLoadContext + PluginLoader + DI 集成 | 1, 2, 3, 4 | — |
| 6 | [engine-plugins-three-modes](./engine-plugins-three-modes/spec.md) | LLamaSharp / Ollama / OpenAI 三模式引擎 + 降级链编排 | 1, 2, 5 | — |

## 执行方式

每个 spec 严格遵循：
1. 读 spec.md / tasks.md / checklist.md
2. 用 Sub-Agent 实现 tasks.md 中的每个 Task
3. 完成后逐项对照 checklist.md 验证
4. 全部勾选后进入下一个 spec

## 完成判定

所有 6 个 spec 的 checklist 全部勾选完毕，即视为插件化方案落地完成。届时项目应满足：
- 三对象（排盘 / Prompt / 引擎）均可通过配置 + 拷贝 DLL 替换
- 三类 AI 调用模式（进程内 / 本地 HTTP / 远程 API）统一为 IInferenceEngine
- 降级链 A → B → C → 模板自动切换
- 健康检查端点可见所有引擎状态
