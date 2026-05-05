# TagSystem

**Script:** `Assets/Scripts/TagSystem.cs`
**Status:** Phase 1 — Planned
**Category:** Gameplay

## Purpose

Handles the Seeker's tag (capture) mechanic. On left-click, performs local raycast to identify target, sends TagAttempt to server, and plays feedback on TagResult.

## Dependencies

- `NetworkManager.Send()` — sends TagAttempt, receives TagResult
- `ClientProtocol.SerializeTagAttempt()` — serialization
- `PlayerController` — only active when role=Seeker
- `GameManager` — only active during Seeking state

## Public API

| Member | Type | Description |
|--------|------|-------------|
| `tagRange` | float | Max tag distance (default 3.0m) |
| `tagCooldown` | float | Min time between tag attempts (default 1.0s) |
| `OnTagResult` | `event Action<bool>` | Callback on tag success/fail |

## Tag Flow

```
1. Seeker aims crosshair at target + left-click
2. Local raycast from camera → check hit.collider has PlayerTag component
3. If hit: extract target player_id, send TagAttempt(targetId) on ch0 reliable
4. Wait for server TagResult (ch0 reliable)
5. On success: play hit SFX/VFX, update score
6. On fail: play miss feedback
7. Cooldown: ignore input for tagCooldown seconds
```

## Server Validation (server-side, not in this script)

- Distance check: seeker ↔ target < 3.0m
- No line-of-sight check in MVP (Phase 3: raycast on server)

## Notes

- TagAttempt is ch0 reliable since it's a critical event (must not be lost).
- Cooldown prevents spam; server also enforces cooldown.
- Tagged player transitions to Spectator role, removed from PlayerStates broadcast.
