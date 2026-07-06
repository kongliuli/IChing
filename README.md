# IChing

IChing is a .NET Lab for bazi, liuyao, tarot, calendar, prompt, inference, and chart-engine experiments.

The official implementation path is the .NET Lab under `src/`. The former non-.NET prototype has been removed.

## Quick Start

```bash
cd src/IChing.Lab.Api
dotnet run
```

Optional ONNX model download:

```bash
bash scripts/download-qwen-15b-model.sh ./models/qwen2.5-1.5b-genai
```

Prompt dry run:

```bash
cd src
dotnet run --project IChing.Lab.PromptTest -- --dry-run
dotnet run --project IChing.Lab.PromptTest -- --model ../models/qwen2.5-1.5b-genai --fixture tarot-tier1-en
```

## Lab API

| Method | Route | Notes |
| --- | --- | --- |
| POST | `/lab/bazi` | Bazi chart |
| POST | `/lab/bazi/read?tier=0` | Bazi read envelope with `ruleDigest` |
| POST | `/lab/bazi/interpret` | Bazi chart plus inference |
| POST | `/lab/bazi/hepan` | Two-person bazi comparison |
| GET | `/lab/bazi/cities` | City longitude lookup |
| POST | `/lab/liuyao/coin` | Six-line coin chart |
| POST | `/lab/liuyao/time` | Six-line time chart |
| POST | `/lab/liuyao/read?tier=0` | Liuyao read envelope with `ruleDigest` |
| POST | `/lab/tarot/draw` | Tarot draw |
| POST | `/lab/tarot/read?tier=0` | Tarot read envelope with `ruleDigest` |
| POST | `/lab/tarot/interpret` | Tarot draw plus deterministic narrative |
| GET | `/lab/tarot/spreads` | Available spreads |
| GET | `/lab/calendar/day` | Calendar day |
| GET | `/lab/engines` | Registered chart engines |
| GET | `/lab/rules/plugins` | Rule plugin status |
| PUT | `/lab/rules/plugins/{id}` | Enable/disable or reweight a rule plugin |
| GET | `/health/chart-engines` | Chart-engine health |
| GET | `/health/engines` | Inference-engine health |

The Razor management page is available at `/rules/plugins`.

## Rule Engine

Layer1 deterministic rules live in `src/IChing.Lab.Core/Rules/`.

Rules are built-in plugins for now. They are selected by domain, then filtered by `Enabled=false` and `Weight < RuleEngine:MinWeight`.

`ruleDigest` includes:

- `activePlugins`
- `items`: `pluginId`, `title`, `text`, `weight`

Compatibility fields remain in the read envelopes for existing prompt builders, such as liuyao Shi/Ying/yongshen summaries and tarot statistics.

## Sidecar Sample

```bash
scripts/run-chart-sidecar.cmd
```

See `samples/sidecars/IChing.ChartSidecar/README.md`.

## Tarot App

```bash
scripts/run-tarot-app.cmd
```

or:

```bash
cd src/IChing.Tarot.App
dotnet run -f net10.0-windows10.0.19041.0
```

## Projects

| Project | Notes |
| --- | --- |
| `IChing.Lab.Core` | Bazi, liuyao, tarot, calendar, hepan, and deterministic rules |
| `IChing.Lab.Inference` | Prompt orchestration and inference fallback |
| `IChing.Lab.Api` | HTTP and Razor Lab |
| `IChing.Lab.PluginLoader` | External chart/inference plugin loading |
| `IChing.Desktop` | Legacy WPF desktop shell, paused |

## Docs

- `docs/tech-stack-dotnet.md`
- `docs/research-paipan-algorithms.md`
- `docs/inference-layer-design.md`
- `docs/onnx-models-survey.md`

## Tests

```bash
dotnet test src/IChing.Lab.Tests/IChing.Lab.Tests.csproj --no-restore
```
