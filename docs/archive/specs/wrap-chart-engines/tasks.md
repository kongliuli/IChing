# Tasks

- [x] Task 1: 创建 BaziChartEngine
  - [x] SubTask 1.1: 新建 `Engines/BaziChartEngine.cs`，实现 `IChartEngine`
  - [x] SubTask 1.2: Domain="bazi"，EngineId="lunar-csharp-1.6.8"
  - [x] SubTask 1.3: `Calculate` 委托给 `BaziEngine.Calculate`，从 `ChartRequest.Args` 反序列化 `BaziInput`

- [x] Task 2: 创建 LiuyaoChartEngine
  - [x] SubTask 2.1: 新建 `Engines/LiuyaoChartEngine.cs`，实现 `IChartEngine`
  - [x] SubTask 2.2: Domain="liuyao"，EngineId="iching-sixlines-2.0.3"
  - [x] SubTask 2.3: `Calculate` 支持 method=coin / time 两种方式

- [x] Task 3: 创建 TarotChartEngine
  - [x] SubTask 3.1: 新建 `Engines/TarotChartEngine.cs`，实现 `IChartEngine`
  - [x] SubTask 3.2: Domain="tarot"，EngineId="iching-tarot-built-in"
  - [x] SubTask 3.3: `Calculate` 委托给 `TarotEngine.Draw`

- [x] Task 4: 创建 CalendarEngine
  - [x] SubTask 4.1: 新建 `Engines/CalendarEngine.cs`，实现 `IChartEngine`
  - [x] SubTask 4.2: Domain="calendar"，EngineId="lunar-csharp-1.6.8"
  - [x] SubTask 4.3: `Calculate` 委托给 `HuangLiService.GetDay`

- [x] Task 5: DI 注册与示范端点
  - [x] SubTask 5.1: `Program.cs` 注册 4 个 `IChartEngine` 实现
  - [x] SubTask 5.2: `LabController` 新增 `GET /lab/engines` 返回所有注册引擎列表
  - [x] SubTask 5.3: 验证 `/lab/engines` 输出 4 条记录

- [x] Task 6: 验证向下兼容
  - [x] SubTask 6.1: `dotnet build` 通过
  - [x] SubTask 6.2: `dotnet test` 全绿，无回归
  - [x] SubTask 6.3: 原 `BaziEngine.Calculate` 等 static 调用未改动

# Task Dependencies
- 依赖 [plugin-abstractions](../plugin-abstractions/spec.md) 完成
- Task 1 / 2 / 3 / 4 可并行
- Task 5 依赖前 4 个完成
- Task 6 依赖 Task 5
