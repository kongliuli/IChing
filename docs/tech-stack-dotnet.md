# 技术路线：.NET 后台（调研 Lab）

## 定位变更

| 项 | 决策 |
|----|------|
| **正式方向** | .NET 8 + ASP.NET Core（`src/IChing.Lab.*`） |
| **Java 单体** | 历史 Spike，仅作参考，**不作为成品**（见仓库根目录 `iching-*`） |
| **阶段目标** | 算法调研、接口原型、ONNX 接入验证 |

## 推荐架构（调研期）

```
src/
├── IChing.Lab.Core/     # 纯算法：八字、六爻、塔罗
├── IChing.Lab.Api/      # 最小 HTTP 探针
└── IChing.Lab.Tests/    # 单测（概率、确定性）
```

**暂不引入**：微服务、完整用户/订单域、生产级鉴权。

## 算法依赖策略

| 模块 | 调研期方案 | 生产升级路径 |
|------|-----------|-------------|
| 八字 | [lunar-csharp](https://www.nuget.org/packages/lunar-csharp) | + 真太阳时库 / MingPan 逻辑移植 |
| 六爻 | 自研铜钱概率 + 时间卦 | 对接 [IChingLibrary](https://github.com/TheodoreCheung/IChingLibrary) 纳甲六亲 |
| 塔罗 | 自研牌阵 + seed 洗牌 | 78 张完整牌库 + 二层解读（模板 + LLM） |
| AI 解读 | ONNX Runtime GenAI | 见 `docs/onnx-models-survey.md` |

## 本地运行

```bash
export PATH="$HOME/.dotnet:$PATH"
cd src/IChing.Lab.Api
dotnet run
```

Swagger：`http://localhost:5xxx/swagger`（端口见控制台输出）

## Lab API

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/lab/bazi` | 八字四柱（lunar-csharp） |
| POST | `/lab/liuyao/coin` | 铜钱法起卦（可选 `seed`） |
| POST | `/lab/liuyao/time` | 时间卦（可选 `at`） |
| POST | `/lab/tarot/draw` | 塔罗牌阵（`spreadId`, `seed`） |
