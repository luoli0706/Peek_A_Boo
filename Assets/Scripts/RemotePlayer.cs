using UnityEngine;
using Peekaboo;

public class RemotePlayer : MonoBehaviour
{
    [Header("Identity")]
    public int playerId = -1;
    public PlayerRole role;
    public PlayerState state;

    [Header("Sync Settings")]
    [Tooltip("平滑移动速度")]
    public float lerpSpeed = 15f;
    [Tooltip("瞬移阈值(米)")]
    public float teleportThreshold = 5f;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool hasFirstState = false;

    private Material playerMaterial;
    private MeshRenderer meshRenderer;
    private Collider playerCollider;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        playerCollider = GetComponent<Collider>();

        // 自动补充 CapsuleCollider：防止远程玩家或本地假人 Prefab 没有挂载碰撞体而导致射线穿透、无法被按 F 捕获
        if (playerCollider == null)
        {
            CapsuleCollider cc = gameObject.AddComponent<CapsuleCollider>();
            cc.center = new Vector3(0f, 0f, 0f);
            cc.radius = 0.5f;
            cc.height = 2.0f;
            playerCollider = cc;
            Debug.LogWarning($"[RemotePlayer] Remote player {playerId} lacks a Collider. Automatically added CapsuleCollider (Height=2.0) for raycast support.");
        }

        // 实例化材质，防止修改公共 Prefab 材质造成所有玩家串色
        if (meshRenderer != null)
        {
            playerMaterial = meshRenderer.material;
        }
    }

    /// <summary>
    /// 初始化远程玩家角色基础身份
    /// </summary>
    public void Setup(int id)
    {
        playerId = id;
        // id == 0 是固定 Seeker，其余是 Hider
        role = (id == 0) ? PlayerRole.Seeker : PlayerRole.Hider;
        UpdateVisuals();
    }

    /// <summary>
    /// 接收网络消息包更新目标状态与位置
    /// </summary>
    public void UpdateState(PlayerStateEntry entry)
    {
        state = entry.State;

        // 水平目标位置 (PosX, PosZ)
        Vector3 newTargetPos = new Vector3(entry.PosX, 0.5f, entry.PosZ);

        // ── 贴地修正逻辑 (Ground Snapping) ──
        // 从目标坐标上空 5 米处向下发射射线，精准定位地面/障碍物高度
        Vector3 rayStart = new Vector3(entry.PosX, 5f, entry.PosZ);
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 10f);
        float bestGroundY = 0f;
        bool foundGround = false;

        foreach (var hit in hits)
        {
            // 排除自身碰撞体以及其他远程玩家，防止射线射在角色头上造成悬空
            if (hit.collider.gameObject != gameObject && !hit.collider.name.StartsWith("RemotePlayer_"))
            {
                if (hit.point.y > bestGroundY || !foundGround)
                {
                    bestGroundY = hit.point.y;
                    foundGround = true;
                }
            }
        }

        if (foundGround)
        {
            // Capsule pivot 位于中心，贴地时中心坐标为地面高度加 1 米 (胶囊体高度为 2)
            newTargetPos.y = bestGroundY + 1f;
        }
        else
        {
            newTargetPos.y = 1.0f; // 默认平地高度
        }

        targetPosition = newTargetPos;
        targetRotation = Quaternion.Euler(0f, entry.RotY, 0f);

        if (!hasFirstState)
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            hasFirstState = true;
        }

        UpdateVisuals();
    }

    void Update()
    {
        if (!hasFirstState) return;

        // 如果被抓捕了，则立刻停止所有平滑插值移动，固定在被抓捕点
        if (state == PlayerState.Caught)
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            return;
        }

        // 平滑插值更新位置
        float dist = Vector3.Distance(transform.position, targetPosition);
        if (dist > teleportThreshold)
        {
            transform.position = targetPosition;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * lerpSpeed);
        }

        // 平滑插值更新旋转
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * lerpSpeed);
    }

    /// <summary>
    /// 根据阵营与当前姿态状态动态刷新网格渲染色彩与隐形逻辑
    /// </summary>
    private void UpdateVisuals()
    {
        if (meshRenderer == null || playerMaterial == null) return;

        // 旁观者状态：对其他玩家完全不可见
        if (state == PlayerState.Spectating)
        {
            meshRenderer.enabled = false;
            if (playerCollider != null) playerCollider.enabled = false;
            return;
        }

        meshRenderer.enabled = true;
        if (playerCollider != null) playerCollider.enabled = true;

        Color targetColor = Color.white;

        if (state == PlayerState.Caught)
        {
            // 被抓捕状态：展现冰冷的灰色，并关闭任何自发光
            targetColor = new Color(0.4f, 0.4f, 0.4f);
            playerMaterial.DisableKeyword("_EMISSION");
        }
        else
        {
            if (role == PlayerRole.Seeker)
            {
                // 抓捕者：霓虹橙红色自发光，增加压迫感
                targetColor = new Color(1.0f, 0.2f, 0.0f);
                playerMaterial.EnableKeyword("_EMISSION");
                playerMaterial.SetColor("_EmissionColor", targetColor * 0.6f);
            }
            else
            {
                // 躲藏者
                if (state == PlayerState.Crouching)
                {
                    // 下蹲状态：亮蓝色
                    targetColor = new Color(0.0f, 0.5f, 1.0f);
                }
                else
                {
                    // 正常奔跑状态：亮绿色
                    targetColor = new Color(0.0f, 0.9f, 0.2f);
                }
                playerMaterial.DisableKeyword("_EMISSION");
            }
        }

        playerMaterial.color = targetColor;
    }

    // ── OnGUI 屏幕辅助精美头顶标签 ──
    void OnGUI()
    {
        if (state == PlayerState.Spectating) return;
        if (Camera.main == null) return;

        // 将胶囊体头顶上方的位置转换到屏幕坐标
        Vector3 worldPos = transform.position + Vector3.up * 1.2f;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        // 确保目标在相机视野前方
        if (screenPos.z > 0)
        {
            float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
            // 视距超过 40 米则不渲染，防止 UI 元素扎堆拥挤
            if (distance > 40f) return;

            string roleTag = "";
            Color labelColor = Color.white;

            if (state == PlayerState.Caught)
            {
                roleTag = "[CAPTURED]";
                labelColor = new Color(0.7f, 0.7f, 0.7f);
            }
            else
            {
                if (role == PlayerRole.Seeker)
                {
                    roleTag = "☠️ [SEEKER]";
                    labelColor = new Color(1.0f, 0.3f, 0.1f);
                }
                else
                {
                    roleTag = (state == PlayerState.Crouching) ? "👥 [HIDER (CROUCH)]" : "👥 [HIDER]";
                    labelColor = (state == PlayerState.Crouching) ? new Color(0.2f, 0.7f, 1.0f) : new Color(0.2f, 1.0f, 0.4f);
                }
            }

            string displayText = $"{roleTag} Player {playerId} ({distance:F1}m)";

            GUIStyle labelStyle = new GUIStyle();
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontSize = 13;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.normal.textColor = labelColor;

            // 绘制精美的深黑色阴影以增强复杂背景下的对比度
            GUIStyle shadowStyle = new GUIStyle(labelStyle);
            shadowStyle.normal.textColor = Color.black;

            float labelWidth = 250f;
            float labelHeight = 25f;
            float x = screenPos.x - labelWidth / 2f;
            float y = Screen.height - screenPos.y - labelHeight;

            // 阴影
            GUI.Label(new Rect(x + 1f, y + 1f, labelWidth, labelHeight), displayText, shadowStyle);
            // 文本
            GUI.Label(new Rect(x, y, labelWidth, labelHeight), displayText, labelStyle);
        }
    }
}
