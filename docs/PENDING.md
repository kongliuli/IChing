# PENDING — 进行中 / 待办主控

> 编写模型: Claude Fable 5 (Cursor Agent) · 2026-07-16；Cursor Grok 4.5 · 2026-07-16

接到任务先看本页。每次提交后，仍未完成且确认保留的事项统一登记在这里；已完成或明确暂不处理的事项不保留。对应 spec 全部落地并验证后整篇归档到 `archive/specs/` 并删除条目。

## 进行中

- [~] **塔罗前端壳演进**（P1–P3 代码已落地，待人工验证）→ [specs/tarot-shell-evolution/spec.md](active/specs/tarot-shell-evolution/spec.md)
  Qwen3.5 下载/接入说明另见 [qwen35-genai.md](active/qwen35-genai.md)（自建 GenAI 包仍待上游/本地导出）
- [~] **Byok 壳全流程接入**（代码已通，待 GUI 点验）→ [specs/byok-full-flow/spec.md](active/specs/byok-full-flow/spec.md)

## 待启动

- [ ] 易占域壳提取：`IChing.App` 按塔罗模式抽 `IChing.App.Shared` + Free/Byok/Biz head（等塔罗壳演进 P1 验证后复制）
- [ ] 端侧 Qwen3.5 GenAI 包：用 `scripts/build-qwen35-genai.ps1` 自建后接入下载器（见 [qwen35-genai.md](active/qwen35-genai.md)）
- [ ] 商业版追问走 Lab：`FollowUpChatPage` / `CommercialLabProvider.StreamFollowUpAsync` 接 `LabApiClient.FollowUpAsync`（Byok 全流程已统一 Facade，本项专做 Commercial）

## 长期池（无 spec，来源见 roadmap）

产品计费、解读质量、ReadingExchange 远期等未排期事项见 [roadmap.md](active/roadmap.md)，暂不建 spec。
