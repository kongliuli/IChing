# Tasks

## 阶段一：桥接基础设施与 EngineMetadata

- [x] Task 1: 扩展 IChartEngine 与 EngineMetadata 契约
  - [x] SubTask 1.1: 在 `IChing.Lab.Abstractions` 新增 `EngineMetadata` 记录（`Source / Version / AlgorithmBasis / TemplateHint / ModuleFocus`）
  - [x] SubTask 1.2: `IChartEngine` 新增 `EngineMetadata Metadata { get; }` 只读属性
  - [x] SubTask 1.3: 4 个内置包装器（BaziChartEngine/LiuyaoChartEngine/TarotChartEngine/CalendarEngine）填充各自 metadata
  - [x] SubTask 1.4: 排盘响应序列化含 `engine.metadata` 字段，向下兼容

- [x] Task 2: 实现 HTTP 桥接基类
  - [x] SubTask 2.1: 新建 `samples/ChartBridge/ChartBridge.csproj`（引用 Abstractions）
  - [x] SubTask 2.2: 实现 `ExternalHttpChartBridge` 抽象基类：POST `ChartRequest.Args` JSON 到子类提供的 `SidecarUrl`，解析响应
  - [x] SubTask 2.3: `IsReady` 通过 `GET /health` 探活 sidecar，不可达返回 false 不抛异常
  - [x] SubTask 2.4: 提供 sidecar 协议文档（请求/响应 JSON schema）放 `samples/ChartBridge/README.md`

- [x] Task 3: 实现 MCP 桥接基类
  - [x] SubTask 3.1: 在 `samples/ChartBridge` 实现 `McpChartBridge` 抽象基类：通过 stdio/HTTP 调用 MCP server 的 `tools/call`
  - [x] SubTask 3.2: `IsReady` 探活 MCP server `initialize`

## 阶段二：Bazi 域扩展至 6 引擎

- [x] Task 4: 实现 bazi-cnlunar-port（C# 移植）
  - [x] SubTask 4.1: 新建 `samples/BaziCnlunarEngine/`，移植 cnlunar 月柱/宜忌等第核心算法到 C#（参考 [cnlunar](https://pypi.org/project/cnlunar/) 0.2.4）
  - [x] SubTask 4.2: 实现 `IChartEngine`，EngineId="bazi-cnlunar-port"，TemplateHint="cnlunar"
  - [x] SubTask 4.3: 单测：对照 cnlunar Python 输出验证 2026 立春月柱等关键节点

- [x] Task 5: 实现 bazi HTTP/MCP 桥接引擎
  - [x] SubTask 5.1: `bazi-openfate-bridge`（包装 [@openfate/bazi-engine](https://www.npmjs.com/package/@openfate/bazi-engine)）
  - [x] SubTask 5.2: `bazi-alvamind-bridge`（包装 [bazi-calculator-by-alvamind](https://www.npmjs.com/package/bazi-calculator-by-alvamind)）
  - [x] SubTask 5.3: `bazi-lunar-python-bridge`（包装 [lunar-python](https://github.com/6tail/lunar-python)）
  - [x] SubTask 5.4: `bazi-mymcp-bridge`（包装 [@mymcp-fun/bazi](https://lobehub.com/mcp/mymcp-fun-bazi) MCP）

## 阶段三：Liuyao 域扩展至 6 引擎

- [x] Task 6: 实现 liuyao 桥接引擎
  - [x] SubTask 6.1: `liuyao-npm-bridge`（包装 [liuyao](https://www.npmjs.com/package/liuyao) 0.3.2）
  - [x] SubTask 6.2: `liuyao-ichingshifa-bridge`（包装 [ichingshifa](https://github.com/kentang2017/ichingshifa)）
  - [x] SubTask 6.3: `liuyao-l2yao-bridge`（包装 [l2yao/iching](https://github.com/l2yao/iching)）
  - [x] SubTask 6.4: `liuyao-zhouyilab-bridge`（包装 [ZhouYiLab](https://github.com/banderzhm/ZhouYiLab) C++23）

- [x] Task 7: 实现 liuyao-yijingframework-annot（C# 直接引用）
  - [x] SubTask 7.1: 引用 [YiJingFramework.Annotating](https://www.nuget.org/packages/YiJingFramework.Annotating) 5.0.1
  - [x] SubTask 7.2: 实现 `IChartEngine`，提供《周易》《易传》爻辞注解作为 liuyao 第 6 引擎

## 阶段四：Tarot 域扩展至 6 引擎

- [x] Task 8: 实现 tarot-deckaura-data（C# 数据插件）
  - [x] SubTask 8.1: 内嵌 [tarot-card-meanings](https://www.npmjs.com/package/tarot-card-meanings) Deckaura 78 牌 12 维 JSON 数据
  - [x] SubTask 8.2: 实现 `IChartEngine` 提供 Tier 0 牌义库查询

- [x] Task 9: 实现 tarot 桥接/远程引擎
  - [x] SubTask 9.1: `tarot-arcanite-bridge`（包装 [arcanite](https://pypi.org/project/arcanite/) 0.2.0，仅取牌阵/牌义）
  - [x] SubTask 9.2: `tarot-ttarot-bridge`（包装 [ttarot](https://www.npmjs.com/package/ttarot) 0.3.0）
  - [x] SubTask 9.3: `tarot-roxyapi-remote`（[RoxyAPI tarot](https://roxyapi.com/docs/tutorials/tarot-app) 远程 API）
  - [x] SubTask 9.4: `tarot-morax-mcp-bridge`（[Tarot MCP Server](https://lobehub.com/mcp/morax-tarot-mcp) MCP 桥接）

## 阶段五：Calendar 域扩展至 6 引擎

- [x] Task 10: 实现 calendar 桥接/远程引擎
  - [x] SubTask 10.1: `calendar-cnlunar-bridge`（包装 [cnlunar](https://pypi.org/project/cnlunar/) 宜忌等第）
  - [x] SubTask 10.2: `calendar-lunar-calendar-bridge`（包装 [lunar-calendar](https://gitcode.com/gh_mirrors/lu/lunar-calendar) VSOP87/LEA-406）
  - [x] SubTask 10.3: `calendar-koyomi-remote`（[国立天文台 koyomi](https://eco2.mtk.nao.ac.jp/cgi-bin/koyomi/cande/phenomena_sy.cgi)）
  - [x] SubTask 10.4: `calendar-lunar-python-bridge`（包装 [lunar-python](https://github.com/6tail/lunar-python)）
  - [x] SubTask 10.5: `calendar-finddays-remote`（[find-days](https://find-days.com/solar-terms/) 节气表）

## 阶段六：配置、发现与验证

- [ ] Task 11: 配置与发现
  - [ ] SubTask 11.1: `appsettings.json` 新增 `plugins:chartEngines` 数组，声明每域默认 + 备用引擎
  - [ ] SubTask 11.2: `GET /lab/engines` 返回所有引擎含 `engineId / domain / source / algorithmBasis`
  - [ ] SubTask 11.3: 桥接 sidecar 离线时引擎 `IsReady=false` 不阻断其他引擎

- [ ] Task 12: 验证
  - [ ] SubTask 12.1: `dotnet build` 全绿（主程序 + 所有新 samples 项目）
  - [ ] SubTask 12.2: `dotnet test` 全绿（含各域桥接 mock 测试）
  - [ ] SubTask 12.3: `/lab/engines` 每域返回 ≥ 5 条记录
  - [ ] SubTask 12.4: 切换 `chartEngines[domain=bazi].default` 到 `bazi-cnlunar-port`，`/lab/bazi` 响应 `engine.paipan="bazi-cnlunar-port"`

# Task Dependencies
- 依赖 [plugin-abstractions](../plugin-abstractions/spec.md) / [wrap-chart-engines](../wrap-chart-engines/spec.md) / [plugin-loader-and-di](../plugin-loader-and-di/spec.md) 已完成
- Task 1 → Task 2/3（桥接基类）→ 各域 Task 4-10（可并行）→ Task 11 → Task 12
- 阶段二/三/四/五（Task 4-10）相互独立，可并行实现
