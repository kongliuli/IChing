# IChing 用户与额度 API（.NET）

Lightweight .NET account service for Tier 1+ reading credits and mock payment flows.

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

## 与 Lab 集成

Lab.Api 在 Tier ≥ `Accounts:RequireForTierGte`（默认 1）时，于解读前调用 Accounts `POST /api/credits/consume`；Tier 0 不扣额度。

配置（`appsettings.json`）：

```json
"Accounts": {
  "Enabled": false,
  "BaseUrl": "http://localhost:5002",
  "RequireForTierGte": 1
}
```

MAUI App 向 Lab 请求时在 `Authorization: Bearer` 头携带登录 Token；未登录且 Lab 启用 Accounts 时返回 401/402。

## 存储

当前为**进程内内存**实现（`AccountStore`），重启后数据丢失。生产化时需重新设计表结构（SQLite / MySQL 等），勿沿用已删除的 Java MVP `schema.sql`。
