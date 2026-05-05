# Peek-A-Boo Skills Index

This directory registers all project scripts as skills for Claude Code review and usage.

## Structure

```
Skills/
├── README.md              ← This file
├── Network/               ← Network communication scripts
│   ├── network-manager.md
│   └── client-protocol.md
├── Core/                  ← Core game systems
│   ├── game-manager.md
│   ├── level-timer.md
│   ├── score-manager.md
│   └── protocol-defs.md
├── Gameplay/              ← Player-facing mechanics
│   ├── player-controller.md
│   ├── highlight-system.md
│   └── tag-system.md
├── UI/                    ← User interface scripts
│   └── menu-controller.md
└── World/                 ← World/map generation
    └── procedural-map-generator.md
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
