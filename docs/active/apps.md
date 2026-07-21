# App 入口

> 编写模型: Claude Fable 5 (Cursor Agent) · 2026-07-16

## 易占 App

`src/IChing.App` 是八字和六爻的正式 MAUI App。

功能：

- 八字本地排盘
- 六爻铜钱/时间起卦
- 本地规则摘要
- AI 解读
- 解读后追问
- 历史记录
- HTML 风格解读展示与本地图片导出

运行：

```bat
scripts\run-iching-app.cmd
```

## 塔罗 App（共享库 + 版本 head）

塔罗 UI 全部在共享库 `src/IChing.Tarot.App.Shared`（Pages / Views / Services / Resources），四个 head 工程只保留 `MauiProgram`（注入 `EditionHost.Capabilities`）、平台入口和图标：

| Head | ApplicationId | 定位 |
|------|---------------|------|
| `src/IChing.Tarot.App` | `com.iching.tarot.dev` | 开发壳（DevShell：Lab / BYOK / 端侧 ONNX 全开） |
| `src/IChing.Tarot.Free` | `com.iching.tarot.free` | 免费版：抽牌 + 牌面 + 基本牌义，无 AI |
| `src/IChing.Tarot.Byok` | `com.iching.tarot.byok` | 自助版：用户自带 OpenAI 兼容 Key |
| `src/IChing.Tarot.Biz` | `com.iching.tarot` | 商业版：走自建 Lab 服务（Key 在服务端） |

功能（按版本裁剪）：多牌阵抽牌、AI 解读、追问、历史详情、探索页与人格测评、长图导出。版本能力矩阵见 [editions.md](editions.md)，后续演进见 [specs/tarot-shell-evolution/spec.md](specs/tarot-shell-evolution/spec.md)。

运行（开发壳）：

```bat
scripts\run-tarot-app.cmd
```

Android 调试：

```bat
scripts\run-tarot-app-android.cmd
```

版本 head 构建示例：

```bat
dotnet build src\IChing.Tarot.Free\IChing.Tarot.Free.csproj -f net10.0-windows10.0.19041.0
```

## 已移除入口

`src/IChing.Bazi.App` 和 `src/IChing.Liuyao.App` 已移除。八字与六爻统一进入 `src/IChing.App`。
