#include "protocol.h"
#include "types.h"
#include <cstring>

void send_welcome(ENetPeer* peer, uint8_t player_id, uint8_t role) {
    uint8_t buf[3];
    buf[0] = MsgType::Welcome;
    buf[1] = player_id;
    buf[2] = role;

    ENetPacket* pkt = enet_packet_create(buf, sizeof(buf), ENET_PACKET_FLAG_RELIABLE);
    enet_peer_send(peer, ENET_CH_RELIABLE, pkt);
}

void send_game_state_change(ENetPeer* peer, uint8_t state, uint16_t countdown_sec) {
    uint8_t buf[4];
    buf[0] = MsgType::GameStateChange;
    buf[1] = state;
    buf[2] = countdown_sec & 0xFF;       // low byte
    buf[3] = (countdown_sec >> 8) & 0xFF; // high byte

    ENetPacket* pkt = enet_packet_create(buf, sizeof(buf), ENET_PACKET_FLAG_RELIABLE);
    enet_peer_send(peer, ENET_CH_RELIABLE, pkt);
}

void send_player_states(ENetHost* host, const Player players[], size_t count) {
    // 1(msg_type) + 1(count) + count * (id:1 state:1 x:4 z:4 yaw:4) = 2 + count*14
    constexpr size_t MAX_PACKET = 1 + 1 + MAX_PLAYERS * 14;
    uint8_t buf[MAX_PACKET];
    size_t offset = 0;

    buf[offset++] = MsgType::PlayerStates;

    size_t count_offset = offset;
    buf[offset++] = 0; // placeholder
    uint8_t actual_count = 0;

    for (size_t i = 0; i < count; i++) {
        if (!players[i].connected || players[i].peer == nullptr) continue;

        buf[offset++] = players[i].id;
        buf[offset++] = static_cast<uint8_t>(players[i].state);
        memcpy(&buf[offset], &players[i].pos_x, 4); offset += 4;
        memcpy(&buf[offset], &players[i].pos_z, 4); offset += 4;
        memcpy(&buf[offset], &players[i].rot_y, 4); offset += 4;

        actual_count++;
    }

    if (actual_count == 0) return;

    buf[count_offset] = actual_count;

    ENetPacket* pkt = enet_packet_create(buf, offset, ENET_PACKET_FLAG_UNSEQUENCED);
    enet_host_broadcast(host, ENET_CH_UNRELIABLE, pkt);
}

bool read_player_input(const uint8_t* data, size_t max_len,
                       float& out_mx, float& out_mz, float& out_ry,
                       uint8_t& out_flags) {
    if (max_len < 13) return false;
    memcpy(&out_mx, data, 4);
    memcpy(&out_mz, data + 4, 4);
    memcpy(&out_ry, data + 8, 4);
    out_flags = data[12];
    return true;
}

std::string read_string(const uint8_t* data, size_t max_len) {
    if (max_len == 0) return "";
    size_t len = data[0];
    if (len > max_len - 1) len = max_len - 1;
    return std::string(reinterpret_cast<const char*>(data + 1), len);
}
