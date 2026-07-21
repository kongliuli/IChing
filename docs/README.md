# IChing 文档索引

链接清单只在本文件维护。`docs/active/` 为当前文档，`docs/archive/` 为已完成调研与历史记录。

## 工作入口

- [PENDING](PENDING.md)：进行中 / 待办主控，指向各 spec。
- [塔罗前端壳演进 Spec](active/specs/tarot-shell-evolution/spec.md)：三版本打磨 → 品牌 → 商业增长面。
- [Byok 壳全流程 Spec](active/specs/byok-full-flow/spec.md)：自助版抽牌→解读→追问→历史，Ollama 验证。

## Active

- [App 入口](active/apps.md)：八字/六爻 App、塔罗 App、运行脚本。
- [Lab API](active/lab-api.md)：HTTP 路由、Demo、规则插件管理、envelope v2。
- [架构说明](active/architecture.md)：项目分层、目录边界、正式/示例代码划分。
- [技术栈](active/tech-stack-dotnet.md)：.NET、MAUI、ASP.NET Core、推理与排盘依赖。
- [推理层设计](active/inference-layer-design.md)：Tier、模型、PromptTest（envelope 见 ReadingExchange）。
- [规则/插件设计](active/plugin-design.md)：已落地的插件接口、加载机制、推理引擎扩展。
- [ReadingExchange 设计](active/design/reading-exchange.md)：统一 AI 交互、envelope v2、Producer 分层。
- [Design docs](active/design/README.md)：ReadingExchange 周边设计与 UI 方向图。
- [Accounts API](active/accounts-api.md)：用户、额度、Mock 支付与 Lab 集成。
- [未实现路线图](active/roadmap.md)：仍待落地的产品与技术事项。
- [三版本产品线](active/editions.md)：免费 / 自助 / 商业版拆分。
- [Qwen3.5 GenAI 接入](active/qwen35-genai.md)：端侧模型下载与自建 GenAI 包。
- [iOS 发布](active/ios-release.md)：塔罗 App iOS / Mac Catalyst 打包发布。
- [Prompt fixtures](active/prompts/fixtures/)

## Archive

- [Archive 目录说明](archive/README.md)
- [已完成 Specs](archive/specs/README.md)：插件化与引擎相关 Trae specs（已全部完成）。
- [ONNX 模型调研](archive/research/onnx-models-survey.md)
- [排盘算法调研](archive/research/research-paipan-algorithms.md)
- [塔罗优化调研](archive/research/research-tarot-optimization.md)
- [插件生态调研](archive/research/plugin-ecosystem-survey.md)
- [ReadingExchange loop 池](archive/history/reading-exchange-loop-pool.md)
- [Java Spike 历史](archive/history/legacy-java-spike.md)
