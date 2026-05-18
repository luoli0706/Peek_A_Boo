#include "protocol.h"
#include "types.h"
#include <string>

namespace {
bool send_packet(ENetPeer* peer, const peekaboo::Packet& packet, uint32_t flags) {
    std::string data;
    if (!packet.SerializeToString(&data)) return false;
    ENetPacket* pkt = enet_packet_create(data.data(), data.size(), flags);
    enet_peer_send(peer, ENET_CH_RELIABLE, pkt);
    return true;
}

bool broadcast_packet(ENetHost* host, const peekaboo::Packet& packet, uint32_t flags) {
    std::string data;
    if (!packet.SerializeToString(&data)) return false;
    ENetPacket* pkt = enet_packet_create(data.data(), data.size(), flags);
    enet_host_broadcast(host, ENET_CH_UNRELIABLE, pkt);
    return true;
}
} // namespace

bool parse_packet(const ENetPacket* packet, peekaboo::Packet& out_packet) {
    if (!packet || packet->dataLength == 0) return false;
    return out_packet.ParseFromArray(packet->data, static_cast<int>(packet->dataLength));
}

void send_welcome(ENetPeer* peer, uint8_t player_id, peekaboo::PlayerRole role) {
    peekaboo::Packet packet;
    auto* msg = packet.mutable_welcome();
    msg->set_player_id(player_id);
    msg->set_role(role);
    send_packet(peer, packet, ENET_PACKET_FLAG_RELIABLE);
}

void send_game_state_change(ENetPeer* peer, peekaboo::GameState state, uint16_t countdown_sec) {
    peekaboo::Packet packet;
    auto* msg = packet.mutable_game_state_change();
    msg->set_state(state);
    msg->set_countdown_sec(countdown_sec);
    send_packet(peer, packet, ENET_PACKET_FLAG_RELIABLE);
}

void send_player_states(ENetHost* host, const Player players[], size_t count) {
    peekaboo::Packet packet;
    auto* msg = packet.mutable_player_states();
    for (size_t i = 0; i < count; i++) {
        if (!players[i].connected || players[i].peer == nullptr) continue;
        auto* entry = msg->add_players();
        entry->set_player_id(players[i].id);
        entry->set_state(players[i].state);
        entry->set_pos_x(players[i].pos_x);
        entry->set_pos_z(players[i].pos_z);
        entry->set_rot_y(players[i].rot_y);
    }

    if (msg->players_size() == 0) return;
    broadcast_packet(host, packet, ENET_PACKET_FLAG_UNSEQUENCED);
}
