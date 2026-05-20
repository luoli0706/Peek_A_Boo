using System;
using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Peekaboo;

public class TagSystem : MonoBehaviour
{
    [Header("Tag Settings")]
    [Tooltip("基础抓捕蓄力时间(秒)")]
    [SerializeField] private float baseInteractTime = 2.0f;
    [Tooltip("有效抓捕最大距离(米)")]
    [SerializeField] private float maxInteractDistance = 3.0f;

    [Header("Optional UI References")]
    [Tooltip("可拖入 Canvas 的 Text 组件用于按键提示")]
    public UnityEngine.UI.Text promptText;
    [Tooltip("可拖入 Canvas 的 Slider 组件用于显示蓄力进度")]
    public UnityEngine.UI.Slider progressSlider;

    [Header("Debug & Diagnostics")]
    [Tooltip("在编辑器且未连接服务器时，强行将自身角色设为 Seeker 以便本地极速调试 F 键交互")]
    [SerializeField] private bool forceSeekerRoleInEditor = true;
    [Tooltip("是否开启 Console 状态诊断日志")]
    [SerializeField] private bool enableDiagnosticLogs = true;

    private float currentHoldTime = 0f;
    private bool isTagAttemptSent = false;
    private byte activeTargetId = 0;
    private Transform activeTargetTransform = null;

    private bool showGuiPrompt = false;
    private string guiPromptMessage = "";
    private bool showGuiProgressBar = false;

    private PlayerRole myRole = PlayerRole.Spectator;
    private Camera cachedCamera;
    private float nextDiagnosticLogTime = 0f;

    // 屏幕中央炫酷通知消息控制
    private float notificationTimer = 0f;
    private string notificationMessage = "";
    private Color notificationColor = Color.green;


    void Start()
    {
        // 1. 健壮获取相机引用 (完美解决 URP 场景中 Camera.main 的 Tag 缺失问题)
        cachedCamera = GetComponentInChildren<Camera>();
        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
        }
        if (cachedCamera == null)
        {
            cachedCamera = FindObjectOfType<Camera>();
        }

        if (cachedCamera != null)
        {
            Debug.Log($"[TagSystem] Cached interactive camera: {cachedCamera.name} (Tag: {cachedCamera.tag})");
        }
        else
        {
            Debug.LogError("[TagSystem] [CRITICAL] No active Camera found in player or scene! F interaction will fail.");
        }

        // 2. 绑定网络逻辑
        if (NetworkManager.Instance != null)
        {
            myRole = (PlayerRole)NetworkManager.Instance.myRole;
            NetworkManager.Instance.OnTagResultReceived += OnTagResultReceived;
        }

        // 3. 编辑器强制 Seeker 调试保险
        #if UNITY_EDITOR
        if (forceSeekerRoleInEditor && (NetworkManager.Instance == null || !NetworkManager.Instance.IsConnected))
        {
            myRole = PlayerRole.Seeker;
            Debug.Log("[TagSystem] [EDITOR DEBUG] Forced local player myRole to Seeker for offline F interact testing!");
        }
        #endif

        // 初始化可选 UI
        if (progressSlider != null)
        {
            progressSlider.gameObject.SetActive(false);
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = 0f;
        }
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnTagResultReceived -= OnTagResultReceived;
        }
    }

    void Update()
    {
        // 只有 Seeker 需要执行抓捕发射逻辑
        if (myRole == PlayerRole.Seeker)
        {
            HandleSeekerInteraction();
        }
        else if (enableDiagnosticLogs && Time.time > nextDiagnosticLogTime)
        {
            nextDiagnosticLogTime = Time.time + 5f;
            Debug.Log($"[TagSystem] Seeker logic bypassed. Current Role: {myRole} (Only Seeker has F tag interaction).");
        }

        if (notificationTimer > 0f)
        {
            notificationTimer -= Time.deltaTime;
        }
    }

    private void HandleSeekerInteraction()
    {
        Camera mainCam = (cachedCamera != null) ? cachedCamera : Camera.main;
        if (mainCam == null)
        {
            if (enableDiagnosticLogs && Time.time > nextDiagnosticLogTime)
            {
                nextDiagnosticLogTime = Time.time + 3f;
                Debug.LogError("[TagSystem] [DIAGNOSTIC] Cannot interact: No camera available!");
            }
            return;
        }

        // 1. 发射准心射线
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        
        bool targetInRange = false;
        byte hitPlayerId = 0;
        Transform hitTransform = null;

        // 绘制辅助 debug 射线，方便在 Scene 视图中观察射线检测方向
        Debug.DrawRay(ray.origin, ray.direction * maxInteractDistance, Color.red);

        // 假设 RemotePlayer 的碰撞体没有 Layer 隔离，我们对准命名为 RemotePlayer_xx 的物体
        if (Physics.Raycast(ray, out hit, maxInteractDistance))
        {
            GameObject hitGo = hit.collider.gameObject;
            if (hitGo.name.StartsWith("RemotePlayer_"))
            {
                string idStr = hitGo.name.Replace("RemotePlayer_", "");
                if (byte.TryParse(idStr, out hitPlayerId))
                {
                    targetInRange = true;
                    hitTransform = hitGo.transform;
                }
            }
        }

        // 2. 更新交互 UI 提示
        if (targetInRange && !isTagAttemptSent)
        {
            string msg = "[F] 长按以捕获 Hider_" + hitPlayerId;
            SetPromptText(msg);
            
            showGuiPrompt = true;
            guiPromptMessage = msg;
        }
        else
        {
            SetPromptText("");
            showGuiPrompt = false;
        }

        // 3. 兼容多套输入系统的 F 键状态检测
        bool fPressed = false;
        #if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            fPressed = Keyboard.current.fKey.isPressed;
        }
        #endif
        if (!fPressed)
        {
            try
            {
                fPressed = Input.GetKey(KeyCode.F);
            }
            catch { }
        }

        // 4. 定期打印控制台诊断日志，帮助快速定位 bug 核心原因
        if (enableDiagnosticLogs && Time.time > nextDiagnosticLogTime)
        {
            nextDiagnosticLogTime = Time.time + 2.0f;
            string hitGoName = (hit.collider != null) ? hit.collider.gameObject.name : "Nothing";
            float hitDist = (hit.collider != null) ? hit.distance : -1f;
            Debug.Log($"[TagSystem Debug] Cam: {mainCam.name}, Ray Hit: {hitGoName} (Dist: {hitDist:F2}m), targetInRange: {targetInRange}, F_Pressed: {fPressed}, myRole: {myRole}");
        }

        // 5. 按下 F 键且目标在范围内时的处理
        if (fPressed && targetInRange && !isTagAttemptSent)
        {
            // 如果刚开始按下或目标切换，锁定当前目标
            if (activeTargetId != hitPlayerId)
            {
                activeTargetId = hitPlayerId;
                activeTargetTransform = hitTransform;
                currentHoldTime = 0f;
            }

            // 心跳与动态距离判定（如果目标移出最大距离，则清零）
            float dist = Vector3.Distance(transform.position, activeTargetTransform.position);
            if (dist > maxInteractDistance)
            {
                // 超出范围重置
                ResetHoldState();
                SetPromptText("<color=red>目标太远！</color>");
                guiPromptMessage = "目标太远！";
                showGuiPrompt = true;
            }
            else
            {
                // 持续蓄力计数
                currentHoldTime += Time.deltaTime;
                float progress = Mathf.Clamp01(currentHoldTime / baseInteractTime);
                UpdateProgressBar(progress);

                showGuiProgressBar = true;

                // 蓄力完成
                if (currentHoldTime >= baseInteractTime)
                {
                    isTagAttemptSent = true;
                    SendTagAttempt(activeTargetId);

                    #if UNITY_EDITOR
                    if (NetworkManager.Instance == null || !NetworkManager.Instance.IsConnected)
                    {
                        LocalSimulateTagSuccess(activeTargetId);
                    }
                    #endif

                    ResetHoldState();
                }
            }
        }
        else
        {
            // 松开按键或没有对准目标，则重置蓄力
            ResetHoldState();
        }
    }

    private void ResetHoldState()
    {
        currentHoldTime = 0f;
        activeTargetId = 0;
        activeTargetTransform = null;
        UpdateProgressBar(0f);
        showGuiProgressBar = false;
    }

    private void LocalSimulateTagSuccess(byte targetId)
    {
        Debug.Log($"[TagSystem] [OFFLINE DEBUG] Simulating TagSuccess for target player {targetId}...");

        // 重置按键状态锁，以防单机调试时被永久锁死无法再次交互
        isTagAttemptSent = false;

        // 1. 寻找被捕获的假人物体并修改其状态为 Caught
        string targetName = $"RemotePlayer_{targetId}";
        GameObject targetGo = GameObject.Find(targetName);
        if (targetGo != null)
        {
            RemotePlayer rp = targetGo.GetComponent<RemotePlayer>();
            if (rp != null)
            {
                PlayerStateEntry entry = new PlayerStateEntry
                {
                    PlayerId = targetId,
                    State = PlayerState.Caught,
                    PosX = targetGo.transform.position.x,
                    PosZ = targetGo.transform.position.z,
                    RotY = targetGo.transform.eulerAngles.y
                };
                rp.UpdateState(entry);
                Debug.Log($"[TagSystem] [OFFLINE DEBUG] Target {targetName} state set to Caught successfully.");
            }
            else
            {
                Debug.LogError($"[TagSystem] [OFFLINE DEBUG] Target {targetName} found, but lacks RemotePlayer component!");
            }
        }
        else
        {
            Debug.LogError($"[TagSystem] [OFFLINE DEBUG] Target GameObject '{targetName}' NOT found in scene!");
        }

        // 2. 检查场上是否还有存活的 Hider
        RemotePlayerManager rpm = FindObjectOfType<RemotePlayerManager>();
        int activeHiders = 0;
        if (rpm != null)
        {
            activeHiders = rpm.GetActiveHiderCount();
            Debug.Log($"[TagSystem] [OFFLINE DEBUG] RemotePlayerManager found. Remaining active Hiders: {activeHiders}");
        }
        else
        {
            Debug.LogWarning("[TagSystem] [OFFLINE DEBUG] RemotePlayerManager NOT found in scene.");
        }

        // 3. 进入结算阶段：为了确保单人测试及任何自定义测试场景绝对能够成功触发并展现 UI，
        // 只要 activeHiders == 0 或者捕获的是测试假人 (targetId == 99)，就强制触发结算面板显示！
        if (activeHiders == 0 || targetId == 99)
        {
            Debug.Log($"[TagSystem] [OFFLINE DEBUG] Triggering settlement! activeHiders: {activeHiders}, targetId: {targetId}");
            
            ScoreManager sm = FindObjectOfType<ScoreManager>();
            if (sm == null)
            {
                // [极度健壮性修复]：若场景中未挂载 ScoreManager，自动创建动态节点并挂载该组件，确保结算 UI 100% 弹出
                Debug.LogWarning("[TagSystem] [AUTO-REPAIR] ScoreManager was NOT found in the scene! Automatically creating 'ScoreManager_AutoCreated' GameObject to host the settlement UI.");
                GameObject scoreManagerGo = new GameObject("ScoreManager_AutoCreated");
                sm = scoreManagerGo.AddComponent<ScoreManager>();
            }

            if (sm != null)
            {
                // 生成包含 Seeker（我）及假人的模拟结算 JSON
                string simJson = "{\"scores\":[" +
                                 "{\"playerId\":0,\"name\":\"Local Seeker\",\"role\":\"Seeker\",\"score\":150,\"tags\":1,\"surviveTime\":0}," +
                                 "{\"playerId\":" + targetId + ",\"name\":\"Hider Dummy " + targetId + "\",\"role\":\"Hider\",\"score\":80,\"tags\":0,\"surviveTime\":45}" +
                                 "]}";

                sm.TriggerLocalOfflineScoreboard(simJson);
                Debug.Log("[TagSystem] [OFFLINE DEBUG] Successfully called TriggerLocalOfflineScoreboard!");
            }
            else
            {
                Debug.LogError("[TagSystem] [OFFLINE DEBUG] [CRITICAL] Failed to find or auto-create ScoreManager!");
            }
        }
        else
        {
            Debug.Log($"[TagSystem] [OFFLINE DEBUG] Capture completed, but activeHiders is {activeHiders} (not 0) and targetId is {targetId} (not 99). Skip auto-settlement.");
        }
    }

    private void SendTagAttempt(byte targetId)
    {
        // 如果抓捕的是本地单机测试假人，直接在本地触发模拟，不需要发送给服务端（服务端并不存在该ID假人，会回包失败）
        if (targetId == 99)
        {
            Debug.Log("[TagSystem] Caught local test dummy (Id=99), bypass network and simulate success directly.");
            LocalSimulateTagSuccess(targetId);

            notificationTimer = 3.0f;
            notificationMessage = $"✔ 抓捕成功！已捕获本地假人 Hider_{targetId}";
            notificationColor = Color.green;
            return;
        }

        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsConnected)
        {
            Debug.LogWarning("[TagSystem] Network disconnected, skip TagAttempt");
            return;
        }

        byte[] tagAttemptData = ClientProtocol.SerializeTagAttempt(targetId);
        NetworkManager.Instance.Send(channel: 0, reliable: true, tagAttemptData);
        Debug.Log($"[TagSystem] Sent TagAttempt target_id={targetId} to server!");
    }

    private void OnTagResultReceived(TagResult result)
    {
        if (result == null) return;

        // 1. 立即释放按键输入锁，允许 Seeker 连续抓捕
        isTagAttemptSent = false;

        Debug.Log($"[TagSystem] OnTagResultReceived: success={result.Success}, Seeker={result.SeekerId}, Target={result.TargetId}");

        // 2. 炫酷赛博通知提示控制
        notificationTimer = 3.0f;
        if (result.Success)
        {
            notificationMessage = $"✔ 抓捕成功！已捕获 Hider_{result.TargetId}";
            notificationColor = Color.green;

            // 本地打击感涂灰冷冻优化：无需等待主同步消息，立即涂灰
            string targetName = $"RemotePlayer_{result.TargetId}";
            GameObject targetGo = GameObject.Find(targetName);
            if (targetGo != null)
            {
                RemotePlayer rp = targetGo.GetComponent<RemotePlayer>();
                if (rp != null)
                {
                    PlayerStateEntry entry = new PlayerStateEntry
                    {
                        PlayerId = (byte)result.TargetId,
                        State = PlayerState.Caught,
                        PosX = targetGo.transform.position.x,
                        PosZ = targetGo.transform.position.z,
                        RotY = targetGo.transform.eulerAngles.y
                    };
                    rp.UpdateState(entry);
                    Debug.Log($"[TagSystem] Local-Visual-Sync: Target {targetName} immediately set to Caught!");
                }
            }
        }
        else
        {
            notificationMessage = "❌ 抓捕失败！超出交互距离";
            notificationColor = Color.red;
        }

        // 3. 如果被抓的是我自己，则进入观战模式
        if (result.Success && NetworkManager.Instance != null && result.TargetId == NetworkManager.Instance.myPlayerId)
        {
            EnterSpectatorMode(result.SeekerId);
        }
    }

    private void EnterSpectatorMode(uint seekerId)
    {
        Debug.Log($"[TagSystem] I was captured by Seeker_{seekerId}! Entering Spectator Mode...");

        // 1. 禁用自身的移动控制器
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.enabled = false;
        }

        // 2. 锁定光标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 3. 寻找捕获者的角色 GameObject 进行相机附着
        string seekerName = $"RemotePlayer_{seekerId}";
        GameObject seekerGo = GameObject.Find(seekerName);

        if (seekerGo != null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                // 将本地相机重置父物体到 Seeker 的肩膀后方，实现无缝观战
                mainCam.transform.SetParent(seekerGo.transform);
                mainCam.transform.localPosition = new Vector3(0.5f, 1.8f, -1.8f);
                mainCam.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
                Debug.Log($"[TagSystem] Camera attached to Seeker_{seekerId} for spectator view.");
            }
        }
        else
        {
            Debug.LogWarning($"[TagSystem] Seeker_{seekerId} GameObject not found. Floating view.");
        }
    }

    // ── UI 控制包装 ──

    private void SetPromptText(string text)
    {
        if (promptText != null)
        {
            if (string.IsNullOrEmpty(text))
            {
                promptText.gameObject.SetActive(false);
            }
            else
            {
                promptText.gameObject.SetActive(true);
                promptText.text = text;
            }
        }
    }

    private void UpdateProgressBar(float value)
    {
        if (progressSlider != null)
        {
            if (value <= 0f)
            {
                progressSlider.gameObject.SetActive(false);
            }
            else
            {
                progressSlider.gameObject.SetActive(true);
                progressSlider.value = value;
            }
        }
    }

    // ── OnGUI 屏幕辅助显示 ──
    // 用于零配置的即插即用表现，渲染极其精美的交互进度提示

    void OnGUI()
    {
        // 1. 全局网络抓捕赛博霓虹通知 (适用于任何角色，捕获打击感翻倍)
        if (notificationTimer > 0f)
        {
            int noteWidth = 420;
            int noteHeight = 44;
            int noteX = Screen.width / 2 - noteWidth / 2;
            int noteY = 120; // 屏幕中上方

            // 卡片磨砂背景
            Texture2D noteBg = MakeTex(noteWidth, noteHeight, new Color(0.02f, 0.03f, 0.05f, 0.95f));
            GUIStyle noteBoxStyle = new GUIStyle();
            noteBoxStyle.normal.background = noteBg;
            GUI.Box(new Rect(noteX, noteY, noteWidth, noteHeight), GUIContent.none, noteBoxStyle);

            // 发光霓虹侧边条
            Texture2D leftStrip = MakeTex(4, noteHeight, notificationColor);
            GUI.DrawTexture(new Rect(noteX, noteY, 4, noteHeight), leftStrip);
            GUI.DrawTexture(new Rect(noteX + noteWidth - 4, noteY, 4, noteHeight), leftStrip);

            GUIStyle noteTextStyle = new GUIStyle();
            noteTextStyle.alignment = TextAnchor.MiddleCenter;
            noteTextStyle.fontSize = 15;
            noteTextStyle.fontStyle = FontStyle.Bold;
            noteTextStyle.normal.textColor = notificationColor;

            // 文本立体阴影
            GUIStyle noteShadowStyle = new GUIStyle(noteTextStyle) { normal = { textColor = Color.black } };
            GUI.Label(new Rect(noteX + 1, noteY + 1, noteWidth, noteHeight), notificationMessage, noteShadowStyle);
            GUI.Label(new Rect(noteX, noteY, noteWidth, noteHeight), notificationMessage, noteTextStyle);
        }

        if (myRole != PlayerRole.Seeker) return;

        // 1. 交互按键提示 (科技卡片式赛博霓虹面板)
        if (showGuiPrompt && !showGuiProgressBar)
        {
            int tipWidth = 320;
            int tipHeight = 36;
            int posX = Screen.width / 2 - tipWidth / 2;
            int posY = Screen.height / 2 + 100;

            // 绘制卡片半透明黑蓝底色
            Texture2D cardBg = MakeTex(tipWidth, tipHeight, new Color(0.04f, 0.06f, 0.1f, 0.85f));
            GUIStyle cardStyle = new GUIStyle();
            cardStyle.normal.background = cardBg;
            GUI.Box(new Rect(posX, posY, tipWidth, tipHeight), GUIContent.none, cardStyle);

            // 绘制左侧的科技霓虹青色发光饰条 (4 像素宽发光青)
            Texture2D lineTex = MakeTex(4, tipHeight, Color.cyan);
            GUI.DrawTexture(new Rect(posX, posY, 4, tipHeight), lineTex);

            GUIStyle promptStyle = new GUIStyle();
            promptStyle.alignment = TextAnchor.MiddleCenter;
            promptStyle.fontSize = 13;
            promptStyle.fontStyle = FontStyle.Bold;
            promptStyle.normal.textColor = Color.yellow;
            
            // 绘制文字阴影
            Rect labelRectShadow = new Rect(posX + 4 + 1, posY + 1, tipWidth - 4, tipHeight);
            GUIStyle shadowStyle = new GUIStyle(promptStyle) { normal = { textColor = Color.black } };
            GUI.Label(labelRectShadow, guiPromptMessage, shadowStyle);

            // 绘制文本
            Rect labelRect = new Rect(posX + 4, posY, tipWidth - 4, tipHeight);
            GUI.Label(labelRect, guiPromptMessage, promptStyle);
        }

        // 2. 长按蓄力进度条 (次世代霓虹发光双色填充槽)
        if (showGuiProgressBar)
        {
            float progress = Mathf.Clamp01(currentHoldTime / baseInteractTime);
            int barWidth = 260;
            int barHeight = 16;
            int posX = Screen.width / 2 - barWidth / 2;
            int posY = Screen.height / 2 + 80;

            // 绘制暗金色微光的半透明黑槽底色
            Texture2D bgTex = MakeTex(barWidth, barHeight, new Color(0.04f, 0.04f, 0.06f, 0.9f));
            GUIStyle bgStyle = new GUIStyle();
            bgStyle.normal.background = bgTex;
            GUI.Box(new Rect(posX, posY, barWidth, barHeight), GUIContent.none, bgStyle);

            // 绘制外侧亮灰色细边框
            Texture2D borderTex = MakeTex(barWidth, 1, new Color(0.3f, 0.35f, 0.4f, 0.8f));
            GUI.DrawTexture(new Rect(posX, posY, barWidth, 1), borderTex);
            GUI.DrawTexture(new Rect(posX, posY + barHeight - 1, barWidth, 1), borderTex);

            // 绘制填充槽
            int fillWidth = (int)(barWidth * progress);
            if (fillWidth > 0)
            {
                // 临界进度颤动闪烁动画：增加抓捕紧张情绪！
                Color fillColor = Color.Lerp(Color.cyan, Color.green, progress);
                if (progress > 0.8f && Mathf.Sin(Time.time * 30f) > 0f)
                {
                    fillColor = Color.white; // 闪白
                }
                
                Texture2D fillTex = MakeTex(fillWidth, barHeight, fillColor);
                GUIStyle fillStyle = new GUIStyle();
                fillStyle.normal.background = fillTex;
                GUI.Box(new Rect(posX, posY, fillWidth, barHeight), GUIContent.none, fillStyle);
            }

            // 绘制心跳跳动的百分比数字
            GUIStyle percentStyle = new GUIStyle();
            percentStyle.alignment = TextAnchor.MiddleCenter;
            percentStyle.fontSize = 12;
            percentStyle.fontStyle = FontStyle.Bold;
            percentStyle.normal.textColor = Color.white;
            Rect percentRect = new Rect(posX, posY - 22, barWidth, 20);
            GUI.Label(percentRect, $"⚡ TARGET CAPTURING... {(int)(progress * 100)}% ⚡", percentStyle);
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
