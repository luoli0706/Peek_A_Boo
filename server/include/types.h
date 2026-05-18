#pragma once
#include <cstdint>

constexpr int MAX_PLAYERS = 7;
constexpr int ENET_CHANNELS = 2;     // ch0=reliable, ch1=unreliable
constexpr int ENET_CH_RELIABLE = 0;
constexpr int ENET_CH_UNRELIABLE = 1;
constexpr uint32_t SERVER_TICK_MS = 33;    // 30Hz tick rate
constexpr uint32_t SERVER_TICK_STEP_MS = 33; // single tick duration
constexpr uint16_t SERVER_PORT = 9000;
