using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Peekaboo;

public class HighlightSystem : MonoBehaviour
{
    [Header("Highlight Settings")]
    [Tooltip("每次曝光脉冲的持续高亮时间(秒)")]
    [SerializeField] private float highlightDuration = 3.0f;

    [Header("Optional UI References")]
    [Tooltip("用于 Canvas 上动态生成的高亮指示器 Prefab")]
    public GameObject indicatorPrefab;
    [Tooltip("用于挂载指示器的 Canvas 父节点")]
    public RectTransform canvasParent;

    private Dictionary<int, Coroutine> activeHighlights = new Dictionary<int, Coroutine>();
    private Dictionary<int, Transform> targetsToHighlight = new Dictionary<int, Transform>();

    private PlayerRole myRole = PlayerRole.Spectator;

    void Start()
    {
        if (NetworkManager.Instance != null)
        {
            myRole = (PlayerRole)NetworkManager.Instance.myRole;
            NetworkManager.Instance.OnHighlightReceived += OnHighlightReceived;
        }
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnHighlightReceived -= OnHighlightReceived;
        }
    }

    private void OnHighlightReceived(Highlight msg)
    {
        if (msg == null) return;

        // 只有 Seeker 端能够处理和偷窥被高亮的躲藏者坐标
        if (myRole != PlayerRole.Seeker) return;

        int exposedPlayerId = (int)msg.PlayerId;
        Debug.Log($"[HighlightSystem] Exposed Hider_{exposedPlayerId} pulse received!");

        // 1. 在场景中检索对应的远程 Hider 玩家物体
        string targetName = $"RemotePlayer_{exposedPlayerId}";
        GameObject targetGo = GameObject.Find(targetName);

        if (targetGo != null)
        {
            // 如果此玩家已存在高亮协程，则先停止它以刷新高亮时间
            if (activeHighlights.TryGetValue(exposedPlayerId, out Coroutine existingCoroutine) && existingCoroutine != null)
            {
                StopCoroutine(existingCoroutine);
            }

            targetsToHighlight[exposedPlayerId] = targetGo.transform;
            activeHighlights[exposedPlayerId] = StartCoroutine(HighlightTimerCoroutine(exposedPlayerId));
        }
        else
        {
            Debug.LogWarning($"[HighlightSystem] Exposed target {targetName} not found in scene.");
        }
    }

    private IEnumerator HighlightTimerCoroutine(int playerId)
    {
        // 如果拖入了 Indicator UI 预制体，我们可以在 UI Canvas 上实例化一个红点指示器
        GameObject spawnedIndicator = null;
        if (indicatorPrefab != null && canvasParent != null)
        {
            spawnedIndicator = Instantiate(indicatorPrefab, canvasParent);
            spawnedIndicator.SetActive(true);
        }

        float elapsed = 0f;
        while (elapsed < highlightDuration)
        {
            elapsed += Time.deltaTime;
            
            // 如果 Canvas 上的指示器存在，我们可以计算其屏幕坐标位置
            if (spawnedIndicator != null && targetsToHighlight.ContainsKey(playerId))
            {
                Transform targetTransform = targetsToHighlight[playerId];
                if (targetTransform != null)
                {
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(targetTransform.position);
                    
                    // 判断是否在相机前方
                    if (screenPos.z > 0)
                    {
                        spawnedIndicator.transform.position = screenPos;
                    }
                }
            }

            yield return null;
        }

        // 销毁 UI 指示器
        if (spawnedIndicator != null)
        {
            Destroy(spawnedIndicator);
        }

        targetsToHighlight.Remove(playerId);
        activeHighlights.Remove(playerId);
    }

    // ── OnGUI 屏幕辅助指示器 ──
    // 用于无配置的高水准直接视觉呈现，在屏幕中央下方和目标方位渲染华丽的红点测距与方向箭头

    void OnGUI()
    {
        if (myRole != PlayerRole.Seeker || Camera.main == null) return;

        GUIStyle labelStyle = new GUIStyle();
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.fontSize = 14;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.normal.textColor = Color.red;

        foreach (var entry in targetsToHighlight)
        {
            Transform targetTransform = entry.Value;
            if (targetTransform == null) continue;

            // 计算该被曝光躲藏者在世界坐标与主相机的屏幕空间投影
            Vector3 worldPos = targetTransform.position;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            // 只有当 Hider 处于相机前方时才渲染世界坐标标记
            if (screenPos.z > 0)
            {
                float dist = Vector3.Distance(Camera.main.transform.position, worldPos);
                string text = $"⚠️ HIDER_{entry.Key} ({dist:F1}m)";
                
                // 绘制一个小红点指示
                int dotSize = 12;
                Texture2D dotTex = MakeRedDot(dotSize, dotSize);
                GUI.DrawTexture(new Rect(screenPos.x - dotSize / 2, Screen.height - screenPos.y - dotSize / 2, dotSize, dotSize), dotTex);

                // 绘制文本 (带阴影效果)
                GUIStyle shadowStyle = new GUIStyle(labelStyle);
                shadowStyle.normal.textColor = Color.black;
                GUI.Label(new Rect(screenPos.x - 100 + 1, Screen.height - screenPos.y - 30 + 1, 200, 20), text, shadowStyle);
                GUI.Label(new Rect(screenPos.x - 100, Screen.height - screenPos.y - 30, 200, 20), text, labelStyle);
            }
            else
            {
                // 如果被曝光的目标在玩家视野后方，在屏幕边缘绘制方向警告
                Vector3 localPos = Camera.main.transform.InverseTransformPoint(worldPos);
                string edgeText = "⚠️ 警报: 后方有躲藏者曝光！";
                if (localPos.x > 0)
                    edgeText = "⚠️ 警报: 右后方有躲藏者曝光！";
                else if (localPos.x < 0)
                    edgeText = "⚠️ 警报: 左后方有躲藏者曝光！";

                GUIStyle edgeStyle = new GUIStyle(labelStyle);
                edgeStyle.fontSize = 16;
                edgeStyle.normal.textColor = new Color(1f, 0.2f, 0.2f, Mathf.PingPong(Time.time * 2f, 1f)); // 呼吸闪烁

                GUIStyle shadowStyle = new GUIStyle(edgeStyle);
                shadowStyle.normal.textColor = Color.black;

                Rect rectShadow = new Rect(Screen.width / 2 - 200 + 1, 40 + 1, 400, 30);
                Rect rect = new Rect(Screen.width / 2 - 200, 40, 400, 30);
                
                GUI.Label(rectShadow, edgeText, shadowStyle);
                GUI.Label(rect, edgeText, edgeStyle);
            }
        }
    }

    private Texture2D MakeRedDot(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        Color[] pix = new Color[width * height];
        
        float r = width / 2.0f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = x - r;
                float dy = y - r;
                if (dx * dx + dy * dy <= r * r)
                {
                    pix[y * width + x] = new Color(1f, 0f, 0f, 0.9f);
                }
                else
                {
                    pix[y * width + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(pix);
        texture.Apply();
        return texture;
    }
}
