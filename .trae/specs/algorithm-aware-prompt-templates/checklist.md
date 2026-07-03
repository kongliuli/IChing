- [x] `PromptContext` 新增 `EngineMetadata? Engine` 字段
- [x] `PromptContext` 新增 `IReadOnlyList<string> ModuleFocuses` 字段
- [x] 向下兼容：旧调用方不传新字段时行为不变
- [x] `ChartInterpretationOrchestrator` 从 `IChartEngine.EngineMetadata` 填充 `PromptContext.Engine`
- [x] `ChartInterpretationOrchestrator` 解析启用模块列表填充 `PromptContext.ModuleFocuses`
- [x] `PromptTemplateRegistry` 支持三级回退命名 `{domain}-tier{N}-{engineVariant}-{module}.txt` → `{domain}-tier{N}-{engineVariant}.txt` → `{domain}-tier{N}-default.txt`
- [x] `TemplatePromptBuilder.Build` 传入 `engineVariant` 与 `module` 选择模板
- [x] 模板内 Scriban 上下文注入 `engine_variant` / `module_focuses` 变量
- [x] 单测覆盖三级回退路径
- [x] `ChartInterpretationOrchestrator` 多模块（`ModuleFocuses.Count > 1`）时分别生成片段
- [x] 用 `{domain}-tier{N}-combined.txt` 组合模板拼装，`{{ module_snippets.{module} }}` 被片段替换
- [x] 组合模板缺失时回退到单模块模板
- [x] 单测：双模块组合生成含两个片段的 prompt

### 模板文件
- [x] `prompts/bazi-tier1-lunar-default.txt`
- [x] `prompts/bazi-tier1-cnlunar-default.txt`
- [x] `prompts/bazi-tier1-openfate-default.txt`
- [x] `prompts/bazi-tier1-lunar-yongshen.txt`
- [x] `prompts/bazi-tier1-lunar-geju.txt`
- [x] `prompts/bazi-tier1-combined.txt`
- [x] `prompts/liuyao-tier1-sixlines-najia.txt`
- [x] `prompts/liuyao-tier1-sixlines-shensha.txt`
- [x] `prompts/tarot-tier1-deckaura-default.txt`

### 验证
- [x] `dotnet build` 全绿
- [x] `dotnet test` 全绿（含三级回退、多模块组合测试）
- [x] `PromptTest --dry-run` 用 cnlunar 引擎产出 prompt 引用宜忌字段
- [x] 双模块组合 prompt 同时含格局与用神片段
- [x] 无 engineVariant 模板时回退到 default，dry-run diff 与改造前为空
- [x] 未引入 RAG（模板选择基于 metadata，不做向量检索）
