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

std::string read_string(const uint8_t* data, size_t max_len) {
    if (max_len == 0) return "";
    size_t len = data[0];
    if (len > max_len - 1) len = max_len - 1;
    return std::string(reinterpret_cast<const char*>(data + 1), len);
}
