# Lab API

运行：

```bat
scripts\run-lab-api.cmd
```

默认入口：

- `/demo`：三域 Web Demo
- `/rules/plugins`：Layer1 规则插件管理页
- `/swagger`：接口文档

## 主要路由

| Method | Route | 用途 |
| --- | --- | --- |
| POST | `/lab/bazi` | 八字排盘 |
| POST | `/lab/bazi/read?tier=0` | 八字规则摘要 |
| POST | `/lab/bazi/interpret` | 八字排盘 + 解读 |
| POST | `/lab/bazi/hepan` | 双人合盘 |
| GET | `/lab/bazi/cities` | 城市经度 |
| POST | `/lab/liuyao/coin` | 六爻铜钱起卦 |
| POST | `/lab/liuyao/time` | 六爻时间起卦 |
| POST | `/lab/liuyao/read?tier=0` | 六爻规则摘要 |
| POST | `/lab/tarot/draw` | 塔罗抽牌 |
| POST | `/lab/tarot/read?tier=0` | 塔罗规则摘要 |
| POST | `/lab/tarot/interpret` | 塔罗抽牌 + 解读 |
| GET | `/lab/tarot/spreads` | 牌阵列表 |
| GET | `/lab/calendar/day` | 日历日课 |
| GET | `/lab/engines` | 排盘引擎列表 |
| GET | `/lab/rules/plugins` | 规则插件状态 |
| PUT | `/lab/rules/plugins/{id}` | 启停或调权重 |
| GET | `/health/chart-engines` | 排盘引擎健康状态 |
| GET | `/health/engines` | 推理引擎健康状态 |

## 规则插件配置

规则插件运行时修改会保存到：

```text
src/IChing.Lab.Api/App_Data/rule-engine-options.json
```

服务重启后会优先读取该文件；没有文件时使用 `appsettings.json`。
