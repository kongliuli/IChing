# 插件生态调研（归档）

> **状态**：已归档（2026-07）
> **来源**：原 `docs/active/plugin-design.md` §2 / §9
> **现行实现说明**：[plugin-design.md](../../active/plugin-design.md)

本文件保留选型调研与外部参考链接，不再作为实现入口。

---

## 1. 命理算法库生态（可候选排盘插件）

### 1.1 已在用

| 库 | 语言 | 协议 | 覆盖 | 备注 |
|----|------|------|------|------|
| [lunar-csharp](https://github.com/6tail/lunar-csharp) (6tail) | C# | MIT | 八字 / 黄历 / 节气 / 纳音 / 十神 / 胎元命宫身宫 / 起运 | 多语言版本同步维护，0001–9999 年 |
| [IChingLibrary.SixLines](https://www.nuget.org/packages/IChingLibrary.SixLines/) 2.0.3 | C# (.NET 10) | — | 京房纳甲 / 世应 / 六亲 / 六神 / 16 神煞 / 卦属特性 | 已抽象 `ICastingMethod`，Builder 模式，本仓库已用 |

### 1.2 可作为替代 / 补充插件

| 库 | 语言 | 协议 | 价值 |
|----|------|------|------|
| [YiJingFramework](https://www.nuget.org/packages/YiJingFramework.Annotating) 5.0.1 | C# (.NET 8) | MIT | 易学注解仓库结构，`Annotating.Zhouyi` 子包提供《周易》《易传》注解读写 |
| [cnlunar](https://pypi.org/project/cnlunar/) 0.2.4 | Python | MIT | 基于《钦定协纪辨方书》，宜忌等第表更严谨；可作港式八字月柱对照（需互操作或重写） |
| [ZhouYiLab](https://github.com/banderzhm/ZhouYiLab) | C++23 Modules | — | 大六壬 / 六爻 / 紫微 / 八字 / 奇门遁甲；扩展新术种参考 |
| [ichingshifa](https://github.com/kentang2017/ichingshifa) | Python | — | 周易筮法 / 大衍之数 / 京房易 / 爻辭 |
| [l2yao/iching](https://github.com/l2yao/iching) | Python | — | 八字 + 风水 + 六爻，有 JS 版本 |
| [horosa](https://awesome.ecosyste.ms/projects/github.com%2Fhorace-maxwell) | Mac App | — | 紫微 / 八字 / 占星 / 六壬 / 遁甲 / 太乙 / 六爻 / 风水——产品形态参考 |

### 1.3 塔罗数据源

| 库 / 数据集 | 形态 | 维度 | 价值 |
|----|------|------|------|
| [tarot-card-meanings](https://www.npmjs.com/package/tarot-card-meanings) (Deckaura) | NPM + PyPI | 12 维牌义 | Tier 0 模板与小阿卡纳牌义库（见 [research-tarot-optimization.md](./research-tarot-optimization.md) Phase A） |
| [Deckaura 78 牌研究论文](https://cdn.shopify.com/s/files/1/0953/6195/8161/files/Tarot_Interpretation_Systems_Academic_Paper.pdf) | PDF | 元素分布统计 | 学术化牌义参考 |
| [Tarot MCP Server](https://lobehub.com/mcp/morax-tarot-mcp) (Morax) | Node.js / TS | 11 牌阵 + 自定义 | 牌阵扩展与元素平衡分析参考 |
| [RoxyAPI tarot](https://roxyapi.com/docs/tutorials/tarot-app) | 商业 API | daily / three-card / celtic-cross | 远程排盘插件形态参考 |

---

## 2. AI 推理引擎生态（可候选解读插件）

| 引擎 | NuGet | 模型格式 | 适合本项目的角色 |
|------|-------|----------|------------------|
| **Microsoft.ML.OnnxRuntimeGenAI**（已在用） | `Microsoft.ML.OnnxRuntimeGenAI` | ONNX + `genai_config.json` | **默认引擎**，Qwen2.5-1.5B |
| [LLamaSharp](https://github.com/SciSharp/LLamaSharp) | `LLamaSharp` + Backend | GGUF | **次选进程内引擎**（见 `samples/LLamaSharpEngine`） |
| [LM-Kit.NET](https://cloud.tencent.com.cn/developer/article/2690938) | 单 NuGet | GGUF / ONNX / LMK | 进阶 RAG / 多 Agent 时再评估 |
| **Ollama / LMStudio** | HTTP | GGUF | **本地 HTTP 引擎**（见 `samples/OpenAiCompatibleEngine`） |
| **OpenAI 兼容远程** | HTTP | 远程 | **远程 API 引擎**（见 `samples/OpenAiCompatibleEngine/OpenAiRemoteEngine.cs`） |

### 2.1 关键趋势（调研时点 2026 Q2）

1. Qwen3.5 时代 llama.cpp 参数支持通常优于 Ollama
2. 本地小模型（0.8B–9B）质量已够叙述层使用
3. GGUF 生态成熟，HuggingFace 可直接拉量化版
4. .NET ONNX GenAI 与本仓库默认路径一致

---

## 3. .NET 插件化机制（实现依据）

| 机制 | 状态 | 适用 |
|------|------|------|
| **`AssemblyLoadContext` + `AssemblyDependencyResolver`** | 官方推荐（[MS Learn](https://learn.microsoft.com/dotnet/core/tutorials/creating-app-with-plugin-support)） | 主选，支持 `isCollectible: true` |
| **MEF / MEF2** | [社区共识：不再推荐](https://www.devleader.ca/2026/04/09/plugin-loading-in-net-assemblyloadcontext-with-dependency-injection) | 不采用 |
| **`Assembly.LoadFrom`** | 旧式，依赖冲突高发 | 不采用 |
| **AppDomain** | .NET Core 后已不支持隔离加载 | 不采用 |

安全提示：不可信代码不能安全加载到 .NET 进程内；本项目插件均为自有/可信来源，ALC 足够。

---

## 4. 参考资料

### 命理算法库

- [lunar-csharp (6tail, GitHub)](https://github.com/6tail/lunar-csharp)
- [IChingLibrary.SixLines (NuGet)](https://www.nuget.org/packages/IChingLibrary.SixLines/)
- [YiJingFramework.Annotating (NuGet)](https://www.nuget.org/packages/YiJingFramework.Annotating)
- [cnlunar (PyPI)](https://pypi.org/project/cnlunar/)
- [ZhouYiLab (GitHub)](https://github.com/banderzhm/ZhouYiLab)
- [ichingshifa (GitHub)](https://github.com/kentang2017/ichingshifa)
- [tarot-card-meanings (NPM)](https://www.npmjs.com/package/tarot-card-meanings)
- [Tarot MCP Server (LobeHub)](https://lobehub.com/mcp/morax-tarot-mcp)
- [RoxyAPI tarot tutorial](https://roxyapi.com/docs/tutorials/tarot-app)

### AI 推理引擎

- [LLamaSharp (GitHub)](https://github.com/SciSharp/LLamaSharp)
- [Running Local AI with LlamaSharp in .NET](https://www.c-sharpcorner.com/article/running-local-ai-with-llamasharp-in-net-a-developers-guide/)
- [LM-Kit.NET](https://cloud.tencent.com.cn/developer/article/2690938)
- [llama.cpp vs Ollama: 70% Performance Divide](https://www.banandre.com/blog/llamacpp-vs-ollama-performance-divide-local-llm-runtimes)

### .NET 插件化

- [Create a .NET Core application with plugins (MS Learn)](https://learn.microsoft.com/dotnet/core/tutorials/creating-app-with-plugin-support)
- [How to use and debug assembly unloadability (MS Learn)](https://learn.microsoft.com/dotnet/standard/assembly/unloadability)
- [Plugin Loading in .NET: AssemblyLoadContext with DI](https://www.devleader.ca/2026/04/09/plugin-loading-in-net-assemblyloadcontext-with-dependency-injection)
