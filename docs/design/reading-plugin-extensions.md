# RuleEngine Prompt 扩展规范

> 对应 ADR：[reading-exchange.md](./reading-exchange.md) §7.2  
> 实现：`IChing.Lab.Core/Rules/RulePromptExtensionRegistry.cs`

## 原则

1. **流派/插件不改 chart**，只追加 AI 上下文（`SystemDirectives`、`OutputSections`、`Constraints`）。
2. **开关**仍走 `RuleEngineOptions.Plugins[id].Enabled/Weight`（`App_Data/rule-engine-options.json` + API）。
3. **首期不做**独立 pack 目录；新流派 = 新 `RulePlugin` + Registry 条目 + 配置开关。

## PromptExtension 结构

```csharp
PromptExtension(
  SystemDirectives[],   // 追加到 packet system / plugin directives
  OutputSections[],     // 追加 output section key（domain 白名单校验）
  Constraints[])        // 写入 PluginPromptContext.Constraints
```

## 已注册插件扩展

| pluginId | OutputSections | 用途 |
|----------|----------------|------|
| `bazi.yongshen.current` | `yongshen` | 用神分析节 |
| `bazi.wuxing.balance` | `flow` | 五行平衡提示 |
| `bazi.flow.current` | `flow` | 大运/流年（仅当有 facts） |
| `bazi.school.ziping.geju` | `geju` | 子平格局（插件启用后） |
| `liuyao.interpretation.traditional` | `changing`, `shi_ying` | 传统六爻解读 |
| `liuyao.shensha.markers` | `overview` | 神煞提示 |

## 消费方

| 阶段 | 行为 |
|------|------|
| `RuleEngine.Run` | 产出 `RuleDigestItem` + `ActivePlugins` |
| `ReadingPromptPackets.*Initial` | `BuildTemplate` 合并 `RulePromptExtensionRegistry.Merge(ActivePlugins)` |
| `PluginPromptContext` | 注入 constraints + 额外 system directives |
| `ReadingOutputParser` | section key 白名单 + unknown 警告 |

## 配置示例

```json
"RuleEngine": {
  "MinWeight": 0,
  "Plugins": {
    "bazi.yongshen.current": { "Enabled": true, "Weight": 100 },
    "bazi.school.ziping.geju": { "Enabled": false, "Weight": 90 },
    "liuyao.interpretation.traditional": { "Enabled": true, "Weight": 50 }
  }
}
```

## 新增流派 checklist

1. 在 `Rules/Plugins/*RulePlugins.cs` 注册 `RulePlugin`。
2. 在 `RulePromptExtensionRegistry` 增加 `PromptExtension` 条目（key 对齐 section 白名单）。
3. 默认 `EnabledByDefault` / 文档更新。
4. 单元测试：`RulePromptExtensionRegistryTests` + packet 快照（可选）。
