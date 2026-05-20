using Peekaboo;
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
        // 动态排布 HUD 的 Labels，完美防止在不同分辨率和场景加载时发生任何重叠
        if (roleLabel != null)
        {
            RectTransform roleRect = roleLabel.GetComponent<RectTransform>();
            if (roleRect != null)
            {
                roleRect.anchorMin = new Vector2(0f, 1f); // 锚定到左上角
                roleRect.anchorMax = new Vector2(0f, 1f);
                roleRect.pivot = new Vector2(0f, 1f);
                roleRect.anchoredPosition = new Vector2(20f, -20f); // 距左侧 20，距顶部 -20
                roleRect.sizeDelta = new Vector2(300f, 40f);
                
                roleLabel.fontSize = 18f;
                roleLabel.fontStyle = FontStyles.Bold;
                roleLabel.color = new Color(0f, 1f, 1f); // 科技青色
            }
        }

        if (stateLabel != null)
        {
            RectTransform stateRect = stateLabel.GetComponent<RectTransform>();
            if (stateRect != null)
            {
                stateRect.anchorMin = new Vector2(0f, 1f); // 同样锚定到左上角
                stateRect.anchorMax = new Vector2(0f, 1f);
                stateRect.pivot = new Vector2(0f, 1f);
                stateRect.anchoredPosition = new Vector2(20f, -60f); // 距左侧 20，距顶部 -60 (间隔 40px)
                stateRect.sizeDelta = new Vector2(300f, 40f);

                stateLabel.fontSize = 16f;
                stateLabel.fontStyle = FontStyles.Italic;
                stateLabel.color = new Color(1f, 0.8f, 0f); // 警报金色
            }
        }

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

        NetworkManager.Instance.Connect();
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
