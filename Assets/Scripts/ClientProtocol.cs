using System;
using Google.Protobuf;
using Peekaboo;

public static class ClientProtocol
{
    // ── Client → Server ──

    public static byte[] SerializeJoinRoom(string playerName)
    {
        var packet = new Packet();
        packet.JoinRoom = new JoinRoom { Name = playerName };
        return packet.ToByteArray();
    }

    public static byte[] SerializePlayerInput(float moveX, float moveZ, float rotY, bool crouch, bool jump)
    {
        uint flags = 0;
        if (crouch) flags |= 0x01;
        if (jump)   flags |= 0x02;

        var packet = new Packet();
        packet.PlayerInput = new PlayerInput
        {
            MoveX = moveX,
            MoveZ = moveZ,
            RotY = rotY,
            Flags = flags
        };
        return packet.ToByteArray();
    }

    public static byte[] SerializeTagAttempt(byte targetId)
    {
        var packet = new Packet();
        packet.TagAttempt = new TagAttempt { TargetId = targetId };
        return packet.ToByteArray();
    }

    public static byte[] SerializePlayerReady()
    {
        var packet = new Packet();
        packet.PlayerReady = new PlayerReady();
        return packet.ToByteArray();
    }
}
