# Byok 壳全流程接入 Spec

> 编写模型: Cursor Grok 4.5 (Cursor Agent) · 2026-07-16
> 状态：代码已落地，待 GUI 人工点验
> 验证载体：自助壳 `IChing.Tarot.Byok` + 本机 Ollama `qwen3.5:9b`

## 1. 目标

打通自助版「抽牌 → 规则预览 → AI 解读 → 追问 → 历史」全链路，统一走 `InterpretationFacade` / Provider，用本机 Qwen 做内容验证。

## 2. 设计要点

| 项 | 做法 |
|----|------|
| 无 Key 本地端点 | `OpenAiEndpointHelpers.IsConfigured`：有 Key **或** BaseUrl 为本机；`OpenAiCompatibleChatClient` 空 Key 不加 `Authorization` |
| Ollama 预设 | `ProviderPresets.Ollama` → `http://localhost:11434/v1` / `qwen3.5:9b`；设置页可选 |
| 追问 | `FollowUpChatPage` → `InterpretationService.StreamFollowUpAsync` → Facade → Byok Provider；删除 `RemoteInterpretationService` |
| 历史 | `HistoryEntry.Interpretation` 可空；解读成功后 `UpdateLatestInterpretation`；详情页展示 |
| 商业追问 | **本轮不接**：`CommercialLabProvider.StreamFollowUpAsync` 仍为空流，见 PENDING |

## 3. 数据流

```
DrawPage
  → TarotEngine.Draw（本地）
  → InterpretationFacade.InterpretTarotAsync
      → Composite → ByokRemote → OpenAI 兼容（Ollama）
  → History.UpdateLatestInterpretation
FollowUpChatPage
  → InterpretationService.StreamFollowUpAsync
      → Composite.StreamFollowUpAsync → ByokRemote.StreamFollowUpAsync
```

## 4. 验证清单

- [x] 单元：本机 BaseUrl 无 Key 时 `IsConfigured==true`；远程无 Key 为 false（`OpenAiEndpointHelpersTests`）
- [x] CLI/脚本：`scripts/smoke-ollama-byok.ps1` 对本机 Ollama 发 chat/completions（空 Key + `think:false`）成功
- [x] 集成：`ByokOllamaIntegrationTests`（Category=Integration）TestAsync + 本机追问非流式路径通过
- [ ] Byok 壳 GUI：选 Ollama → 抽牌 → AI 解读 → 追问 → 历史含解读正文（需人工点验）
- [x] 商业版追问仍不走 Lab（已知缺口，PENDING 已记）

## 备注（Qwen3.5 + Ollama）

- 请求体会加 `think: false`；流式下模型仍可能把 token 耗在 `reasoning`，故**本机追问走非流式一次返回**。
- HttpClient 超时 5 分钟，适配本地大模型。

## 5. 不做

- 不接 Commercial Lab FollowUp UI
- 不把 PromptTest ONNX 路径改成 Ollama（另用轻量冒烟脚本）
- 不改 Free 版（无 AI）
