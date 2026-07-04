# IChing.ChartSidecar

HTTP sidecar 样板，实现 [ChartBridge 协议](../../ChartBridge/README.md)，供 `ExternalHttpChartBridge` 桥接插件本地联调。

## 协议

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/health` | 探活，返回 200 |
| POST | `/bazi` | 请求体 `{ "args": { ... } }`，返回 `BaziChart` JSON |
| POST | `/liuyao` | 请求体 `{ "args": { "method", "at", "seed" } }`，返回 `LiuyaoNajiaResult` |
| POST | `/tarot` | 请求体 `{ "args": { "spreadId", "question", "seed" } }`，返回 Deckaura enrich 后的 `TarotReading` |

## 运行

```bash
# 最小联调：5001(bazi/openfate) + 5004(liuyao/npm)
dotnet run --project samples/sidecars/IChing.ChartSidecar -- --preset minimal

# 开发全套端口（5001-5009）
dotnet run --project samples/sidecars/IChing.ChartSidecar -- --preset dev

# 单端口全路由
dotnet run --project samples/sidecars/IChing.ChartSidecar -- --preset all
```

Windows 快捷脚本：`scripts/run-chart-sidecar.cmd`

## 与桥接引擎端口对照

| 端口 | 桥接 EngineId |
|------|----------------|
| 5001 | `bazi-openfate-bridge` |
| 5002 | `bazi-alvamind-bridge` |
| 5003 | `bazi-lunar-python-bridge` |
| 5004 | `liuyao-npm-bridge` |
| 5005 | `liuyao-ichingshifa-bridge` |
| 5006 | `liuyao-l2yao-bridge` |
| 5007 | `liuyao-zhouyilab-bridge` |
| 5008 | `tarot-arcanite-bridge` |
| 5009 | `tarot-ttarot-bridge` |

Sidecar 在多端口模式下各端口暴露相同路由；桥接只需命中对应端口的 domain 路径即可。
