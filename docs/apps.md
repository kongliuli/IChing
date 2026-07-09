# App 入口

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

## 塔罗 App

`src/IChing.Tarot.App` 是塔罗正式 MAUI App。

功能：

- 多牌阵抽牌
- AI 解读
- 解读后追问
- 历史详情
- 探索页、人格测评与趣味模块
- 长图导出

运行：

```bat
scripts\run-tarot-app.cmd
```

Android 调试：

```bat
scripts\run-tarot-app-android.cmd
```

## 已移除入口

`src/IChing.Bazi.App` 和 `src/IChing.Liuyao.App` 已移除。八字与六爻统一进入 `src/IChing.App`。
