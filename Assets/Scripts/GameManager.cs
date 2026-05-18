using Peekaboo;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.WaitingForPlayers;
    public ushort CountdownSeconds { get; private set; }
    public PlayerRole MyRole { get; private set; }

    public event System.Action<GameState, ushort> OnStateChanged;

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
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnGameStateChange += HandleGameStateChange;
            MyRole = (PlayerRole)NetworkManager.Instance.myRole;
        }
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.OnGameStateChange -= HandleGameStateChange;
    }

    void HandleGameStateChange(GameState state, ushort countdown)
    {
        CurrentState = state;
        CountdownSeconds = countdown;
        OnStateChanged?.Invoke(state, countdown);

        Debug.Log($"[GameManager] State changed to {state}, countdown={countdown}s");
    }
}
