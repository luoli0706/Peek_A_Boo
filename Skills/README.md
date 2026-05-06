# Peek-A-Boo Skills Index

This directory registers all project scripts as skills for Claude Code review and usage.

## Structure

```
Skills/
├── README.md              ← This file
├── Network/               ← Network communication scripts
│   ├── network-manager.md       (Phase 1 Active)
│   └── client-protocol.md       (Phase 1 Active)
├── Core/                  ← Core game systems
│   ├── game-manager.md          (Phase 1 Active)
│   ├── level-timer.md           (Phase 1 Active)
│   ├── score-manager.md         (Phase 2 Planned)
│   └── protocol-defs.md         (Infrastructure)
├── Gameplay/              ← Player-facing mechanics
│   ├── player-controller.md     (Phase 1 Active — verified)
│   ├── remote-player-manager.md (Phase 1 Active)
│   ├── highlight-system.md      (Phase 2 Planned)
│   └── tag-system.md            (Phase 2 Planned)
├── UI/                    ← User interface scripts
│   └── menu-controller.md       (Phase 1 Active — verified)
└── World/                 ← World/map generation
    └── procedural-map-generator.md (Phase 1 Active — verified)
```

## Skill File Format

Each skill file documents:
- **Script path** — where the .cs file lives
- **Status** — Planned / Active / Deprecated
- **Purpose** — what it does
- **Dependencies** — what other scripts/systems it needs
- **Public API** — key members and methods
- **Network messages** — which protocol messages it handles (if any)
- **Notes** — implementation constraints, gotchas, phase plans

## Usage

When reviewing or modifying code, Claude Code reads the relevant skill file first to understand the script's contract, dependencies, and constraints.

## Status Legend

| Status | Meaning |
|--------|---------|
| **Planned** | Script defined but not yet created |
| **Active** | Script exists and is in use |
| **Deprecated** | Script was replaced or removed |

## Phase Status

| Phase | Status |
|-------|--------|
| Phase 0 — ENET 连接 | ✅ 完成 |
| Phase 1 — 基础游戏循环 | ✅ 完成（客户端已验证：连接、移动、视角、跳跃、碰撞） |
| Phase 2 — 核心玩法 | 🔲 规划中（HighlightSystem, TagSystem, ScoreManager） |
| Phase 3 — 服务器权威 | 🔲 规划中（Server reconciliation） |
