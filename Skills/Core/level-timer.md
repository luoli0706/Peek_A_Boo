# LevelTimer

**Script:** `Assets/Scripts/LevelTimer.cs`
**Status:** Phase 1 — Planned
**Category:** Core

## Purpose

Displays countdown timer on HUD. Receives time values from server via GameStateChange messages (authoritative), renders locally as a ticking display.

## Dependencies

- `GameManager` — receives countdown_seconds from GameStateChange
- `UnityEngine.UI.Text` or `TMPro.TextMeshProUGUI` — display

## Public API

| Member | Type | Description |
|--------|------|-------------|
| `CurrentTime` | `float` (get) | Remaining seconds |
| `IsRunning` | `bool` (get) | Whether timer is active |
| `SyncTime(ushort seconds)` | void | Sync to server countdown |
| `StartCountdown(ushort seconds)` | void | Begin countdown from server value |
| `Stop()` | void | Stop and hide |

## Display Format

```
MM:SS  (e.g., "01:52" for 112 seconds)
```

Color transitions:
- Green (>30s remaining)
- Yellow (10-30s)
- Red (<10s, pulsing)

## Notes

- Local ticking is cosmetic only — server is authoritative. On state change or re-sync, jump to server value.
- Do NOT use `Time.deltaTime` accumulation for game-logic timing. The local tick is purely visual.
