using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuController : MonoBehaviour
{
    [Header("Main Menu")]
    public GameObject mainMenuPanel;
    public TMP_InputField playerNameInput;
    public TMP_InputField serverIPInput;
    public Button connectButton;
    public TMP_Text statusText;

    [Header("HUD")]
    public GameObject hudPanel;
    public TMP_Text roleLabel;
    public TMP_Text stateLabel;

    void Start()
    {
        // Main menu setup
        if (connectButton != null)
            connectButton.onClick.AddListener(OnConnectClicked);

        if (serverIPInput != null)
            serverIPInput.text = "127.0.0.1";

        if (playerNameInput != null)
            playerNameInput.text = "Player";

        // Subscribe to game state changes
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += OnGameStateChanged;

        ShowMainMenu();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    void OnConnectClicked()
    {
        if (NetworkManager.Instance == null) return;

        NetworkManager.Instance.serverIP = serverIPInput != null ? serverIPInput.text : "127.0.0.1";
        NetworkManager.Instance.playerName = playerNameInput != null ? playerNameInput.text : "Player";

        if (statusText != null)
            statusText.text = "Connecting...";
    }

    void OnGameStateChanged(GameState state, ushort countdown)
    {
        switch (state)
        {
            case GameState.WaitingForPlayers:
                ShowLobby();
                break;
            case GameState.Preparing:
            case GameState.Hiding:
            case GameState.Seeking:
                ShowHUD();
                break;
            case GameState.RoundEnd:
            case GameState.GameOver:
                ShowHUD(); // ScoreBoard overlay handled by ScoreManager (Phase 2)
                break;
        }

        if (stateLabel != null)
            stateLabel.text = state.ToString();
    }

    public void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);
    }

    public void ShowLobby()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);
        if (stateLabel != null) stateLabel.text = "Waiting for players...";
    }

    public void ShowHUD()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);

        // Show role
        if (NetworkManager.Instance != null && roleLabel != null)
        {
            string roleName = ((PlayerRole)NetworkManager.Instance.myRole) switch
            {
                PlayerRole.Seeker => "Seeker",
                PlayerRole.Hider => "Hider",
                PlayerRole.Spectator => "Spectator",
                _ => "Unknown"
            };
            roleLabel.text = $"Role: {roleName}";
        }
    }
}
