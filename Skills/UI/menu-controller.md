# MenuController

**Script:** `Assets/Scripts/MenuController.cs`
**Status:** Phase 1 — Planned
**Category:** UI

## Purpose

Manages main menu, lobby screen, and in-game HUD. Handles player name input, server connection, ready state, and UI transitions.

## Dependencies

- `NetworkManager` — serverIP, serverPort, playerName configuration
- `GameManager.OnStateChanged` — UI transitions based on game state

## Public API

| Member | Type | Description |
|--------|------|-------------|
| `ShowMainMenu()` | void | Display main menu |
| `ShowLobby()` | void | Display lobby (WaitingForPlayers) |
| `ShowHUD()` | void | Display in-game HUD |
| `ShowScoreBoard(string json)` | void | Display round results |
| `playerNameInput` | InputField | Player name input field |
| `serverIPInput` | InputField | Server IP input field |

## UI Screens

### Main Menu
- Title ("Peek-A-Boo")
- Player name input field
- Server IP input field (default 127.0.0.1)
- Connect button
- Status text ("Connecting...", "Connected!")

### Lobby (WaitingForPlayers)
- Player list (connected players, ready status)
- Ready button → sends PlayerReady
- "Waiting for players..." status

### In-Game HUD
- Role indicator (Seeker / Hider / Spectator)
- Countdown timer (from LevelTimer)
- Game state label ("Hiding...", "Seeking...")
- Score indicator (Seeker: "Caught 3/6", Hider: "Surviving")

### Scoreboard Overlay
- Round results display (from ScoreManager)
- "Next round in 15s..." countdown

## Notes

- All UI uses Unity Canvas + uGUI (Text, Image, Button) — no custom shaders.
- Phase 1 MVP: Main menu + in-game HUD only. Lobby and scoreboard can be simple overlays.
- NetworkManager connection parameters set from menu input fields before calling Start().
