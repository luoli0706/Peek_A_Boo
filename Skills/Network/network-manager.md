# NetworkManager

**Script:** `Assets/Scripts/NetworkManager.cs`
**Status:** Phase 0 — Active
**Category:** Network

## Purpose

Unity MonoBehaviour that manages the ENET client connection lifecycle. Handles host creation, server connection, message dispatch, and cleanup.

## Dependencies

- `ENet-CSharp` (Assets/Plugins/ENet-CSharp/Source/Managed/ENet.cs)
- `enet.dll` (Assets/Plugins/enet.dll)
- `ClientProtocol.cs` — serialization helpers
- `MsgType` / `PlayerRole` / `GameState` — from ClientProtocol.cs

## Public API

| Member | Type | Description |
|--------|------|-------------|
| `serverIP` | string | Server address (default 127.0.0.1) |
| `serverPort` | ushort | Server port (default 9000) |
| `playerName` | string | Player display name |
| `IsConnected` | bool (get) | Whether peer is in connected state |
| `Send(channel, reliable, byte[])` | void | Send raw bytes to server |

## Network Messages Handled

| MsgType | Handler | Channel |
|---------|---------|---------|
| `0x10` Welcome | `HandleWelcome()` → auto-sends JoinRoom | ch0 |
| `0x11` GameStateChange | `HandleGameStateChange()` | ch0 |
| `0x12` PlayerStates | Silent (high-frequency, Phase 0) | ch1 |
| `0x13` Highlight | Log only (Phase 0) | ch0 |
| `0x14` TagResult | Log only (Phase 0) | ch0 |
| `0x15` ScoreBoard | Log only (Phase 0) | ch0 |
| `0x16` Error | LogError | ch0 |

## Lifecycle

```
Start() → Library.Initialize() → Host.Create(1,2) → Connect()
Update() → host.Service() → event dispatch (Connect/Receive/Disconnect)
OnDestroy() → DisconnectNow → host.Dispose → Library.Deinitialize
```

## Notes

- `Host.Create(1, 2)` — 1 peer capacity (client), 2 channels (ch0=reliable, ch1=unreliable)
- `Address.SetHost()` + `Address.Port =` (property, not SetPort method)
- `Peer` is a struct — use `peer.IsSet` not `peer != null`
