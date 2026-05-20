# Peek-A-Boo / 躲猫猫

> 7-player PVP first-person hide-and-seek — 1 Seeker, 6 Hiders, 120 seconds, heartbeat reveal every 30s.
> 7人 PVP 第一人称躲猫猫——1位寻找者，6位躲藏者，120秒倒计时，每30秒全局高亮打破僵局。

**Status / 状态:** Phase 2 — Core gameplay complete (F-Hold tag, heartbeat highlight, neon notifications, robust scoreboard reset)
**Engine / 引擎:** Unity 6.3 LTS (C#) + C++20 dedicated server (ENET UDP)
**License / 许可证:** Apache 2.0

---

## Architecture / 架构

```
Unity Client (C#)                    Dedicated Server (C++20)
┌──────────────────────┐            ┌──────────────────────┐
│  PlayerController    │──ch1 UDP──→│  Tick Loop (30Hz)    │
│  GameManager         │←─ch0 UDP──│  InputBuffer (ring)  │
│  LevelTimer          │            │  Authoritative Move  │
│  MenuController      │←─ch1 UDP──│  PlayerStates bcast  │
│  RemotePlayerManager │            │  GameState machine   │
│  ProceduralMapGen    │            │                      │
└──────────────────────┘            └──────────────────────┘
         │                                    │
         └──────── ENET UDP (dual-channel) ───┘
              ch0 = reliable ordered
              ch1 = unreliable unsequenced (30Hz snapshots)
```

## Quick Start / 快速开始

### Server / 服务端

```bash
cd server
cmake -B build -S .
cmake --build build
./build/server.exe     # listens on UDP :9000
```

Prerequisites: CMake 4.0+, MinGW GCC 14+ (Windows) or GCC 13+ (Linux).  
前置条件：CMake 4.0+、MinGW GCC 14+（Windows）或 GCC 13+（Linux）。

### Client / 客户端

Open the project in Unity 6.3 LTS. Open `SampleScene`, press Play.  
在 Unity 6.3 LTS 中打开项目，打开 `SampleScene`，点击 Play。

Required packages / 必需包: `com.unity.textmeshpro`, `com.unity.inputsystem`

See [docs/phase1-scene-setup.md](docs/phase1-scene-setup.md) for detailed scene configuration.  
详细场景配置参见 [docs/phase1-scene-setup.md](docs/phase1-scene-setup.md)。

## Current Phase / 当前阶段

| Phase | Status | Highlights |
|-------|--------|------------|
| **0** | Done / 已完成 | C++ ENET server ↔ Unity client handshake, Welcome/JoinRoom |
| **1** | Done / 已完成 | 30Hz tick loop, player input sync, procedural map, HUD, timer |
| **2** | Done / 已完成 | Heartbeat highlight, F-Hold interactive tag, dynamic caught visual lock, cyber-neon HUD UI layout & scoreboard round-reset loops |
| **3** | Active / 进行中 | Server reconciliation, client-side prediction, prop hunt mode, AI props |

## Project Structure / 项目结构

```
Peek-A-Boo/
├── Assets/Scripts/           # Unity C# client scripts
│   ├── NetworkManager.cs     #   ENET client lifecycle + events
│   ├── ClientProtocol.cs     #   Hand-written serialization
│   ├── GeneratedProtocol.cs  #   Auto-generated (see Tools/)
│   ├── GameManager.cs        #   Client-side state coordinator
│   ├── PlayerController.cs   #   FPS input + local movement
│   ├── RemotePlayerManager.cs#   Remote player capsule rendering
│   ├── LevelTimer.cs         #   MM:SS countdown display
│   ├── MenuController.cs     #   Main menu + HUD transitions
│   └── ProceduralMapGenerator.cs  # Runtime primitive map generation
├── server/
│   ├── src/
│   │   ├── main.cpp          #   Server entry, tick loop, event dispatch
│   │   ├── player.h          #   Player struct, input ring buffer
│   │   ├── types.h           #   Enums, constants
│   │   ├── protocol.h/cpp    #   Hand-written send/read helpers
│   │   └── generated_protocol.h  # Auto-generated (see Tools/)
│   ├── deps/enet/            #   ENet-CSharp single-file source
│   └── CMakeLists.txt
├── Tools/
│   ├── protocol_defs.json    #   Single source of truth — all messages
│   └── generate_protocol.py  #   Generates C++ + C# from JSON
├── Skills/                   #   Script skill registry (for Claude Code)
│   ├── Core/                 #   game-manager, level-timer, score-manager
│   ├── Gameplay/             #   player-controller, tag-system, highlight
│   ├── Network/              #   network-manager, client-protocol
│   ├── UI/                   #   menu-controller
│   └── World/                #   procedural-map-generator
├── docs/
│   ├── BluePrint.md          #   Game design document (v3.1)
│   ├── phase1-scene-setup.md #   Unity scene configuration guide
│   ├── phase2-gameplay-setup.md #   Phase 2 client components deployment guide
│   └── scene-setup-instructions.md # Main scene YAML reconstruction & HUD physical overlapping fix
└── README.md
```

## Protocol / 协议

Message definitions live in a single JSON file. To regenerate after changes:  
消息定义集中在单个 JSON 文件中。修改后重新生成：

```bash
python3 Tools/generate_protocol.py
# → server/src/generated_protocol.h
# → Assets/Scripts/GeneratedProtocol.cs
```

| Direction | Messages |
|-----------|----------|
| C→S | JoinRoom, PlayerInput, TagAttempt, PlayerReady |
| S→C | Welcome, GameStateChange, PlayerStates, Highlight, TagResult, ScoreBoard, Error |

## Key Design Decisions / 关键设计决策

- **Server-authoritative** — Server owns all game state. Client sends input intents, not positions.
- **服务器权威** — 服务器拥有所有游戏状态。客户端发送输入意图，而非位置。
- **30Hz fixed timestep** — Accumulator-based tick loop, 33ms per step.
- **30Hz 固定时间步** — 基于累加器的 tick 循环，每步 33ms。
- **Dual-channel ENET** — ch0 reliable for events, ch1 unreliable for position snapshots.
- **ENET 双通道** — ch0 可靠通道传输事件，ch1 不可靠通道传输位置快照。
- **Protocol codegen** — Single JSON → Python → C++ + C#. No manual sync drift.
- **协议代码生成** — 单一 JSON → Python → C++ + C#。消除手动同步偏差。
- **Zero asset imports** — Procedural map uses Unity primitives only (Plane, Cube, Cylinder).
- **零资源导入** — 程序化地图仅使用 Unity 基础几何体。
