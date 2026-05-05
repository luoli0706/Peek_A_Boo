# ClientProtocol

**Script:** `Assets/Scripts/ClientProtocol.cs`
**Status:** Phase 0 — Active
**Category:** Network

## Purpose

Static class defining shared message type constants, game enums, and binary serialization/deserialization helpers. Mirrors `server/src/protocol.h` and `server/src/types.h`.

## Dependencies

- `System.Text.Encoding` — UTF8 string serialization
- `System.BitConverter` — float/int conversions
- `System.Buffer` — block copy

## Constants

### Message Types (must match server types.h)

| Direction | ID | Name |
|-----------|------|------|
| C→S | `0x01` | JoinRoom |
| C→S | `0x02` | PlayerInput |
| C→S | `0x03` | TagAttempt |
| C→S | `0x04` | PlayerReady |
| S→C | `0x10` | Welcome |
| S→C | `0x11` | GameStateChange |
| S→C | `0x12` | PlayerStates |
| S→C | `0x13` | Highlight |
| S→C | `0x14` | TagResult |
| S→C | `0x15` | ScoreBoard |
| S→C | `0x16` | Error |

### Enums

| Enum | Values |
|------|--------|
| `GameState` | WaitingForPlayers=0, Preparing=1, Hiding=2, Seeking=3, RoundEnd=4, GameOver=5 |
| `PlayerRole` | Seeker=0, Hider=1, Spectator=2 |

## Public API

### Serializers (C→S)

| Method | Returns | Format |
|--------|---------|--------|
| `SerializeJoinRoom(name)` | `byte[]` | `[0x01] [len:1B] [UTF8]` |
| `SerializePlayerInput(mx,mz,ry,crouch,jump)` | `byte[]` | `[0x02] [mx:4B] [mz:4B] [ry:4B] [flags:1B]` |
| `SerializeTagAttempt(targetId)` | `byte[]` | `[0x03] [id:1B]` |
| `SerializePlayerReady()` | `byte[]` | `[0x04]` |

### Deserializers (S→C)

| Method | Parameters (out) |
|--------|------------------|
| `DeserializeWelcome(payload, out playerId, out role)` | `byte playerId, byte role` |
| `DeserializeGameStateChange(payload, out state, out countdown)` | `byte state, ushort countdown` |

## Protocol Drift Risk

C++ server maintains an independent copy of protocol logic in `server/src/protocol.cpp`. Any change to message format must be made in BOTH files. This is the #1 source of silent communication failures. See `Skills/Core/protocol-defs.md` for the planned single-source-of-truth solution.
