# Wave 0 - Extended findings (architecture deep-dive)

## DI Composition (LabServiceCollectionExtensions.cs)
- Uses custom `XXXEnginesModule().Register(services)` pattern — not standard .NET DI extensions
- ChartEngineRouter wraps all engines — decent pattern
- AddLabInference is verbose with many TemplatePromptBuilder singletons (string-based domain/tier)
- PluginLoader via custom code, not System.Composition
- NO use of Scrutor for assembly scanning
- NO source generators for DI registration

## Program.cs (Lab.Api) — Issues
- NO Authentication/Authorization middleware (app.UseAuthentication/Authorization missing)
- NO CORS configuration
- NO HTTPS redirect
- NO rate limiting
- NO health check endpoint
- Swashbuckle 6.6.2 (likely outdated)
- DataProtection keys to filesystem (basic, no key encryption)
- ModelPath defaults hardcoded to "./models/qwen3.5-2b-genai"

## LabReadService.cs — God Service Antipattern
- 285 lines handling bazi, liuyao, tarot reads in one class
- Multiple responsibilities: chart calc, rule digest, tier checks, credits, prompt building, inference, response envelope
- Violates Single Responsibility Principle
- Repeated token/maxTokens logic across methods
- String-based template routing is fragile

## Security — Accounts API (Program.cs)
- JWT key: "iching-lab-dev-key-change-in-production" (hardcoded in source)
- JWT validation: ValidateIssuer=false, ValidateAudience=false (security concern)
- AccountStore: 100% in-memory ConcurrentDictionary — no persistence
- Password hashing: PBKDF2+ salt 100K iterations ✅ (well done)
- Minimal API pattern — all endpoints in Program.cs
- NO HTTPS enforcement

## Configuration (appsettings.json)
- Rich multi-engine fallback chain design
- External plugin loading from plugins/ directory
- API key via environment variable (CommercialAi__ApiKey) — good practice
- Accounts disabled by default (good for dev)
- Inference engines: ONNX GenAI, llama.cpp, Ollama, OpenAI, Azure, DeepSeek

## Missing Cross-Cutting Concerns
- NO structured logging (Serilog/OpenTelemetry)
- NO health checks
- NO metrics/APM
- NO request rate limiting
- NO API versioning
- NO proper error handling middleware
- NO correlation IDs/tracing
