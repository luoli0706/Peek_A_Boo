using System;
using System.Collections.Generic;
using UnityEngine;
using Peekaboo;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    [System.Serializable]
    public class ScoreEntry
    {
        public int playerId;
        public string name;
        public string role;
        public int score;
        public int tags;
        public int surviveTime;
    }

    [System.Serializable]
    public class ScoreBoardPayload
    {
        public List<ScoreEntry> scores = new List<ScoreEntry>();
    }

    [Header("Optional UI References")]
    [Tooltip("结算面板 UI Root (Canvas Panel)")]
    public GameObject scoreboardPanel;
    [Tooltip("可拖入 Canvas 用于显示积分文本的 Text")]
    public UnityEngine.UI.Text scoresDisplayText;

    private List<ScoreEntry> currentScores = new List<ScoreEntry>();
    private bool showScoreboard = false;
    private float countdownSec = 15f;
    private bool isCountingDown = false;

    void Start()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnScoreBoardReceived += OnScoreBoardReceived;
            NetworkManager.Instance.OnGameStateChange += OnGameStateChange;
        }

        if (scoreboardPanel != null)
        {
            scoreboardPanel.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnScoreBoardReceived -= OnScoreBoardReceived;
            NetworkManager.Instance.OnGameStateChange -= OnGameStateChange;
        }
    }

    void Update()
    {
        if (isCountingDown && showScoreboard)
        {
            countdownSec -= Time.deltaTime;
            if (countdownSec <= 0)
            {
                showScoreboard = false;
                isCountingDown = false;
                if (scoreboardPanel != null) scoreboardPanel.SetActive(false);
            }
        }
    }

    private void OnGameStateChange(GameState state, ushort countdown)
    {
        // 当收到服务端状态进入 RoundEnd 或 GameOver，自动唤醒结算面板展示，倒计时默认为 15s
        if (state == GameState.RoundEnd || state == GameState.GameOver)
        {
            showScoreboard = true;
            countdownSec = countdown > 0 ? (float)countdown : 15f;
            isCountingDown = true;

            if (scoreboardPanel != null) scoreboardPanel.SetActive(true);
        }
        else
        {
            // 其他状态（Hiding, Seeking）自动重置和关闭结算面板
            showScoreboard = false;
            isCountingDown = false;
            if (scoreboardPanel != null) scoreboardPanel.SetActive(false);
        }
    }

    public void TriggerLocalOfflineScoreboard(string simJson)
    {
        Debug.Log($"[ScoreManager] TriggerLocalOfflineScoreboard received: {simJson}");
        showScoreboard = true;
        countdownSec = 15f;
        isCountingDown = true;
        if (scoreboardPanel != null) scoreboardPanel.SetActive(true);

        // 解析模拟的 JSON 并渲染
        OnScoreBoardReceived(new ScoreBoard { Json = simJson });
    }

    private void OnScoreBoardReceived(ScoreBoard board)
    {
        if (board == null || string.IsNullOrEmpty(board.Json))
        {
            Debug.LogWarning("[ScoreManager] Received empty ScoreBoard JSON payload.");
            return;
        }

        // 【网络强启双保险】收到合法的积分战绩包时，若未显示结算面板，则立即强启面板，并将倒计时重置为 15 秒，以防状态包丢失或错序
        if (!showScoreboard)
        {
            showScoreboard = true;
            countdownSec = 15f;
            isCountingDown = true;
            if (scoreboardPanel != null) scoreboardPanel.SetActive(true);
            Debug.Log("[ScoreManager] Scoreboard Panel auto-triggered by valid ScoreBoard JSON packet (Double-Safe)!");
        }

        Debug.Log($"[ScoreManager] Parsing ScoreBoard JSON: {board.Json}");

        // 健壮性防御解析
        try
        {
            // 双向支持：优先使用 Unity 内置 JsonUtility 反序列化
            ScoreBoardPayload payload = JsonUtility.FromJson<ScoreBoardPayload>(board.Json);
            if (payload != null && payload.scores != null && payload.scores.Count > 0)
            {
                currentScores = payload.scores;
            }
            else
            {
                // 防御性硬编码文本提取：如果是简易的 JSON 键值对，进行防崩溃的手动切分解析
                ParseJsonFallback(board.Json);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ScoreManager] JsonUtility parsing failed. Fallback parsing triggered: {ex.Message}");
            ParseJsonFallback(board.Json);
        }

        // 更新 UI Text 表现
        UpdateScoreboardText();
    }

    private void ParseJsonFallback(string json)
    {
        currentScores.Clear();
        try
        {
            string clean = json.Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "").Replace("\"", "");
            string[] records = clean.Split(new string[] { "scores:" }, StringSplitOptions.None);
            string dataSegment = records.Length > 1 ? records[1] : records[0];

            string[] items = dataSegment.Split(',');
            ScoreEntry entry = new ScoreEntry();
            foreach (var item in items)
            {
                string[] kv = item.Split(':');
                if (kv.Length < 2) continue;
                string key = kv[0].Trim();
                string val = kv[1].Trim();

                if (key == "playerId") int.TryParse(val, out entry.playerId);
                else if (key == "name") entry.name = val;
                else if (key == "role") entry.role = val;
                else if (key == "score") int.TryParse(val, out entry.score);
                else if (key == "tags") int.TryParse(val, out entry.tags);
                else if (key == "surviveTime") int.TryParse(val, out entry.surviveTime);
            }
            if (!string.IsNullOrEmpty(entry.name))
            {
                currentScores.Add(entry);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ScoreManager] Fallback manual parser also failed: {ex.Message}");
        }
    }

    private void UpdateScoreboardText()
    {
        if (scoresDisplayText == null) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== ROUND SCOREBOARD ===");
        sb.AppendLine("Rank\tPlayer\tRole\tScore\tTags\tTime");
        
        int rank = 1;
        currentScores.Sort((a, b) => b.score.CompareTo(a.score));

        foreach (var entry in currentScores)
        {
            sb.AppendLine($"{rank}\t{entry.name}\t{entry.role}\t{entry.score}\t{entry.tags}\t{entry.surviveTime}s");
            rank++;
        }

        scoresDisplayText.text = sb.ToString();
    }

    // ── OnGUI 次世代赛博朋克极简磨砂极光结算 UI ──
    void OnGUI()
    {
        if (!showScoreboard) return;

        // 面板尺寸和中心位置
        int panelWidth = 620;
        int panelHeight = 440;
        int posX = Screen.width / 2 - panelWidth / 2;
        int posY = Screen.height / 2 - panelHeight / 2;

        // 1. 绘制次世代暗黑半透明科幻磨砂背板
        Texture2D bgTex = MakeTex(panelWidth, panelHeight, new Color(0.04f, 0.06f, 0.10f, 0.94f));
        GUIStyle panelStyle = new GUIStyle();
        panelStyle.normal.background = bgTex;
        GUI.Box(new Rect(posX, posY, panelWidth, panelHeight), GUIContent.none, panelStyle);

        // 2. 绘制极光霓虹流光渐变顶部饰条 (Neon Pink -> Neon Cyan)
        int segments = 30;
        float segWidth = (float)panelWidth / segments;
        Color neonPink = new Color(1f, 0.08f, 0.58f, 1f); // Neon Pink #FF1493
        Color neonCyan = new Color(0f, 1f, 1f, 1f);       // Neon Cyan #00FFFF
        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);
            Color segColor = Color.Lerp(neonPink, neonCyan, t);
            Texture2D segTex = MakeTex(Mathf.CeilToInt(segWidth), 4, segColor);
            GUI.DrawTexture(new Rect(posX + i * segWidth, posY, Mathf.CeilToInt(segWidth), 4), segTex);
        }

        // 3. 绘制柔和的 1 像素科技感青色边框
        Texture2D borderTex = MakeTex(1, panelHeight, new Color(0f, 1f, 1f, 0.15f));
        GUI.DrawTexture(new Rect(posX, posY, 1, panelHeight), borderTex);
        GUI.DrawTexture(new Rect(posX + panelWidth - 1, posY, 1, panelHeight), borderTex);
        Texture2D bottomBorder = MakeTex(panelWidth, 1, new Color(0f, 1f, 1f, 0.15f));
        GUI.DrawTexture(new Rect(posX, posY + panelHeight - 1, panelWidth, 1), bottomBorder);

        // 4. 判定胜利阵营，渲染发光大艺术字
        string championRole = "Hider";
        if (currentScores.Count > 0)
        {
            currentScores.Sort((a, b) => b.score.CompareTo(a.score));
            championRole = currentScores[0].role;
        }

        string victoryTitle = championRole == "Seeker" ? "SEEKER DOMINATION" : "HIDERS ESCAPED";
        Color titleColor = championRole == "Seeker" ? new Color(1f, 0.2f, 0.3f) : new Color(0.2f, 1f, 0.4f);
        Color titleGlowColor = championRole == "Seeker" ? new Color(0.5f, 0f, 0.1f, 0.5f) : new Color(0f, 0.4f, 0.1f, 0.5f);

        GUIStyle titleStyle = new GUIStyle();
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontSize = 28;
        titleStyle.fontStyle = FontStyle.Bold;
        
        // 3D 外发光阴影
        titleStyle.normal.textColor = titleGlowColor;
        GUI.Label(new Rect(posX - 2, posY + 25, panelWidth, 40), victoryTitle, titleStyle);
        GUI.Label(new Rect(posX + 2, posY + 25, panelWidth, 40), victoryTitle, titleStyle);
        GUI.Label(new Rect(posX, posY + 25 - 2, panelWidth, 40), victoryTitle, titleStyle);
        GUI.Label(new Rect(posX, posY + 25 + 2, panelWidth, 40), victoryTitle, titleStyle);

        // 前景文字
        titleStyle.normal.textColor = titleColor;
        GUI.Label(new Rect(posX, posY + 25, panelWidth, 40), victoryTitle, titleStyle);

        // 副标题
        GUIStyle subTitleStyle = new GUIStyle();
        subTitleStyle.alignment = TextAnchor.MiddleCenter;
        subTitleStyle.fontSize = 11;
        subTitleStyle.fontStyle = FontStyle.Normal;
        subTitleStyle.normal.textColor = new Color(0.5f, 0.6f, 0.7f);
        GUI.Label(new Rect(posX, posY + 65, panelWidth, 20), "—  R O U N D   S E T T L E M E N T  —", subTitleStyle);

        // 5. 优雅心跳缩放的倒计时文本
        int animFontSize = 13 + (int)(Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 3f);
        GUIStyle timerStyle = new GUIStyle();
        timerStyle.alignment = TextAnchor.MiddleRight;
        timerStyle.fontSize = animFontSize;
        timerStyle.fontStyle = FontStyle.Bold;
        timerStyle.normal.textColor = new Color(1f, 0.5f, 0.1f);
        GUI.Label(new Rect(posX + panelWidth - 240, posY + 32, 220, 30), $"⏳ NEXT ROUND: {Mathf.CeilToInt(countdownSec)}s", timerStyle);

        // 6. 表格头部
        GUIStyle headerStyle = new GUIStyle();
        headerStyle.alignment = TextAnchor.MiddleLeft;
        headerStyle.fontSize = 13;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = new Color(0f, 0.85f, 1f);

        int rowStartX = posX + 40;
        int rowStartY = posY + 105;

        GUI.Label(new Rect(rowStartX, rowStartY, 60, 25), "RANK", headerStyle);
        GUI.Label(new Rect(rowStartX + 60, rowStartY, 140, 25), "PLAYER", headerStyle);
        GUI.Label(new Rect(rowStartX + 200, rowStartY, 100, 25), "FACTION", headerStyle);
        GUI.Label(new Rect(rowStartX + 300, rowStartY, 80, 25), "TAGS", headerStyle);
        GUI.Label(new Rect(rowStartX + 380, rowStartY, 80, 25), "SURVIVE", headerStyle);
        GUI.Label(new Rect(rowStartX + 460, rowStartY, 80, 25), "TOTAL SCORE", headerStyle);

        // 分割线
        Texture2D lineTex = MakeTex(panelWidth - 80, 1, new Color(0f, 0.85f, 1f, 0.3f));
        GUI.DrawTexture(new Rect(rowStartX, rowStartY + 26, panelWidth - 80, 1), lineTex);

        // 7. 循环渲染成绩
        GUIStyle itemStyle = new GUIStyle();
        itemStyle.alignment = TextAnchor.MiddleLeft;
        itemStyle.fontSize = 13;

        int itemY = rowStartY + 38;
        int rank = 1;

        if (currentScores.Count == 0)
        {
            GUIStyle loadingStyle = new GUIStyle();
            loadingStyle.alignment = TextAnchor.MiddleCenter;
            loadingStyle.fontSize = 13;
            loadingStyle.normal.textColor = Color.gray;
            GUI.Label(new Rect(posX, posY + 220, panelWidth, 30), "WAITING FOR SCORE DATA FROM HOST...", loadingStyle);
        }
        else
        {
            foreach (var entry in currentScores)
            {
                if (rank > 7) break;

                string rankPrefix = "";
                Color rowTextColor = Color.white;
                
                if (rank == 1)
                {
                    rankPrefix = "🥇";
                    rowTextColor = new Color(1f, 0.88f, 0.2f);
                    Texture2D goldBg = MakeTex(panelWidth - 80, 28, new Color(1f, 0.88f, 0.2f, 0.07f));
                    GUI.DrawTexture(new Rect(rowStartX - 5, itemY - 2, panelWidth - 80, 28), goldBg);
                }
                else if (rank == 2)
                {
                    rankPrefix = "🥈";
                    rowTextColor = new Color(0.9f, 0.9f, 0.95f);
                    Texture2D silverBg = MakeTex(panelWidth - 80, 28, new Color(1f, 1f, 1f, 0.04f));
                    GUI.DrawTexture(new Rect(rowStartX - 5, itemY - 2, panelWidth - 80, 28), silverBg);
                }
                else if (rank == 3)
                {
                    rankPrefix = "🥉";
                    rowTextColor = new Color(0.9f, 0.6f, 0.4f);
                    Texture2D bronzeBg = MakeTex(panelWidth - 80, 28, new Color(0.9f, 0.6f, 0.4f, 0.03f));
                    GUI.DrawTexture(new Rect(rowStartX - 5, itemY - 2, panelWidth - 80, 28), bronzeBg);
                }
                else
                {
                    rankPrefix = $" {rank}";
                    rowTextColor = new Color(0.8f, 0.85f, 0.9f);
                }

                itemStyle.fontStyle = (rank <= 3) ? FontStyle.Bold : FontStyle.Normal;
                itemStyle.normal.textColor = rowTextColor;

                GUI.Label(new Rect(rowStartX, itemY, 60, 25), rankPrefix, itemStyle);
                GUI.Label(new Rect(rowStartX + 60, itemY, 140, 25), entry.name ?? $"Player_{entry.playerId}", itemStyle);
                
                GUIStyle roleStyle = new GUIStyle(itemStyle);
                if (entry.role == "Seeker")
                {
                    roleStyle.normal.textColor = new Color(1f, 0.3f, 0.4f);
                }
                else if (entry.role == "Hider")
                {
                    roleStyle.normal.textColor = new Color(0.2f, 0.95f, 0.6f);
                }
                GUI.Label(new Rect(rowStartX + 200, itemY, 100, 25), entry.role ?? "Spectator", roleStyle);
                
                GUI.Label(new Rect(rowStartX + 300, itemY, 80, 25), entry.tags.ToString(), itemStyle);
                GUI.Label(new Rect(rowStartX + 380, itemY, 80, 25), $"{entry.surviveTime}s", itemStyle);
                
                GUIStyle scoreStyle = new GUIStyle(itemStyle);
                scoreStyle.fontStyle = FontStyle.Bold;
                if (rank == 1) scoreStyle.normal.textColor = new Color(1f, 0.92f, 0f);
                GUI.Label(new Rect(rowStartX + 460, itemY, 80, 25), $"{entry.score} pts", scoreStyle);

                Texture2D rowLine = MakeTex(panelWidth - 80, 1, new Color(1f, 1f, 1f, 0.03f));
                GUI.DrawTexture(new Rect(rowStartX, itemY + 27, panelWidth - 80, 1), rowLine);

                itemY += 34;
                rank++;
            }
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
