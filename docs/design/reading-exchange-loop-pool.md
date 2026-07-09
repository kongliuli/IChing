# ReadingExchange Loop 池

动态 loop 自调度项；每项完成后勾选并 commit。

| ID | 任务 | 状态 |
|----|------|------|
| L1 | 追问结果 `ReadingResultProducer` HTML（`FollowUpReadingPresenter` + WebView） | done |
| L2 | 测评页 WebView 接 `QuizReadingProducerBridge` | done |
| L3 | Legacy `interpret` 端点迁移 + `Orchestrator.Interpret` 模板化 | done |
| L4 | Lab `/lab/chat` register 存 chart + App `ReadingSessionBridge` 同步 | done |
| L5 | `LabReadService` 填充真实 `ExchangeInput`（envelope 非空桩） | pending |
| L6 | Lab chat followup 与 App 双向 history 同步 | pending |
| L7 | `ExchangeInferenceRouter` 统一 Inference Scriban 路径 | pending |

## Loop 唤醒 prompt

```
ReadingExchange loop 池：检查 L5–L7 pending 项，选最小可验证切片实现 + 测试 + commit；全部 done 后仅 arm 长间隔心跳。
```
