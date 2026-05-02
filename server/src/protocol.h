#pragma once
#include <enet/enet.h>
#include <cstdint>
#include <string>

// === Server → Client messages ===

// 0x10 Welcome: player_id (1) + role (1)
void send_welcome(ENetPeer* peer, uint8_t player_id, uint8_t role);

// 0x11 GameStateChange: new_state (1) + countdown_seconds (2 LE)
void send_game_state_change(ENetPeer* peer, uint8_t state, uint16_t countdown_sec);

// === Helpers ===

// Read null-terminated string from packet payload
std::string read_string(const uint8_t* data, size_t max_len);
