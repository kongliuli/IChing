# Prompt 资产说明

仓库根目录 `prompts/` 存放 Scriban 解读模板；本目录存放 PromptTest fixture 与试跑记录。

- 模板清单：[reading-template-inventory.md](../design/reading-template-inventory.md)
- 推理层总览：[inference-layer-design.md](../inference-layer-design.md)
- Fixture：`fixtures/*.json`；试跑输出：`runs/`（可选 gitignore）

## 模板 + 伴生元数据

| 文件 | 作用 |
|------|------|
| `prompts/{templateId}.txt` | Scriban 正文（注入变量后发给模型） |
| `prompts/{templateId}.meta.json` | 伴生元数据（**与 txt 同名前缀**；前缀即 `templateId`） |

`meta.json` 常见字段：

| 字段 | 说明 |
|------|------|
| `templateId` | 与文件名前缀一致 |
| `domain` / `tier` / `mode` | 域、档位、`initial` \| `translate` 等 |
| `language` | 如 `zh-CN` |
| `needsTranslationPass` | `true` 时走第二 pass（英译中）；默认塔罗为 `false` |
| `wordLimit` / `maxTokens` | 篇幅与生成上限 |
| `systemDirectives` | 系统级约束句 |
| `outputSections` | 固定 section 键与中文标题（如 `overview`/`advice`） |

**双源约定（塔罗 initial）**：`tarot-tier1-default.meta.json` 的 `outputSections` 与 `ReadingPromptTemplateManager.Get("tarot","initial")` 硬编码内容保持一致；两侧有注释互指。改一侧须同步另一侧。

注册与解析入口：`ReadingTemplateRegistry`（`IChing.Lab.Core/Readings/Templates/`）。

## 塔罗默认模板 `tarot-tier1-default`

| 项 | 值 |
|----|-----|
| 资产 | `prompts/tarot-tier1-default.txt` + `.meta.json` |
| 语言 | 中文直出（`needsTranslationPass=false`，单 pass） |
| 篇幅 | `wordLimit=400`，`maxTokens=800` |
| 固定 sections | `overview`（整体能量）、`advice`（行动建议）；牌位 section 由牌阵动态生成 |
| 正文结构 | 【牌阵】【牌面】【释义】【星座】(可空)【提问】【追问】(可空) + `reading-output.v2` JSON 规制 |

默认链路：`ReadingTemplateRegistry.ResolveTarot` / `TarotDemoService.ResolveTarotPrompt` 默认分支 → `tarot-tier1-default`。

兼容保留：`tarot-tier1-en`（英文 + `tarot-translate-to-zh` 第二 pass）仍注册，供 deckaura / celtic 等路径引用，**不再作为全局默认**。

## Birthday → 星座链路

可选生日进入模板星座段；不填则模板跳过【星座】，模型不得编造。

```text
HTTP TarotReadRequest.Birthday  (可空, yyyy-MM-dd)
    → ZodiacCalculator.FromBirthday  (src/IChing.Lab.Core/Tarot/ZodiacCalculator.cs)
    → 成功：注入 Scriban 变量 zodiac_block
    → 失败 / 未填：null，模板 {{ if zodiac_block }} 段不输出
```

相关模板变量（`tarot-tier1-default.txt`）：`spread_title`、`positions_block`、`rule_digest`、`zodiac_block`、`question`、`follow_up`、`word_limit`。
