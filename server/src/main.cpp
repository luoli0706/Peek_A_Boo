#include "enet.h"
#include "player.h"
#include "types.h"
#include "protocol.h"
#include <cstdio>
#include <cstring>
#include <cmath>
#include <vector>

static std::vector<Player> players;
static uint8_t next_player_id = 0;

static ENetHost* server_host = nullptr;
static peekaboo::GameState game_state = peekaboo::GameState::GAME_STATE_SEEKING;
static float game_timer = 0.0f;

static Player* find_player(ENetPeer* peer) {
    for (auto& p : players) {
        if (p.peer == peer) return &p;
    }
    return nullptr;
}

static void on_connect(ENetPeer* peer) {
    uint8_t id = next_player_id++;
    auto role = (id == 0) ? peekaboo::PlayerRole::PLAYER_ROLE_SEEKER
                          : peekaboo::PlayerRole::PLAYER_ROLE_HIDER;

    Player p;
    p.id = id;
    p.role = role;
    p.connected = true;
    p.peer = peer;
    p.pos_x = 0.0f;
    p.pos_y = 0.0f;
    p.pos_z = 0.0f;
    p.rot_y = 0.0f;
    p.state = peekaboo::PlayerState::PLAYER_STATE_NORMAL;
    snprintf(p.name, sizeof(p.name), "Player%d", id);

    players.push_back(p);

    send_welcome(peer, id, role);
    printf("[+] Player %d connected (role=%d), total=%zu\n", id, static_cast<int>(role), players.size());
}

static void on_receive(ENetPeer* peer, const ENetPacket* packet) {
    peekaboo::Packet msg;
    if (!parse_packet(packet, msg)) {
        printf("[?] Invalid protobuf packet, len=%zu\n", static_cast<size_t>(packet->dataLength));
        return;
    }

    switch (msg.payload_case()) {
        case peekaboo::Packet::kJoinRoom: {
            const std::string& name = msg.join_room().name();
            Player* p = find_player(peer);
            if (p) {
                strncpy(p->name, name.c_str(), sizeof(p->name) - 1);
                p->name[sizeof(p->name) - 1] = '\0';
            }
            printf("[<] JoinRoom from: %s\n", name.c_str());

            // Send current game state to the connecting player
            send_game_state_change(peer, game_state, static_cast<uint16_t>(game_timer));
            break;
        }
        case peekaboo::Packet::kPlayerInput: {
            const auto& input_msg = msg.player_input();
            Player* p = find_player(peer);
            if (p) {
                InputEntry in;
                in.move_x = input_msg.move_x();
                in.move_z = input_msg.move_z();
                in.rot_y = input_msg.rot_y();
                in.flags = static_cast<uint8_t>(input_msg.flags());
                p->input.push(in);
            }
            break;
        }
        case peekaboo::Packet::kTagAttempt: {
            Player* seeker = find_player(peer);
            if (!seeker) {
                printf("[TagAttempt] Sender peer not found!\n");
                break;
            }
            if (seeker->role != peekaboo::PlayerRole::PLAYER_ROLE_SEEKER) {
                printf("[TagAttempt] Player %d attempted to tag, but is not a Seeker! Role=%d\n", seeker->id, static_cast<int>(seeker->role));
                break;
            }

            uint8_t target_id = static_cast<uint8_t>(msg.tag_attempt().target_id());
            Player* target = nullptr;
            for (auto& pl : players) {
                if (pl.id == target_id) {
                    target = &pl;
                    break;
                }
            }

            if (!target) {
                printf("[TagAttempt] Seeker %d tried to tag player %d, but target not found!\n", seeker->id, target_id);
                broadcast_tag_result(server_host, seeker->id, target_id, false);
                break;
            }

            if (target->role != peekaboo::PlayerRole::PLAYER_ROLE_HIDER) {
                printf("[TagAttempt] Seeker %d tried to tag player %d, but target is not a Hider! Role=%d\n", seeker->id, target_id, static_cast<int>(target->role));
                broadcast_tag_result(server_host, seeker->id, target_id, false);
                break;
            }

            if (target->state == peekaboo::PlayerState::PLAYER_STATE_CAUGHT) {
                printf("[TagAttempt] Seeker %d tried to tag player %d, but target is already Caught!\n", seeker->id, target_id);
                broadcast_tag_result(server_host, seeker->id, target_id, false);
                break;
            }

            // Distance calculation
            float dx = seeker->pos_x - target->pos_x;
            float dz = seeker->pos_z - target->pos_z;
            float dist = sqrtf(dx * dx + dz * dz);

            printf("[TagAttempt] Seeker %d tags Hider %d. Distance: %.2f meters\n", seeker->id, target_id, dist);

            // Allow up to 6 meters to account for lag/tick differences
            if (dist <= 6.0f) {
                target->state = peekaboo::PlayerState::PLAYER_STATE_CAUGHT;
                
                broadcast_tag_result(server_host, seeker->id, target_id, true);
                printf("[TagAttempt] Success! Hider %d is now CAUGHT.\n", target_id);

                // Check if all Hiders are caught
                int active_hiders = 0;
                for (const auto& pl : players) {
                    if (pl.connected && pl.role == peekaboo::PlayerRole::PLAYER_ROLE_HIDER && pl.state != peekaboo::PlayerState::PLAYER_STATE_CAUGHT) {
                        active_hiders++;
                    }
                }

                printf("[TagAttempt] Remaining active Hiders: %d\n", active_hiders);

                if (active_hiders == 0) {
                    game_state = peekaboo::GameState::GAME_STATE_ROUND_END;
                    game_timer = 15.0f;

                    broadcast_game_state_change(server_host, game_state, 15);
                    broadcast_scoreboard(server_host, players);
                    printf("[GameLoop] All Hiders caught! Round end triggered. GameState => RoundEnd. ScoreBoard broadcasted!\n");
                }
            } else {
                printf("[TagAttempt] Failed: Seeker %d too far from Hider %d (%.2f meters)\n", seeker->id, target_id, dist);
                broadcast_tag_result(server_host, seeker->id, target_id, false);
            }
            break;
        }
        case peekaboo::Packet::kPlayerReady:
            printf("[<] PlayerReady\n");
            break;
        default:
            printf("[?] Unknown protobuf message, len=%zu\n", static_cast<size_t>(packet->dataLength));
            break;
    }
}

static void on_disconnect(ENetPeer* peer) {
    Player* p = find_player(peer);
    if (p) {
        printf("[-] Player %d (%s) disconnected, total=%zu\n", p->id, p->name, players.size());
        p->connected = false;
        p->peer = nullptr;
    } else {
        printf("[-] Unknown peer disconnected\n");
    }
}

int main() {
    printf("=== Peek-A-Boo Server v3.1 (ENET) ===\n");

    if (enet_initialize() != 0) {
        fprintf(stderr, "FATAL: enet_initialize() failed\n");
        return 1;
    }
    atexit(enet_deinitialize);

    ENetAddress address;
    address.ipv6 = ENET_HOST_ANY;
    address.port = SERVER_PORT;

    ENetHost* host = enet_host_create(&address, MAX_PLAYERS, ENET_CHANNELS, 0, 0, 0);
    if (!host) {
        fprintf(stderr, "FATAL: enet_host_create() failed\n");
        return 1;
    }
    server_host = host;


    printf("Server listening on UDP port %d (max %d players, %d channels)\n",
           SERVER_PORT, MAX_PLAYERS, ENET_CHANNELS);
    printf("ch0 = reliable (events), ch1 = unreliable (%dHz position snapshots)\n", 1000 / SERVER_TICK_STEP_MS);
    printf("Waiting for connections...\n\n");

    ENetEvent event;
    uint32_t accumulator = 0;
    uint32_t last_tick = enet_time_get();

    const float MOVE_SPEED = 5.0f;
    const float ROT_SPEED  = 180.0f;
    const float PI = 3.14159265f;

    while (true) {
        int ret = enet_host_service(host, &event, 1);

        uint32_t now = enet_time_get();
        uint32_t delta = now - last_tick;
        last_tick = now;
        accumulator += delta;

        while (accumulator >= SERVER_TICK_STEP_MS) {
            accumulator -= SERVER_TICK_STEP_MS;
            float dt = SERVER_TICK_STEP_MS / 1000.0f;

            if (game_state == peekaboo::GameState::GAME_STATE_ROUND_END) {
                game_timer -= dt;
                if (game_timer <= 0.0f) {
                    game_state = peekaboo::GameState::GAME_STATE_SEEKING;
                    game_timer = 0.0f;

                    for (auto& p : players) {
                        p.state = peekaboo::PlayerState::PLAYER_STATE_NORMAL;
                    }

                    broadcast_game_state_change(host, game_state, 0);
                    printf("[GameLoop] Round end timer completed. Transitioning to GAME_STATE_SEEKING. Players state reset to NORMAL.\n");
                }
            }

            for (auto& p : players) {
                if (!p.connected || p.peer == nullptr) continue;

                const InputEntry* input = p.input.latest();
                p.input.clear();

                if (input) {
                    p.rot_y = input->rot_y;

                    float rad = p.rot_y * PI / 180.0f;
                    float fx = sinf(rad);
                    float fz = cosf(rad);
                    float rx = cosf(rad);
                    float rz = -sinf(rad);

                    p.pos_x += (input->move_x * rx + input->move_z * fx) * MOVE_SPEED * dt;
                    p.pos_z += (input->move_x * rz + input->move_z * fz) * MOVE_SPEED * dt;

                    p.state = (input->flags & INPUT_FLAG_CROUCH)
                                  ? peekaboo::PlayerState::PLAYER_STATE_CROUCHING
                                  : peekaboo::PlayerState::PLAYER_STATE_NORMAL;
                }
            }

            send_player_states(host, players.data(), players.size());
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
