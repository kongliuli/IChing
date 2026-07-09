# Design docs

## Architecture & AI

- **[reading-exchange.md](./reading-exchange.md)** — **ReadingExchange** 统一 AI 交互实体、模板、会话、结果产出与扣费（ADR，2026-07-09）

## App UI direction

Generated image references:

- `tarot-overview.png`: Tarot desktop/tablet main draw and interpretation direction.
- `tarot-single-card.png`: single-card spread responsive direction.
- `tarot-three-card.png`: three-card spread responsive direction.
- `tarot-six-card.png`: six-card spread responsive direction.
- `tarot-celtic-cross.png`: Celtic Cross responsive direction.
- `iching-overview.png`: Bazi/Liuyao shared desktop/tablet direction.

Implementation notes:

- Runtime assets are SVG icons and XAML styles, not large mockup bitmaps.
- Tarot keeps a dark plum + parchment + gold system and uses spread-aware layouts.
- IChing keeps paper + ink + cinnabar + jade, with responsive two-column-to-stacked pages.
- MAUI validates Android/iOS/MacCatalyst/Windows here; Linux desktop needs a separate host if required.
