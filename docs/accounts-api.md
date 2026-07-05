# IChing 用户与额度 API（.NET）

轻量账户服务，迁移自 Java MVP 设计（[`mvp-backend.md`](mvp-backend.md)），用于 Tier 1+ 解读额度与 Mock 支付。

## 启动

```bash
cd src/IChing.Accounts.Api
dotnet run
```

默认监听 `http://localhost:5002`（或 launchSettings 配置端口）。

## 端点

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/health` | 存活探活 |
| POST | `/api/register` | 注册 `{ phone, password, nickname? }` |
| POST | `/api/login` | 登录，返回 JWT |
| GET | `/api/credits` | 查询 `interpretCredits`（需 Bearer） |
| POST | `/api/credits/consume` | 扣减额度 `{ amount, readingId? }`；同一 readingId 24h 内不重复扣 |
| POST | `/api/orders/mock-pay` | Mock 支付 `{ productType, amount }`；`membership` 赠 30 次，其他赠 10 次 |

## 与 Lab 集成（后续）

Lab API 可在 Tier 1+ 请求前调用 Accounts `/api/credits/consume`；Tier 0 不扣额度。

## 存储

当前为**进程内内存**实现（`AccountStore`），重启后数据丢失。生产环境可替换为 SQLite / MySQL，表结构参考 [`schema.sql`](schema.sql)。
