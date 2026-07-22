# PENDING — 进行中 / 待办主控

> 编写模型: Claude Fable 5 (Cursor Agent) · 2026-07-16；Cursor Grok 4.5 · 2026-07-16

接到任务先看本页。每次提交后，仍未完成且确认保留的事项统一登记在这里；已完成或明确暂不处理的事项不保留。对应 spec 全部落地并验证后整篇归档到 `archive/specs/` 并删除条目。

## 进行中

- [~] **塔罗前端壳演进**（P1–P3 代码已落地；人工验证 **延后**，不归档）→ [specs/tarot-shell-evolution/spec.md](active/specs/tarot-shell-evolution/spec.md)
  Qwen3.5 下载/接入说明另见 [qwen35-genai.md](active/qwen35-genai.md)（自建 GenAI 包仍待上游/本地导出）
- [~] **Byok 壳全流程接入**（代码已通；GUI 点验 **延后**，不归档）→ [specs/byok-full-flow/spec.md](active/specs/byok-full-flow/spec.md)

## 待启动

- [ ] 架构可视化 + API 参考：已产出 [architecture-diagrams.md](active/architecture-diagrams.md) 和 [api-reference.md](active/api-reference.md)；人工校对 **延后**
- [~] 易占域壳提取：`IChing.App.Shared` + Free/Byok/Biz/DevShell 已落地并可 Windows 构建；GUI 点验（闸门 H6）**延后**
- [ ] 端侧 Qwen3.5 GenAI 包：用 `scripts/build-qwen35-genai.ps1` 自建后接入下载器（见 [qwen35-genai.md](active/qwen35-genai.md)）（C1 探测未发现产物，blocked:export）
- [~] 商业版追问走 Lab：代码已接 `StreamLabFollowUpAsync` → `CommercialLabProvider.FollowUpAsync`（塔罗+易占 FollowUpChatPage）；GUI 点验（闸门 H5）**延后**

## 长期池（无 spec，来源见 roadmap）

产品计费、解读质量、ReadingExchange 远期等未排期事项见 [roadmap.md](active/roadmap.md)，暂不建 spec。
