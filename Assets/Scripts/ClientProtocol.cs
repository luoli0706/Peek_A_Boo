// Peek-A-Boo Client Protocol — mirrors server/src/protocol.h
// Shared message type constants and serialization helpers

public static class MsgType
{
    // Client -> Server
    public const byte JoinRoom    = 0x01;
    public const byte PlayerInput = 0x02;
    public const byte TagAttempt  = 0x03;
    public const byte PlayerReady = 0x04;

    // Server -> Client
    public const byte Welcome         = 0x10;
    public const byte GameStateChange = 0x11;
    public const byte PlayerStates   = 0x12;
    public const byte Highlight      = 0x13;
    public const byte TagResult      = 0x14;
    public const byte ScoreBoard     = 0x15;
    public const byte Error          = 0x16;
}

// Game state enum (must match server types.h)
public enum GameState : byte
{
    WaitingForPlayers = 0,
    Preparing = 1,
    Hiding = 2,
    Seeking = 3,
    RoundEnd = 4,
    GameOver = 5
}

// Player role
public enum PlayerRole : byte
{
    Seeker = 0,
    Hider = 1,
    Spectator = 2
}

public static class ClientProtocol
{
    // --- Client -> Server ---

    // JoinRoom: 1-byte name length prefix + UTF8 name
    public static byte[] SerializeJoinRoom(string playerName)
    {
        byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(playerName);
        byte[] buf = new byte[2 + nameBytes.Length];
        buf[0] = MsgType.JoinRoom;
        buf[1] = (byte)nameBytes.Length;
        System.Buffer.BlockCopy(nameBytes, 0, buf, 2, nameBytes.Length);
        return buf;
    }

    // PlayerInput: float moveX(4) + float moveZ(4) + float rotY(4) + byte flags(1) = 13 bytes
    public static byte[] SerializePlayerInput(float moveX, float moveZ, float rotY, bool crouch, bool jump)
    {
        byte[] buf = new byte[14];
        buf[0] = MsgType.PlayerInput;
        int offset = 1;

        System.BitConverter.GetBytes(moveX).CopyTo(buf, offset); offset += 4;
        System.BitConverter.GetBytes(moveZ).CopyTo(buf, offset); offset += 4;
        System.BitConverter.GetBytes(rotY).CopyTo(buf, offset);  offset += 4;

        byte flags = 0;
        if (crouch) flags |= 0x01;
        if (jump)   flags |= 0x02;
        buf[offset] = flags;

        return buf;
    }

    // TagAttempt: uint8 target_player_id
    public static byte[] SerializeTagAttempt(byte targetId)
    {
        return new byte[] { MsgType.TagAttempt, targetId };
    }

    // PlayerReady: no payload
    public static byte[] SerializePlayerReady()
    {
        return new byte[] { MsgType.PlayerReady };
    }

    // --- Server -> Client deserializers ---

    // Welcome: [msg_type(1)] + player_id(1) + role(1) = 3 bytes
    public static void DeserializeWelcome(byte[] payload, out byte playerId, out byte role)
    {
        playerId = payload[0];
        role = payload[1];
    }

    // GameStateChange: [msg_type(1)] + new_state(1) + countdown(2 LE)
    public static void DeserializeGameStateChange(byte[] payload, out byte state, out ushort countdown)
    {
        state = payload[0];
        countdown = (ushort)(payload[1] | (payload[2] << 8));
    }
}

// Per-player state from server PlayerStates broadcast (0x12)
public struct RemotePlayerState
{
    public byte playerId;
    public byte state;    // PlayerState enum value
    public float posX;
    public float posZ;
    public float rotY;
}

public static class PlayerStatesDeserializer
{
    // PlayerStates: [count:1] [per-player: id:1 state:1 x:4 z:4 yaw:4]
    // Returns null if payload is malformed
    public static RemotePlayerState[] Deserialize(byte[] payload)
    {
        if (payload == null || payload.Length < 1) return null;

        int count = payload[0];
        int expectedLen = 1 + count * 14; // id:1 state:1 x:4 z:4 yaw:4
        if (payload.Length < expectedLen) return null;

        RemotePlayerState[] states = new RemotePlayerState[count];
        int offset = 1;

        for (int i = 0; i < count; i++)
        {
            states[i].playerId = payload[offset]; offset += 1;
            states[i].state    = payload[offset]; offset += 1;
            states[i].posX     = System.BitConverter.ToSingle(payload, offset); offset += 4;
            states[i].posZ     = System.BitConverter.ToSingle(payload, offset); offset += 4;
            states[i].rotY     = System.BitConverter.ToSingle(payload, offset); offset += 4;
        }

        return states;
    }
}
