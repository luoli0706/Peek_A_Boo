#pragma once
#include "enet.h"
#include "player.h"
#include <cstdint>
#include <string>

// === Server → Client messages ===

// 0x10 Welcome: player_id (1) + role (1)
void send_welcome(ENetPeer* peer, uint8_t player_id, uint8_t role);

// 0x11 GameStateChange: new_state (1) + countdown_seconds (2 LE)
void send_game_state_change(ENetPeer* peer, uint8_t state, uint16_t countdown_sec);

// 0x12 PlayerStates: broadcast all player positions on ch1 (unreliable)
// Format: [0x12] [count:1] [per-player: id:1 state:1 x:4 z:4 yaw:4]
void send_player_states(ENetHost* host, const Player players[], size_t count);

// === Helpers ===

// Read player input from packet payload (13 bytes: 3 floats + 1 flags byte)
bool read_player_input(const uint8_t* data, size_t max_len,
                       float& out_mx, float& out_mz, float& out_ry,
                       uint8_t& out_flags);

// Read length-prefixed string from packet payload
std::string read_string(const uint8_t* data, size_t max_len);
