# Wave 0 - Pre-research findings (codebase walk)

## .NET SDK & Framework
- global.json: .NET 10.0.301, current as of mid-2026
- ALL projects target net10.0 (MAUI: net10.0-android/ios/maccatalyst/windows)
- Nullable enable, ImplicitUsings enable — good

## MAUI Configuration
- MauiXamlInflator=SourceGen (modern approach)
- Microsoft.Maui.Controls via $(MauiVersion) — good floating pattern
- SupportedOSPlatformVersion: iOS 15.0, maccatalyst 15.0, android 21.0, windows 10.0.17763.0

## Package Versions — Potential Issues
- Microsoft.NET.Test.Sdk v17.8.0 — likely outdated (17.12+ latest)
- xunit v2.5.3 — likely outdated (2.9+ latest)
- xunit.runner.visualstudio v2.5.3
- Swashbuckle.AspNetCore v6.6.2 — v7.x+ available
- coverlet.collector v6.0.0
- Microsoft.ML.OnnxRuntimeGenAI v0.14.1 — check latest stable
- VERSION INCONSISTENCY: Extensions.Logging.Abstractions v9.0.0 in Inference + PromptTest vs v10.* elsewhere
- VERSION INCONSISTENCY: Extensions.Logging.Console v9.0.0 in PromptTest
- Microsoft.Data.Sqlite v10.0.10

## Testing
- xUnit framework, 31 test files, ~158 Fact/Theory methods
- NO mocking library (Moq/NSubstitute/FakeItEasy) detected
- NO assertion library (FluentAssertions/Shouldly) detected
- Integration tests marked with Category!=Integration filter

## CI/CD (lab-ci.yml)
- Single workflow file, only ubuntu-latest runner
- Tests only Lab project — NO MAUI build/test
- No caching step
- No code analysis/linting step
- No signing/publishing/deployment
- actions/checkout@v4 + setup-dotnet@v4 — current versions

## Missing Infrastructure
- No .editorconfig
- No Roslyn analyzer packages
- No Central Package Management (Directory.Packages.props)
- No Docker support detected
- No infrastructure-as-code
