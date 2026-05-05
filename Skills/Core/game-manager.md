# GameManager

**Script:** `Assets/Scripts/GameManager.cs`
**Status:** Phase 1 — Active
**Category:** Core

## Purpose

Client-side singleton that coordinates all game systems. Receives GameStateChange messages from server and orchestrates LevelTimer, ScoreManager, PlayerController, and UI transitions.

## Dependencies

- `NetworkManager` — receives GameStateChange events
- `LevelTimer` — countdown display
- `ScoreManager` — score tracking
- `UIManager` — HUD and menu transitions
- `PlayerController` — enable/disable player input per state

## Public API

| Member | Type | Description |
|--------|------|-------------|
| `CurrentState` | `GameState` (get) | Current game state |
| `OnStateChanged` | `event Action<GameState>` | State transition event |
| `Instance` | `GameManager` (static) | Singleton accessor |

## State Transition Behavior

| State | Client Behavior |
|-------|----------------|
| WaitingForPlayers | Show lobby UI, Ready button |
| Preparing | Show role assignment + countdown |
| Hiding | Seeker=black screen, Hider=free movement (30s) |
| Seeking | Seeker=activate Tag, Hider=hide (120s) |
| RoundEnd | Show ScoreBoard, disable input |
| GameOver | Show final scores, return-to-menu option |

## Network Messages Consumed

- `0x11` GameStateChange → triggers state transition + timer sync

## Notes

- Does NOT drive the state machine — server is authoritative. GameManager only reacts.
- All timer values come from server (countdown_seconds field), never from local clock.
