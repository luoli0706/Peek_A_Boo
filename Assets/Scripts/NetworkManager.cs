using System;
using System.Runtime.InteropServices;
using UnityEngine;

// Peek-A-Boo NetworkManager (Phase 1)
// ENET client lifecycle, message dispatch, events for other systems
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

    // Events for other systems to subscribe
    public event Action<RemotePlayerState[]> OnPlayerStates;
    public event Action<GameState, ushort> OnGameStateChange;

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
        host.Create(1, 2); // client host: 1 peer, 2 channels (ch0=reliable, ch1=unreliable)
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

        peer = host.Connect(address, 2); // 2 channels (ch0=reliable, ch1=unreliable)
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

        // Copy unmanaged data to managed array
        byte[] data = new byte[length];
        Marshal.Copy(dataPtr, data, 0, length);

        byte msgType = data[0];
        byte[] payload = new byte[length - 1];
        if (length > 1)
            Buffer.BlockCopy(data, 1, payload, 0, length - 1);

        switch (msgType)
        {
            case MsgType.Welcome:
                HandleWelcome(payload);
                break;

            case MsgType.GameStateChange:
                HandleGameStateChange(payload);
                break;

            case MsgType.PlayerStates:
                OnPlayerStates?.Invoke(PlayerStatesDeserializer.Deserialize(payload));
                break;

            case MsgType.Highlight:
                Debug.Log($"[Network] Highlight received ({payload.Length} bytes)");
                break;

            case MsgType.TagResult:
                Debug.Log($"[Network] TagResult: seeker={payload[0]}, target={payload[1]}, success={payload[2]}");
                break;

            case MsgType.ScoreBoard:
                string json = System.Text.Encoding.UTF8.GetString(payload);
                Debug.Log($"[Network] ScoreBoard: {json}");
                break;

            case MsgType.Error:
                Debug.LogError($"[Network] Server error: code={payload[0]}");
                break;

            default:
                Debug.LogWarning($"[Network] Unknown msg_type=0x{msgType:X2}, len={length}");
                break;
        }
    }

    void HandleWelcome(byte[] payload)
    {
        ClientProtocol.DeserializeWelcome(payload, out byte playerId, out byte role);
        myPlayerId = playerId;
        myRole = role;
        string roleName = (PlayerRole)myRole switch
        {
            PlayerRole.Seeker => "Seeker",
            PlayerRole.Hider => "Hider",
            PlayerRole.Spectator => "Spectator",
            _ => "Unknown"
        };
        Debug.Log($"[Network] Welcome! player_id={myPlayerId}, role={roleName}");

        // Auto-send JoinRoom
        byte[] joinMsg = ClientProtocol.SerializeJoinRoom(playerName);
        Send(channel: 0, reliable: true, joinMsg);
        Debug.Log($"[Network] Sent JoinRoom: \"{playerName}\"");
    }

    void HandleGameStateChange(byte[] payload)
    {
        ClientProtocol.DeserializeGameStateChange(payload, out byte state, out ushort countdown);
        Debug.Log($"[Network] GameState => {(GameState)state}, countdown={countdown}s");
        OnGameStateChange?.Invoke((GameState)state, countdown);
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
