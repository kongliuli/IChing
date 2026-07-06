# 排盘算法调研摘要

本文只记录当前 .NET Lab 的算法选择和后续方向。

## 八字

- 当前使用 `lunar-csharp` 计算四柱、十神、纳音、节气、大运流年。
- 项目内补充了真太阳时、格局/破格、用神分析、流月/流日辅助信息。
- 后续若扩展流派差异，应通过 Layer1 规则插件输出摘要，不改排盘事实。

## 六爻

- 当前使用 `IChingLibrary.SixLines`。
- 铜钱法保留三枚铜钱概率分布：6/7/8/9 分别映射老阴、少阳、少阴、老阳。
- `IChingLibrary.SixLines` 已负责纳甲、世应、六亲、六神、伏神、神煞等排盘事实。
- 后续不同断法、用神取法、旺衰提示进入 `src/IChing.Lab.Core/Rules/Plugins/LiuyaoRulePlugins.cs`。

## 塔罗

- 当前使用内置 78 张牌、牌阵、Fisher-Yates 洗牌和可复现 `seed`。
- Layer1 只做牌位、正逆位、牌义和统计摘要。
- Layer2 才交给 ONNX/LLM 生成叙述。

## 原则

- 排盘 deterministic，解读 generative。
- 模型只叙述，不重新计算干支、卦象、牌名、正逆位。
- 规则扩展优先写成内置 RulePlugin，等运营确实需要热更新再考虑外部规则包。
