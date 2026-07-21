# Tasks

- [x] Task 1: 引入 Scriban 依赖
  - [x] SubTask 1.1: `IChing.Lab.Inference.csproj` 加 `<PackageReference Include="Scriban" Version="5.*" />`
  - [x] SubTask 1.2: 验证 `dotnet restore` 通过

- [x] Task 2: 提取现有 prompt 字符串到模板文件
  - [x] SubTask 2.1: 新建 `prompts/bazi-tier1-default.txt`（从 `BaziPromptBuilder` 迁移）
  - [x] SubTask 2.2: 新建 `prompts/liuyao-tier1-default.txt`（从 LiuyaoPromptBuilder 迁移）
  - [x] SubTask 2.3: 新建 `prompts/tarot-tier1-en.txt`（从 TarotPromptBuilder 迁移）
  - [x] SubTask 2.4: 新建 `prompts/tarot-translate-to-zh.txt`（迁移翻译 prompt）
  - [x] SubTask 2.5: 用 Scriban 语法替换占位符（`{{ focus }}` / `{{ chart_json }}` / `{{ rule_digest }}`）

- [x] Task 3: 实现 TemplatePromptBuilder
  - [x] SubTask 3.1: 新建 `Prompts/TemplatePromptBuilder.cs`，实现 `IPromptBuilder`
  - [x] SubTask 3.2: 构造注入 `PromptTemplateRegistry` + `(Domain, Tier, TemplateId)`
  - [x] SubTask 3.3: `Build` 方法用 Scriban 渲染，返回 `PromptBuildResult`（含 `NeedsTranslationPass`）

- [x] Task 4: 实现 PromptTemplateRegistry
  - [x] SubTask 4.1: 新建 `Prompts/PromptTemplateRegistry.cs`
  - [x] SubTask 4.2: 启动时扫描 `prompts/*.txt`，按文件名（`{domain}-tier{N}-{variant}`）建索引
  - [x] SubTask 4.3: `FileSystemWatcher` 监听变更，触发 `Reload(templateId)`
  - [x] SubTask 4.4: 加载失败时回退到内嵌默认模板（`EmbeddedPromptDefaults`）

- [x] Task 5: Orchestrator 切换到 IPromptBuilder
  - [x] SubTask 5.1: `ChartInterpretationOrchestrator` 注入 `IEnumerable<IPromptBuilder>`（按 templateId 选）
  - [x] SubTask 5.2: 删除对 `TarotPromptBuilder.BuildTranslateToChinese` / 静态 `BuildPrompt(fixture)` 的直接调用，改为 `IPromptBuilder.Build(PromptContext)`（LabController 的 liuyao/read、tarot/read 同步切换，PromptTest dry-run 切换）
  - [x] SubTask 5.3: 原 3 个 PromptBuilder 标 `[Obsolete]`

- [x] Task 6: DI 注册与配置
  - [x] SubTask 6.1: `Program.cs` 注册 `PromptTemplateRegistry`（单例）+ 4 个 `IPromptBuilder`
  - [x] SubTask 6.2: `appsettings.json` 加 `prompts:templateRoot` 配置项
  - [x] SubTask 6.3: 配置 `prompts/` 目录复制到输出（csproj `<None Include="..\..\prompts\**\*.txt" CopyToOutputDirectory="PreserveNewest" LinkBase="prompts" />`）

- [x] Task 7: 验证
  - [x] SubTask 7.1: `dotnet test` 全绿（29 passed）
  - [x] SubTask 7.2: dry-run fixture 输出与改造前一致（prompt 文本 diff 为空）
  - [x] SubTask 7.3: 手动改 `prompts/bazi-tier1-default.txt`，验证热重载（`HotReload_FileChanged_ReturnsNewContent` 测试通过 + FileSystemWatcher 触发）
  - [x] SubTask 7.4: 删除模板文件，验证降级到内嵌默认（`Fallback_*` 测试通过 + dry-run 缺失目录日志有 warning）

# Task Dependencies
- 依赖 [plugin-abstractions](../plugin-abstractions/spec.md) 完成
- 依赖 [refactor-inference-engine](../refactor-inference-engine/spec.md) 完成（Orchestrator 已存在）
- Task 1 → Task 2 → Task 3 / Task 4（并行）→ Task 5 → Task 6 → Task 7
