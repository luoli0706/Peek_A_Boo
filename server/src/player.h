#pragma once
#include "enet.h"
#include "types.h"
#include <cstdint>
#include <cstring>

struct InputEntry {
    float move_x = 0.0f;
    float move_z = 0.0f;
    float rot_y  = 0.0f;
    uint8_t flags = 0;
};

constexpr uint8_t INPUT_FLAG_CROUCH = 0x01;
constexpr uint8_t INPUT_FLAG_JUMP   = 0x02;

constexpr int INPUT_BUFFER_SIZE = 8; // power of 2

struct InputBuffer {
    InputEntry entries[INPUT_BUFFER_SIZE];
    int head = 0;
    int tail = 0;

    bool empty() const { return head == tail; }

    void push(const InputEntry& in) {
        entries[head & (INPUT_BUFFER_SIZE - 1)] = in;
        head++;
        if (head - tail > INPUT_BUFFER_SIZE)
            tail = head - INPUT_BUFFER_SIZE;
    }

    bool pop(InputEntry& out) {
        if (empty()) return false;
        out = entries[tail & (INPUT_BUFFER_SIZE - 1)];
        tail++;
        return true;
    }

    const InputEntry* latest() const {
        if (empty()) return nullptr;
        return &entries[(head - 1) & (INPUT_BUFFER_SIZE - 1)];
    }

    void clear() { head = tail = 0; }
};

struct Player {
    uint8_t id = 0;
    Role role = Role::Spectator;
    bool connected = false;
    ENetPeer* peer = nullptr;
    float pos_x = 0.0f;
    float pos_y = 0.0f;
    float pos_z = 0.0f;
    float rot_y = 0.0f;
    char name[32] = "";
    InputBuffer input;
    PlayerState state = PlayerState::Normal;
};
