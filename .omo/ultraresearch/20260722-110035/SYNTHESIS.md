# Ultraresearch Synthesis: IChing Project Architecture Assessment

**Date:** 2026-07-22
**Workers:** 8 planned (cancelled due to model failure — research completed directly by orchestrator)
**Sources:** 30+ web pages, full codebase scan (~25 projects, 100+ files examined)

---

## Executive Summary

The IChing project is a .NET 10 MAUI-based divination platform (Bazi, Liuyao, Tarot, Calendar) with AI inference via ONNX Runtime GenAI. The codebase demonstrates solid engineering fundamentals — clean project separation, plugin architecture, proper async patterns, and well-implemented PBKDF2 password hashing — but suffers from significant technical debt and missed modernization opportunities.

**Key findings:**
- **Tech stack is current but inconsistent:** .NET 10 SDK ✓, but package versions vary wildly (Logging.Abstractions v9 pinned vs v10.* elsewhere, xunit v2.5.3 while v3 stable exists)
- **No build-quality infrastructure:** Missing `.editorconfig`, Roslyn analyzers, Central Package Management, and code style enforcement
- **Architecture is functional but dated:** Manual DI registration (no Scrutor), god services (LabReadService), no functional error handling
- **AI integration is custom but not following Microsoft's new standard:** Should adopt `Microsoft.Extensions.AI` (GA in .NET 10, v10.7.0 on NuGet) instead of direct OnnxRuntimeGenAI dependency
- **Security has both strengths and critical gaps:** PBKDF2+ salt is excellent, but Accounts API is entirely in-memory (no persistence), JWT ValidateIssuer=false, no CORS
- **CI/CD is minimal:** Single workflow without MAUI builds, code analysis, or deployment
- **MAUI is viable but Avalonia offers better cross-platform consistency** for the desktop-focused use case

**Overall assessment:** The project is functional but backward in tooling, infrastructure, and several architecture patterns. It would benefit significantly from a systematic modernization effort.

---

## Findings by Theme

### 1. Tech Stack & Package Management

| Assessment | Details | Evidence |
|---|---|---|
| ✅ .NET version | .NET 10 SDK 10.0.301, `net10.0` target, rollForward "latestFeature" | `global.json`, `*.csproj` |
| ✅ Nullable enabled | `<Nullable>enable</Nullable>` across all projects | `*.csproj` |
| ❌ Package version mismatch | `Logging.Abstractions` v9.0.0 pinned in Inference project vs v10.* elsewhere | `IChing.Lab.Inference.csproj:11` |
| ❌ No Central Package Management | No `Directory.Packages.props` exists | File scan |
| ❌ No .editorconfig | No style enforcement file | File scan |
| ❌ No Roslyn analyzers | None referenced in any project | File scan |
| ❌ xunit outdated | v2.5.3 (v2 maintenance mode); v3 3.2.2 stable exists | `IChing.Lab.Tests.csproj:16-18`, `xunit.net/releases` |
| ❌ Test SDK outdated | `Microsoft.NET.Test.Sdk` 17.8.0 (current would be ~17.14.x) | `IChing.Lab.Tests.csproj:16` |
| ⚠️ Swashbuckle | 6.6.2 — reasonable, but project should evaluate OpenAPI/Scalar alternatives | `Lab.Api.csproj` |
| ✅ ONNX Runtime GenAI | v0.14.1 — matches latest NuGet stable | `IChing.Lab.Inference.csproj:12`, `nuget.org` |

### 2. Architecture & DI Patterns

| Assessment | Details |
|---|---|
| ❌ Manual DI registration | XXXEnginesModule pattern adds boilerplate; no Scrutor assembly scanning |
| ❌ God service | `LabReadService` ~285 lines in a single service |
| ❌ No Result types | Relies on exceptions for error flow |
| ⚠️ Plugin system | Custom plugin loader (not System.Composition/MEF v2) |
| ✅ Clean project separation | ~25 projects with clear responsibility boundaries |
| ✅ Plugin extensibility | External plugin discovery via `PluginLoader` |
| ✅ Immutable records | `UserAccount`, DTOs use `record` types |

**Modern DI recommendation:** [Source: Scrutor best practices 2026]

```csharp
// Replace manual registration with:
services.Scan(scan => scan
    .FromAssemblyOf<IInferenceEngine>()
    .AddClasses(classes => classes.AssignableTo<IInferenceEngine>())
    .AsImplementedInterfaces()
    .WithSingletonLifetime());
```

### 3. MAUI vs Modern UI Frameworks (2026)

[Sources: MAUI discussion #27185, #29647; Avalonia comparison 2026; StartDebugging comparison]

**MAUI in 2026:**
- **Verdict:** Production-ready but not best-in-class
- **Stability:** Far better than 2022-2023 — .NET 9 & 10 brought meaningful perf improvements
- **Performance:** Still slower than native; .NET 11 CoreCLR on Android/iOS is a major improvement
- **Tooling:** Rider/VS support improved but hot reload often breaks
- **Community:** "Cautious confidence" — enterprise apps shipping, but Flutter/Avalonia gaining
- **Microsoft commitment:** Explicitly committed through .NET 10+ roadmap; Build 2026 showed MAUI sessions
- **Android/iOS:** MAUI's strongest ground; Windows/macOS functional

**Avalonia UI:**
- Skia-rendered (pixel-perfect across platforms)
- Linux support (MAUI lacks this)
- Growing enterprise adoption (JetBrains, 170K+ companies)
- Avalonia.MAUI Hybrid allows gradual migration

**Recommendation for IChing:**
- If desktop (Windows/macOS/Linux) is primary: **strongly consider Avalonia**
- If mobile (iOS/Android) is critical: **stick with MAUI** but upgrade tooling
- IChing.Desktop already marked DEPRECATED — focus is Lab.Api web + MAUI client

### 4. AI/Inference Integration

| Component | Used | Best Practice (2026) | Gap |
|---|---|---|---|
| AI Abstraction | Direct OnnxRuntimeGenAI | `Microsoft.Extensions.AI` (IChatClient) | Missing abstraction layer |
| LLM Runtime | OnnxRuntimeGenAI 0.14.1 ✅ | Same — latest stable | OK |
| Prompt Management | Scriban 7.2.5 templates | Same or Liquid | OK but hardcoded paths |
| Provider Abstraction | Custom interfaces | `Microsoft.Extensions.AI` provider model | Should adopt MEAI |
| OpenAI Integration | Custom `OpenAiCompatibleChatClient` | `Microsoft.Extensions.AI.OpenAI` | Replace with MEAI |

**Key finding:** `Microsoft.Extensions.AI` is the new standard (v10.7.0, GA with .NET 10). It provides `IChatClient`, `IEmbeddingGenerator`, built-in telemetry, caching, and DI integration. [Source: Microsoft Learn, learnixo, codingdroplets]

```csharp
// Modern approach with Microsoft.Extensions.AI:
builder.Services.AddChatClient(services =>
    new OpenAIClient(config["OpenAI:Key"]).AsChatClient("gpt-4o"));
// Switch providers: change one line
builder.Services.AddChatClient(services =>
    new OllamaApiClient().AsChatClient("llama3"));
```

### 5. Security Assessment

**Strengths (well-done):**
- PBKDF2 with 100K iterations + 16-byte random salt [Source: `AccountStore.cs:239-258`]
- Constant-time password comparison via `CryptographicOperations.FixedTimeEquals`
- Immutable user records with `record` types
- JWT token auth implemented in Accounts.Api

**Critical Issues:**
| Issue | Location | Severity |
|---|---|---|
| In-memory-only AccountStore | `AccountStore.cs:145-147` — `ConcurrentDictionary`, no persistence | 🔴 CRITICAL — all accounts lost on restart |
| Hardcoded JWT dev key | `Program.cs:16` — "iching-lab-dev-key-change-in-production" | 🔴 CRITICAL — symmetric key in source |
| ValidateIssuer=false | `Program.cs:25` | 🟡 HIGH — token provenance unchecked |
| No auth middleware in Lab.Api | `IChing.Lab.Api/Program.cs` — no `UseAuthentication()` | 🟡 HIGH — Lab API is unauthenticated |
| No CORS configuration | Neither Lab.Api nor Accounts.Api | 🟡 MEDIUM |
| No HTTPS redirect | Neither API has `UseHttpsRedirection()` | 🟡 MEDIUM |
| Mock payment | `AccountStore.cs:218-237` — no Stripe/real integration | ⚠️ LOW for MVP |
| No rate limiting | Accounts.Api has no throttling | 🟡 MEDIUM |

### 6. CI/CD & DevOps

**Current state (lab-ci.yml):**
```
✅ actions/checkout@v4 (latest)
✅ actions/setup-dotnet@v4 (latest)
✅ dotnet 10.0.x SDK
❌ No NuGet caching
❌ No MAUI build/signing/publish
❌ No code quality gates (analyzer reports, coverage)
❌ No Docker support
❌ No deployment (no Azure/AWS publish step)
❌ No matrix builds (Linux only - can't build MAUI)
❌ Builds Accounts.Api but doesn't deploy or test it
❌ Only runs on push/PR to main/master
```

### 7. Testing Quality

| Dimension | Status |
|---|---|
| Test framework | xUnit v2.5.3 (should be v3) |
| Test count vs production | Single test project (`IChing.Lab.Tests`) |
| Mocking library | Not detected |
| Integration tests | Excluded via `Category!=Integration` filter |
| Code coverage | No coverage tooling configured |
| Test patterns | Basic routing tests confirmed |

### 8. Missing Modern .NET Patterns

The project lacks these established .NET best practices:

1. **Microsoft.Extensions.AI** — Unified AI abstraction (GA in .NET 10)
2. **Scrutor** — Assembly scanning DI registration
3. **Central Package Management** — Directory.Packages.props
4. **.editorconfig + Roslyn analyzers** — Code style enforcement
5. **Serilog / OpenTelemetry** — Structured logging and observability
6. **Health checks** — `MapHealthChecks()`
7. **Rate limiting** — `AddRateLimiter()`
8. **API versioning** — `AddApiVersioning()`
9. **FluentValidation** — Input validation
10. **Result types** — OneOf, FluentResults, or Ardalis.Result
11. **EF Core / Dapper** — Proper SQLite persistence
12. **FluentAssertions / Shouldly** — Modern assertion library

---

## Sources (Ranked)

| # | Source | Content | Reliability |
|---|---|---|---|
| 1 | Codebase scan (this session) | All `.csproj`, `.props`, `global.json`, `.editorconfig` | Primary |
| 2 | Codebase grep (this session) | DI patterns, security patterns, AI patterns | Primary |
| 3 | `learn.microsoft.com/dotnet/ai/microsoft-extensions-ai` | MEAI official documentation | High — Official MS |
| 4 | `devblogs.microsoft.com/dotnet/category/maui/` | MAUI roadmap and .NET 11 updates | High — Official MS |
| 5 | `nuget.org/packages/Microsoft.ML.OnnxRuntimeGenAI/` | OnnxRuntimeGenAI version status | High — Official |
| 6 | `github.com/dotnet/maui/discussions/27185` | MAUI community sentiment 2025 | Medium — GitHub discussion |
| 7 | `github.com/dotnet/maui/discussions/29647` | MAUI future discussion | Medium |
| 8 | `startdebugging.net/2026/05/maui-vs-avalonia-vs-uno-in-2026/` | MAUI/Avalonia/Uno comparison | Medium — Independent blog |
| 9 | `codewithmukesh.com/blog/scrutor-dotnet-auto-register-dependencies/` | Scrutor best practices | Medium |
| 10 | `anhtu.dev/microsoft-extensions-ai-unified-ai-abstraction-layer-for-dotnet-10` | MEAI deep dive | Medium |
| 11 | `xunit.net/releases` | xUnit version timeline | High — Official |
| 12 | `github.com/microsoft/onnxruntime-genai` | OnnxRuntime GenAI repo | High — Official |

---

## Verified Claims

| Claim | Verdict | Evidence |
|---|---|---|
| .NET 10 SDK 10.0.301 is latest stable | CONFIRMED | `global.json`, SDK release pattern |
| OnnxRuntimeGenAI 0.14.1 is latest | CONFIRMED | NuGet shows 0.14.1 as latest stable |
| xunit 2.5.3 is outdated | CONFIRMED | v3 3.2.2 stable; v2 in maintenance mode |
| Microsoft.Extensions.AI is GA with .NET 10 | CONFIRMED | v10.7.0 on NuGet, official docs |
| No .editorconfig exists | CONFIRMED | File scan returned zero results |
| No Directory.Packages.props exists | CONFIRMED | File scan returned zero results |
| Logging.Abstractions has version mismatch | CONFIRMED | v9 vs v10.* across projects |
| Accounts.Api has no persistence | CONFIRMED | `ConcurrentDictionary` only storage |
| JWT dev key hardcoded in source | CONFIRMED | `Program.cs:16` |
| Lab.Api has no auth middleware | CONFIRMED | No `UseAuthentication()` in `Program.cs` |
| CI/CD has only 1 workflow, no MAUI build | CONFIRMED | `lab-ci.yml` analysis |

---

## Contradictions

| Finding A | Finding B | Resolution |
|---|---|---|
| MAUI "production-ready in 2026" (ITPath Solutions blog) | "MAUI not ready for production cross-platform" (GitHub discussion) | Both valid: MAUI is production-ready for simple apps; complex UI projects need workarounds |
| Microsoft "heavily investing" in MAUI (official blog) | "Microsoft fired important MAUI engineers" (community discussion) | Official .NET 11 CoreCLR migration shows commitment; team changes don't equal deprecation |

---

## Gaps

These areas could not be fully investigated due to model failures on background workers:
1. **MAUI build/deploy specifics** — iOS signing, Android AOT configuration
2. **External community surveys** — Detailed MAUI vs Avalonia adoption numbers
3. **Performance benchmarks** — No runtime performance testing done

---

## Recommendations Priority

### Critical (security)
1. Add persistence to Accounts.Api (SQLite + EF Core)
2. Move JWT key to environment variable / user secrets
3. Enable issuer validation and remove hardcoded key
4. Add auth middleware to Lab.Api
5. Configure CORS and HTTPS redirect

### High (infrastructure)
6. Add `.editorconfig` + Roslyn analyzers
7. Set up Central Package Management (`Directory.Packages.props`)
8. Unify Logging.Abstractions version (v10.*)
9. Upgrade xunit to v3
10. Upgrade Microsoft.NET.Test.Sdk
11. Add NuGet caching and MAUI build to CI/CD

### Medium (architecture)
12. Adopt `Microsoft.Extensions.AI` for unified AI abstractions
13. Add Scrutor for automatic DI registration
14. Refactor LabReadService (god service)
15. Implement Result types (OneOf/Ardalis.Result)
16. Add FluentValidation
17. Add Serilog/OpenTelemetry
18. Add health checks and rate limiting
19. Add real payment integration (Stripe)

### Low (nice-to-have)
20. Evaluate Avalonia for desktop-focused migration
21. Add API versioning
22. Add Docker support
23. Add code coverage tooling
24. Set up integration test suite
