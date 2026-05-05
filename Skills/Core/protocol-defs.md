# Protocol Definitions (Single Source of Truth)

**Script:** `server/src/protocol_defs.h` (proposed)
**Generated:** `Assets/Scripts/ClientProtocol.cs`, `server/src/protocol.h`
**Status:** Phase 1 — Planned (infrastructure)
**Category:** Core

## Purpose

Eliminate protocol drift between C++ server and C# client by maintaining a single header file that defines all message types, structures, and serialization formats. A Python script generates both C++ and C# source files from it.

## Motivation

Currently protocol definitions are manually mirrored in two files:
- `server/src/protocol.h` / `server/src/types.h` (C++)
- `Assets/Scripts/ClientProtocol.cs` (C#)

Any change to message format requires manual synchronization — the #1 source of silent communication failures in this project.

## Proposed Solution

```
server/src/protocol_defs.h          ← Single source of truth (C-compatible structs + macros)
        │
        ▼
  Tools/generate_protocol.py        ← Python parser + code generator
        │
        ├──→ server/src/protocol_gen.h    (C++ serialization)
        └──→ Assets/Scripts/ClientProtocol_gen.cs  (C# serialization)
```

## protocol_defs.h Format

Simple C-compatible syntax, easy to parse:

```c
// Message type constants
MSG(JoinRoom,    0x01)  // C→S, ch0 reliable
MSG(PlayerInput, 0x02)  // C→S, ch1 unreliable
MSG(TagAttempt,  0x03)  // C→S, ch0 reliable
MSG(PlayerReady, 0x04)  // C→S, ch0 reliable
MSG(Welcome,     0x10)  // S→C, ch0 reliable
MSG(GameStateChange, 0x11)  // S→C, ch0 reliable
MSG(PlayerStates,  0x12)  // S→C, ch1 unreliable
MSG(Highlight,   0x13)  // S→C, ch0 reliable
MSG(TagResult,   0x14)  // S→C, ch0 reliable
MSG(ScoreBoard,  0x15)  // S→C, ch0 reliable
MSG(Error,       0x16)  // S→C, ch0 reliable

// Struct: Welcome payload
STRUCT(WelcomePayload)
  FIELD(u8, player_id)
  FIELD(u8, role)
END_STRUCT

// Struct: PlayerInput payload
STRUCT(PlayerInputPayload)
  FIELD(f32, move_x)
  FIELD(f32, move_z)
  FIELD(f32, rot_y)
  FIELD(u8, flags)
END_STRUCT
```

## Generator Script

- `Tools/generate_protocol.py` — ~200 lines Python
- Reads `protocol_defs.h`, outputs C++ and C# source
- Runs as CMake `add_custom_command` (server) + Unity PreBuild step (client)
- Fails build if protocol_defs.h has parse errors

## Benefits

- Change protocol in ONE place, both ends regenerate
- Compile-time enforcement (mismatch = build failure, not runtime bug)
- Message list self-documents in a single file
- Zero runtime overhead (generated code = hand-written code)

## Implementation Priority

Phase 1.0 (before any new messages are added). The current 7 messages are simple enough to migrate in one session. Cost: ~3 hours. ROI: prevents every future protocol bug.
