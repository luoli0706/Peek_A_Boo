using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Peekaboo;

public class NetworkManager : MonoBehaviour
{
    [Header("Connection")]
    public string serverIP = "127.0.0.1";
    public ushort serverPort = 9000;
    public string playerName = "Player";

    private ENet.Host host;
    private ENet.Peer peer;

    public static NetworkManager Instance { get; private set; }
    public byte myPlayerId { get; private set; }
    public byte myRole { get; private set; }
    public bool IsConnected => peer.IsSet;

    public event Action<PlayerStates> OnPlayerStates;
    public event Action<GameState, ushort> OnGameStateChange;
    public event Action<Highlight> OnHighlightReceived;
    public event Action<TagResult> OnTagResultReceived;
    public event Action<ScoreBoard> OnScoreBoardReceived;

    private bool enetReady;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (!ENet.Library.Initialize())
        {
            Debug.LogError("[Network] ENet.Library.Initialize() failed!");
            return;
        }
        Debug.Log("[Network] ENET initialized");

        host = new ENet.Host();
        host.Create(1, 2);
        enetReady = true;
        Debug.Log("[Network] Client host created. Call Connect() to join server.");
    }

    public void Connect()
    {
        if (!enetReady || host == null || !host.IsSet)
        {
            Debug.LogError("[Network] Cannot connect: host not initialized");
            return;
        }

        if (peer.IsSet)
        {
            peer.DisconnectNow(0);
            peer = default(ENet.Peer);
        }

        ENet.Address address = new ENet.Address();
        address.SetHost(serverIP);
        address.Port = serverPort;

        peer = host.Connect(address, 2);
        Debug.Log($"[Network] Connecting to {serverIP}:{serverPort}...");
    }

    void Update()
    {
        if (host == null || !host.IsSet) return;

        ENet.Event netEvent;
        while (host.Service(0, out netEvent) > 0)
        {
            switch (netEvent.Type)
            {
                case ENet.EventType.Connect:
                    OnConnect();
                    break;

                case ENet.EventType.Receive:
                    OnReceive(netEvent);
                    netEvent.Packet.Dispose();
                    break;

                case ENet.EventType.Disconnect:
                case ENet.EventType.Timeout:
                    OnDisconnect(netEvent.Type);
                    break;
            }
        }
    }

    void OnConnect()
    {
        Debug.Log("[Network] Connected to server! Waiting for Welcome...");
    }

    void OnReceive(ENet.Event netEvent)
    {
        IntPtr dataPtr = netEvent.Packet.Data;
        int length = netEvent.Packet.Length;
        if (length < 1) return;

        byte[] data = new byte[length];
        Marshal.Copy(dataPtr, data, 0, length);

        Packet packet;
        try
        {
            packet = Packet.Parser.ParseFrom(data);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Network] Failed to parse protobuf packet ({length} bytes): {e.Message}");
            return;
        }

        switch (packet.PayloadCase)
        {
            case Packet.PayloadOneofCase.Welcome:
                HandleWelcome(packet.Welcome);
                break;

            case Packet.PayloadOneofCase.GameStateChange:
                HandleGameStateChange(packet.GameStateChange);
                break;

            case Packet.PayloadOneofCase.PlayerStates:
                OnPlayerStates?.Invoke(packet.PlayerStates);
                break;

            case Packet.PayloadOneofCase.Highlight:
                Debug.Log($"[Network] Highlight: player_id={packet.Highlight.PlayerId}");
                OnHighlightReceived?.Invoke(packet.Highlight);
                break;

            case Packet.PayloadOneofCase.TagResult:
                var tag = packet.TagResult;
                Debug.Log($"[Network] TagResult: seeker={tag.SeekerId}, target={tag.TargetId}, success={tag.Success}");
                OnTagResultReceived?.Invoke(tag);
                break;

            case Packet.PayloadOneofCase.ScoreBoard:
                Debug.Log($"[Network] ScoreBoard: {packet.ScoreBoard.Json}");
                OnScoreBoardReceived?.Invoke(packet.ScoreBoard);
                break;

            case Packet.PayloadOneofCase.Error:
                Debug.LogError($"[Network] Server error: code={packet.Error.Code}");
                break;

            default:
                Debug.LogWarning($"[Network] Unhandled packet: {packet.PayloadCase}");
                break;
        }
    }

    void HandleWelcome(Welcome msg)
    {
        myPlayerId = (byte)msg.PlayerId;
        myRole = (byte)msg.Role;
        string roleName = msg.Role switch
        {
            PlayerRole.Seeker => "Seeker",
            PlayerRole.Hider => "Hider",
            PlayerRole.Spectator => "Spectator",
            _ => "Unknown"
        };
        Debug.Log($"[Network] Welcome! player_id={myPlayerId}, role={roleName}");

        byte[] joinMsg = ClientProtocol.SerializeJoinRoom(playerName);
        Send(channel: 0, reliable: true, joinMsg);
        Debug.Log($"[Network] Sent JoinRoom: \"{playerName}\"");
    }

    void HandleGameStateChange(GameStateChange msg)
    {
        GameState state = msg.State;
        ushort countdown = (ushort)msg.CountdownSec;
        Debug.Log($"[Network] GameState => {state}, countdown={countdown}s");
        OnGameStateChange?.Invoke(state, countdown);
    }

    void OnDisconnect(ENet.EventType type)
    {
        Debug.LogWarning($"[Network] Disconnected (type={type})");
    }

    public void Send(byte channel, bool reliable, byte[] data)
    {
        if (!peer.IsSet) return;

        ENet.Packet packet = default(ENet.Packet);
        ENet.PacketFlags flags = reliable ? ENet.PacketFlags.Reliable : ENet.PacketFlags.Unsequenced;
        packet.Create(data, flags);
        peer.Send(channel, ref packet);
    }

    void OnDestroy()
    {
        if (peer.IsSet)
            peer.DisconnectNow(0);

        if (host != null && host.IsSet)
            host.Dispose();

        ENet.Library.Deinitialize();
        Debug.Log("[Network] Shutdown complete");
    }
}
