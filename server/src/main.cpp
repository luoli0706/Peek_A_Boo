#include <enet/enet.h>
#include <cstdio>
#include <cstring>
#include <vector>
#include "types.h"
#include "protocol.h"

struct Player {
    uint8_t id;
    Role role;
    bool connected;
};

static std::vector<Player> players;
static uint8_t next_player_id = 0;

static void on_connect(ENetPeer* peer) {
    uint8_t id = next_player_id++;
    uint8_t role = (id == 0) ? static_cast<uint8_t>(Role::Seeker) : static_cast<uint8_t>(Role::Hider);

    players.push_back({id, static_cast<Role>(role), true});

    send_welcome(peer, id, role);
    printf("[+] Player %d connected (role=%d), total=%zu\n", id, role, players.size());
}

static void on_receive(ENetPeer* peer, const ENetPacket* packet) {
    if (packet->dataLength < 1) return;
    uint8_t msg_type = packet->data[0];

    switch (msg_type) {
        case MsgType::JoinRoom: {
            std::string name = read_string(packet->data + 1, packet->dataLength - 1);
            printf("[<] JoinRoom from player: %s\n", name.c_str());
            break;
        }
        case MsgType::PlayerInput:
            // Silent — high-frequency, avoid log spam
            break;
        case MsgType::PlayerReady:
            printf("[<] PlayerReady\n");
            break;
        default:
            printf("[?] Unknown msg_type=0x%02X, len=%zu\n", msg_type, packet->dataLength);
            break;
    }
}

static void on_disconnect(ENetPeer* peer) {
    // Find player by peer
    (void)peer;
    printf("[-] Player disconnected, total=%zu\n", players.size());
}

int main() {
    printf("=== Peek-A-Boo Server v3.0 (ENET) ===\n");

    if (enet_initialize() != 0) {
        fprintf(stderr, "FATAL: enet_initialize() failed\n");
        return 1;
    }
    atexit(enet_deinitialize);

    ENetAddress address;
    address.host = ENET_HOST_ANY;
    address.port = SERVER_PORT;

    ENetHost* host = enet_host_create(&address, MAX_PLAYERS, ENET_CHANNELS, 0, 0);
    if (!host) {
        fprintf(stderr, "FATAL: enet_host_create() failed\n");
        return 1;
    }

    printf("Server listening on UDP port %d (max %d players, %d channels)\n",
           SERVER_PORT, MAX_PLAYERS, ENET_CHANNELS);
    printf("ch0 = reliable (events), ch1 = unreliable (position snapshots)\n");
    printf("Waiting for connections...\n\n");

    ENetEvent event;
    uint32_t last_tick = enet_time_get();

    while (true) {
        int ret = enet_host_service(host, &event, 50); // 50ms timeout = 20Hz tick

        uint32_t now = enet_time_get();
        if (now - last_tick >= SERVER_TICK_MS) {
            // 20Hz tick — broadcast PlayerStates etc. (Phase 1)
            last_tick = now;
        }

        if (ret > 0) {
            switch (event.type) {
                case ENET_EVENT_TYPE_CONNECT:
                    on_connect(event.peer);
                    break;
                case ENET_EVENT_TYPE_RECEIVE:
                    on_receive(event.peer, event.packet);
                    enet_packet_destroy(event.packet);
                    break;
                case ENET_EVENT_TYPE_DISCONNECT:
                    on_disconnect(event.peer);
                    break;
                case ENET_EVENT_TYPE_NONE:
                    break;
            }
        }
    }

    enet_host_destroy(host);
    return 0;
}
