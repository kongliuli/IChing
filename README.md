# IChing

命理平台 **调研 Lab** + 算法原型。正式后台技术栈为 **.NET 8**。

## 状态说明

| 目录 | 状态 |
|------|------|
| `src/IChing.Lab.*` | **当前**：.NET 调研后台 |
| `iching-*`（Java） | 历史 Spike，见 [docs/legacy-java-spike.md](docs/legacy-java-spike.md) |

## 文档

- [技术路线（.NET）](docs/tech-stack-dotnet.md)
- [排盘算法调研：八字/六爻/塔罗](docs/research-paipan-algorithms.md)
- [ONNX 模型调研](docs/onnx-models-survey.md)

## .NET Lab 快速开始

```bash
# 需 .NET 8 SDK
cd src/IChing.Lab.Api
dotnet run
```

### Lab API 示例

```bash
# 八字
curl -X POST http://localhost:5xxx/lab/bazi \
  -H 'Content-Type: application/json' \
  -d '{"year":1990,"month":5,"day":20,"hour":10}'

# 六爻铜钱法
curl -X POST 'http://localhost:5xxx/lab/liuyao/coin?seed=42'

# 塔罗三牌阵
curl -X POST http://localhost:5xxx/lab/tarot/draw \
  -H 'Content-Type: application/json' \
  -d '{"spreadId":"past-present-future","question":"事业","seed":7}'
```

## 构建

```bash
cd src && dotnet build
```
