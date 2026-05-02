#pragma once
#include <cstdint>

// Game state enum (shared with client)
enum class GameState : uint8_t {
    WaitingForPlayers = 0,
    Preparing = 1,
    Hiding = 2,
    Seeking = 3,
    RoundEnd = 4,
    GameOver = 5
};

// Player role
enum class Role : uint8_t {
    Seeker = 0,
    Hider = 1,
    Spectator = 2
};

// Player live state
enum class PlayerState : uint8_t {
    Normal = 0,
    Crouching = 1,
    Caught = 2,
    Spectating = 3
};

// Message types
namespace MsgType {
    // Client → Server
    constexpr uint8_t JoinRoom     = 0x01;
    constexpr uint8_t PlayerInput  = 0x02;
    constexpr uint8_t TagAttempt   = 0x03;
    constexpr uint8_t PlayerReady  = 0x04;

    // Server → Client
    constexpr uint8_t Welcome         = 0x10;
    constexpr uint8_t GameStateChange = 0x11;
    constexpr uint8_t PlayerStates    = 0x12;
    constexpr uint8_t Highlight       = 0x13;
    constexpr uint8_t TagResult       = 0x14;
    constexpr uint8_t ScoreBoard      = 0x15;
    constexpr uint8_t Error           = 0x16;
}

constexpr int MAX_PLAYERS = 7;
constexpr int ENET_CHANNELS = 2;     // ch0=reliable, ch1=unreliable
constexpr int ENET_CH_RELIABLE = 0;
constexpr int ENET_CH_UNRELIABLE = 1;
constexpr uint32_t SERVER_TICK_MS = 50; // 20Hz
constexpr uint16_t SERVER_PORT = 9000;
