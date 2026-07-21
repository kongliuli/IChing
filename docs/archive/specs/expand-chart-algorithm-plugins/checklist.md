- [x] `IChing.Lab.Abstractions` 新增 `EngineMetadata` 记录（Source/Version/AlgorithmBasis/TemplateHint/ModuleFocus）
- [x] `IChartEngine` 新增 `EngineMetadata Metadata` 只读属性
- [x] 4 个内置包装器（Bazi/Liuyao/Tarot/Calendar）填充 metadata
- [x] 排盘响应序列化含 `engine.metadata` 字段，向下兼容
- [x] `samples/ChartBridge/ChartBridge.csproj` 项目创建
- [x] `ExternalHttpChartBridge` 抽象基类实现（POST + 探活 + 不可达不抛异常）
- [x] `McpChartBridge` 抽象基类实现（MCP tools/call + initialize 探活）
- [x] sidecar 协议文档（README.md 含请求/响应 JSON schema）

### Bazi 域（目标 6）
- [x] `bazi-lunar-csharp`（内置，已有，补 metadata）
- [x] `bazi-cnlunar-port`（C# 移植，对照 cnlunar Python 验证月柱）
- [x] `bazi-openfate-bridge`（HTTP 桥接 @openfate/bazi-engine）
- [x] `bazi-alvamind-bridge`（HTTP 桥接 bazi-calculator-by-alvamind）
- [x] `bazi-lunar-python-bridge`（HTTP 桥接 lunar-python）
- [x] `bazi-mymcp-bridge`（MCP 桥接 @mymcp-fun/bazi）

### Liuyao 域（目标 6）
- [x] `liuyao-iching-sixlines`（内置，已有，补 metadata）
- [x] `liuyao-npm-bridge`（HTTP 桥接 liuyao 0.3.2）
- [x] `liuyao-ichingshifa-bridge`（HTTP 桥接 ichingshifa）
- [x] `liuyao-l2yao-bridge`（HTTP 桥接 l2yao/iching）
- [x] `liuyao-zhouyilab-bridge`（HTTP/进程桥接 ZhouYiLab C++23）
- [x] `liuyao-yijingframework-annot`（C# 直接引用 YiJingFramework.Annotating 5.0.1）

### Tarot 域（目标 6）
- [x] `tarot-builtin`（内置，已有，补 metadata）
- [x] `tarot-deckaura-data`（C# 数据插件，内嵌 78 牌 12 维 JSON）
- [x] `tarot-arcanite-bridge`（HTTP 桥接 arcanite 0.2.0，仅取牌阵/牌义）
- [x] `tarot-ttarot-bridge`（HTTP 桥接 ttarot 0.3.0）
- [x] `tarot-roxyapi-remote`（RoxyAPI 远程 API）
- [x] `tarot-morax-mcp-bridge`（MCP 桥接 Tarot MCP Server）

### Calendar 域（目标 6）
- [x] `calendar-huangli-builtin`（内置，已有，补 metadata）
- [x] `calendar-cnlunar-bridge`（HTTP 桥接 cnlunar）
- [x] `calendar-lunar-calendar-bridge`（HTTP 桥接 lunar-calendar VSOP87/LEA-406）
- [x] `calendar-koyomi-remote`（国立天文台 koyomi 远程 API）
- [x] `calendar-lunar-python-bridge`（HTTP 桥接 lunar-python）
- [x] `calendar-finddays-remote`（find-days 节气表远程 API）

### 配置与验证
- [x] `appsettings.json` 含 `plugins:chartEngines` 数组（每域默认 + 备用）
- [x] `GET /lab/engines` 每域返回 ≥ 5 条记录
- [x] 桥接 sidecar 离线时 `IsReady=false` 不阻断其他引擎
- [x] 切换 `chartEngines[domain=bazi].default` 后 `/lab/bazi` 响应 `engine.paipan` 相应变化
- [x] `dotnet build` 全绿（主程序 + 所有新 samples）
- [x] `dotnet test` 全绿（含各域桥接 mock 测试）
- [x] 未引入 RAG（桥接仅取排盘计算结果，不接 LLM/RAG 层）
