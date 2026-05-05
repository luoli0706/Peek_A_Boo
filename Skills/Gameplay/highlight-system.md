# HighlightSystem

**Script:** `Assets/Scripts/HighlightSystem.cs`
**Status:** Phase 1 — Planned
**Category:** Gameplay

## Purpose

Displays the 30-second highlight mechanic on the Seeker's client. Receives Highlight messages from server containing hider positions, renders minimap dots and screen-edge direction indicators.

## Dependencies

- `NetworkManager` — receives `0x13` Highlight messages
- `GameManager` — only active during Seeking state
- UnityEngine.UI — minimap and direction indicators

## Public API

| Member | Type | Description |
|--------|------|-------------|
| `highlightDuration` | float | How long highlight stays visible (default 1.0s) |
| `ShowHighlight(Vector2[] hiderPositions)` | void | Trigger highlight display |
| `minimapCamera` | Camera | Overhead camera for minimap rendering |

## Highlight Message Format (0x13, ch0 reliable)

```
[msg_type:1B] [count:1B] [per-hider: id:1B x:4B z:4B]
```

## Display Elements (MVP)

1. **Minimap** — overhead view with red dots at hider positions
2. **Screen-edge arrows** — UI arrows pointing toward off-screen hiders
3. **Pulse effect** — brief screen flash on trigger

## Notes

- Highlight only sent to Seeker client (server filters by role).
- Hider positions are approximate (xz only, no y) — intentional design for tension.
- Stretch goal: wall-hack silhouette shader for hiders within 5m (Phase 3).
