# IChing

命理平台 MVP 后台：用户、八字排盘、订单/会员、Admin API。

## 模块

- `iching-common` — 通用工具（JWT、AES、统一响应）
- `iching-paipan-core` — 排盘算法核心（MVP 占位实现）
- `iching-server` — Spring Boot 单体服务

## 快速开始

### 1. 启动依赖

```bash
docker compose up -d
```

### 2. 启动服务

```bash
mvn -pl iching-server -am spring-boot:run
```

Swagger UI: http://localhost:8080/swagger-ui.html

### 3. 默认管理员

- 手机号：`10000000000`
- 密码：`admin123`

## 里程碑验收

| 里程碑 | 接口 |
|--------|------|
| A | `POST /api/user/register`, `login`, `GET/PUT /profile` |
| B | `POST /api/bazi/calculate`, `GET /history`, `GET /{id}` |
| C | `POST /api/order/create`, `pay`, `GET /{orderNo}` |
| D | `GET /admin/users`, `orders`, `paipans`, `PUT /admin/users/{id}/member` |

## 文档

- [MVP 方案](docs/mvp-backend.md)
- [DDL](docs/schema.sql)
- [OpenAPI 草稿](docs/openapi.yaml)

## 测试

```bash
mvn test
```
