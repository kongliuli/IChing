# IChing MVP 后台方案

## 范围确认（已锁定）

| 决策项 | 选择 | 说明 |
|--------|------|------|
| 后台范围 | 核心业务 API + 最小 Admin API | 同仓库，路由 `/api/**` 与 `/admin/**` 分离 |
| 排盘范围 | 仅八字 | 合盘放入 Phase 1.5 |
| 支付 | Mock 支付 | 接口抽象 `PaymentGateway`，便于后续接微信沙箱 |
| 裂变/社区 | 不实现 | `t_coupon` 仅建表预留 |

## 技术栈确认（已锁定）

| 项 | 选型 |
|----|------|
| 语言/框架 | Java 21 + Spring Boot 3.3 |
| 架构 | 模块化单体（common / paipan-core / server） |
| 数据库 | MySQL 8（本地 docker-compose） |
| 缓存 | Redis（可选，MVP 排盘缓存可后开） |
| 认证 | JWT + BCrypt |
| 迁移 | Flyway |
| API 文档 | springdoc-openapi（Swagger UI） |

## 排盘算法策略

- **首期**：`iching-paipan-core` 占位实现 + 固定用例单测
- **接口**：`BaziCalculator.calculate(BirthInfo) -> BaziResult`
- **升级路径**：替换 core 模块实现，HTTP/DB 层不变

## 里程碑

| 里程碑 | 内容 | 验收 |
|--------|------|------|
| A | 用户注册登录、出生信息、DDL | Swagger 可调 register/login/profile |
| B | 八字排盘 API、历史记录 | POST calculate + GET history |
| C | 订单 Mock 支付、解锁报告/会员 | 支付后 is_paid / membership 生效 |
| D | Admin 查询、手动改会员 | /admin 接口需 ADMIN 角色 |

## 模块结构

```
iching/
├── iching-common/       # 响应体、异常、JWT、AES
├── iching-paipan-core/  # 八字算法（无 Web 依赖）
├── iching-server/       # Spring Boot 入口
└── docs/                # 本文档、DDL、OpenAPI
```
