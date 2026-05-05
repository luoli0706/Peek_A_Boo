# PlayerController

**Script:** `Assets/Scripts/PlayerController.cs`
**Status:** Phase 1 — Active
**Category:** Gameplay

## Purpose

Handles local player input (WASD, mouse, crouch, jump) and sends input intents to server via NetworkManager. Applies local immediate movement for responsiveness, with server position as final authority (reconciliation deferred to Phase 3).

## Dependencies

- `NetworkManager.Send()` — sends PlayerInput messages on ch1 (unreliable)
- `ClientProtocol.SerializePlayerInput()` — input serialization
- `GameManager` — enables/disables input based on game state
- Unity Input System (FPS template)

## Public API

| Member | Type | Description |
|--------|------|-------------|
| `moveSpeed` | float | Walk speed (default 5.0) |
| `crouchSpeed` | float | Crouch speed (default 2.0) |
| `mouseSensitivity` | float | Look sensitivity |
| `EnableInput()` | void | Enable player control |
| `DisableInput()` | void | Disable player control |
| `ApplyServerPosition(Vector3, float yaw)` | void | Server authority correction |

## Input Send Rate

Send PlayerInput every frame (~60Hz) on ch1 (unreliable). Server processes latest input each tick (20Hz).

## Movement Modes by Role

| Role | Speed | Special |
|------|-------|---------|
| Seeker | 6.0 | Can Tag |
| Hider | 5.0 | Can crouch (silent) |
| Spectator | N/A | Free cam |

## Notes

- Phase 1: No client prediction reconciliation. Local movement is immediate, server corrections are snap-only.
- Phase 3: Add client-side prediction with server reconciliation (store input history, rewind on correction).
- Crouch reduces footstep audio range (Phase 3 audio system).
