#pragma once
#include "enet.h"
#include "player.h"
#include "peekaboo.pb.h"
#include <cstdint>
#include <vector>

// Parse an ENet packet into a protobuf envelope.
bool parse_packet(const ENetPacket* packet, peekaboo::Packet& out_packet);

// Server → Client messages
void send_welcome(ENetPeer* peer, uint8_t player_id, peekaboo::PlayerRole role);
void send_game_state_change(ENetPeer* peer, peekaboo::GameState state, uint16_t countdown_sec);
void send_player_states(ENetHost* host, const Player players[], size_t count);

// Broadcast helpers
void broadcast_game_state_change(ENetHost* host, peekaboo::GameState state, uint16_t countdown_sec);
void broadcast_tag_result(ENetHost* host, uint8_t seeker_id, uint8_t target_id, bool success);
void broadcast_scoreboard(ENetHost* host, const std::vector<Player>& players);

