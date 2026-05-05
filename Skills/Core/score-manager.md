# ScoreManager

**Script:** `Assets/Scripts/ScoreManager.cs`
**Status:** Phase 1 — Planned
**Category:** Core

## Purpose

Receives ScoreBoard messages from server, parses JSON, and displays scores. MVP phase: minimal implementation — just show "Seeker caught X/6".

## Dependencies

- `NetworkManager` — receives `0x15` ScoreBoard messages
- `GameManager` — triggers display on RoundEnd state

## Public API

| Member | Type | Description |
|--------|------|-------------|
| `LastScoreBoard` | `string` (get) | Raw JSON from server |
| `ShowScoreBoard(json)` | void | Parse and display |
| `Hide()` | void | Hide score display |
| `CurrentRound` | `int` | Round number |

## ScoreBoard JSON Format (from server)

```json
{
  "round": 1,
  "seeker": {"id": 0, "name": "PlayerA", "score": 889},
  "hiders": [
    {"id": 1, "name": "PlayerC", "score": 580, "survived": true},
    {"id": 2, "name": "PlayerD", "score": 445, "survived": false}
  ]
}
```

## MVP Simplification

Phase 1: Display "Seeker caught 3/6" as plain text. Full scoreboard UI in Phase 2.

## Notes

- Server is authoritative on all scores — client never calculates scores locally.
- ScoreBoard is sent once per RoundEnd on ch0 reliable.
