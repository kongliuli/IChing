# Tasks

## 阶段一：契约扩展

- [x] Task 1: PromptContext 扩展 EngineMetadata 与 ModuleFocuses
  - [x] SubTask 1.1: `PromptContext` 新增 `EngineMetadata? Engine` 字段
  - [x] SubTask 1.2: `PromptContext` 新增 `IReadOnlyList<string> ModuleFocuses` 字段
  - [x] SubTask 1.3: 向下兼容：旧调用方不传新字段时默认 null/空，行为不变

- [x] Task 2: Orchestrator 注入引擎 metadata
  - [x] SubTask 2.1: `ChartInterpretationOrchestrator` 在构造 `PromptContext` 时，从 `IChartEngine.EngineMetadata` 填充 `Engine` 字段
  - [x] SubTask 2.2: 从排盘请求/引擎配置解析启用的模块列表，填充 `ModuleFocuses`

## 阶段二：模板选择规则扩展

- [x] Task 3: PromptTemplateRegistry 三级回退命名
  - [x] SubTask 3.1: 模板查找支持 `{domain}-tier{N}-{engineVariant}-{module}.txt`
  - [x] SubTask 3.2: 二级回退 `{domain}-tier{N}-{engineVariant}.txt`
  - [x] SubTask 3.3: 三级回退 `{domain}-tier{N}-default.txt`（向下兼容）
  - [x] SubTask 3.4: 单测覆盖三级回退路径

- [x] Task 4: IPromptBuilder 使用新选择规则
  - [x] SubTask 4.1: `TemplatePromptBuilder.Build` 调用 registry 时传入 `engineVariant`（取自 `ctx.Engine.TemplateHint`）与 `module`（取自 `ctx.ModuleFocuses` 首个有模板的）
  - [x] SubTask 4.2: 模板内 Scriban 上下文注入 `engine_variant` / `module_focuses` 变量

## 阶段三：多模块组合 Prompt

- [x] Task 5: 实现多模块组合拼装
  - [x] SubTask 5.1: `ChartInterpretationOrchestrator` 当 `ModuleFocuses.Count > 1` 时，分别用各模块模板生成片段
  - [x] SubTask 5.2: 用 `{domain}-tier{N}-combined.txt` 组合模板拼装，`{{ module_snippets.{module} }}` 占位被片段替换
  - [x] SubTask 5.3: 组合模板缺失时回退到单模块模板（取首个模块）
  - [x] SubTask 5.4: 单测：双模块组合生成含两个片段的 prompt

## 阶段四：新增模板文件

- [x] Task 6: 编写引擎/模块特定模板
  - [x] SubTask 6.1: `prompts/bazi-tier1-lunar-default.txt`（lunar-csharp 默认四柱+十神）
  - [x] SubTask 6.2: `prompts/bazi-tier1-cnlunar-default.txt`（cnlunar 宜忌等第）
  - [x] SubTask 6.3: `prompts/bazi-tier1-openfate-default.txt`（openfate 真太阳时/大运交互）
  - [x] SubTask 6.4: `prompts/bazi-tier1-lunar-yongshen.txt`（用神模块）
  - [x] SubTask 6.5: `prompts/bazi-tier1-lunar-geju.txt`（格局模块）
  - [x] SubTask 6.6: `prompts/bazi-tier1-combined.txt`（多模块组合拼装）
  - [x] SubTask 6.7: `prompts/liuyao-tier1-sixlines-najia.txt` + `liuyao-tier1-sixlines-shensha.txt`
  - [x] SubTask 6.8: `prompts/tarot-tier1-deckaura-default.txt`（Deckaura 12 维牌义）

## 阶段五：验证

- [x] Task 7: 验证
  - [x] SubTask 7.1: `dotnet build` 全绿
  - [x] SubTask 7.2: `dotnet test` 全绿（含三级回退、多模块组合测试）
  - [x] SubTask 7.3: `PromptTest --dry-run` 用 cnlunar 引擎产出 prompt 引用宜忌字段
  - [x] SubTask 7.4: 双模块组合 prompt 同时含格局与用神片段
  - [x] SubTask 7.5: 无 engineVariant 模板时回退到 default，dry-run diff 与改造前为空

# Task Dependencies
- 依赖 [expand-chart-algorithm-plugins](../expand-chart-algorithm-plugins/spec.md) 完成（提供 `EngineMetadata`）
- 依赖 [externalize-prompt-templates](../externalize-prompt-templates/spec.md) 已完成（提供 Scriban + 热重载）
- Task 1 → Task 2 → Task 3/4（可并行）→ Task 5 → Task 6 → Task 7
