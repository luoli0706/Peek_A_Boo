using System.Collections.Generic;
using Peekaboo;
using UnityEngine;

public class RemotePlayerManager : MonoBehaviour
{
    public GameObject remotePlayerPrefab;

    [Header("Testing & Debugging")]
    [Tooltip("若勾选，将在单机播放时在 Seeker 出生点前方生成一个测试假人(RemotePlayer_99)，以便在无网络/单人状态下直接测试 F 键交互")]
    public bool spawnTestDummy = true;

    private Dictionary<int, GameObject> remotePlayers = new Dictionary<int, GameObject>();

    void Start()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.OnPlayerStates += OnPlayerStates;

        if (remotePlayerPrefab == null)
        {
            remotePlayerPrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            remotePlayerPrefab.name = "RemotePlayer_Default";
            remotePlayerPrefab.SetActive(false);
        }

        // 本地调试伪装测试人机制：使得没有其他玩家连入时仍能零成本进行 F 交互捕获测试
        if (spawnTestDummy)
        {
            SpawnLocalTestDummy();
        }
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.OnPlayerStates -= OnPlayerStates;
    }

    private void SpawnLocalTestDummy()
    {
        // 放置在 Seeker 出生点(约 -23, 1, 0)前方约 2 米处，方便按下 Play 即可直接面对
        Vector3 dummyPos = new Vector3(-21f, 1f, 0.5f);
        GameObject dummy = Instantiate(remotePlayerPrefab);
        dummy.name = "RemotePlayer_99";
        dummy.transform.position = dummyPos;
        dummy.SetActive(true);

        // 挂载 RemotePlayer 组件并将其身份设为 Hider (Id = 99)
        RemotePlayer rp = dummy.GetComponent<RemotePlayer>();
        if (rp == null)
        {
            rp = dummy.AddComponent<RemotePlayer>();
        }
        rp.Setup(99);

        // 模拟一个默认状态
        PlayerStateEntry entry = new PlayerStateEntry
        {
            PlayerId = 99,
            State = PlayerState.Normal,
            PosX = dummyPos.x,
            PosZ = dummyPos.z,
            RotY = 180f
        };
        rp.UpdateState(entry);

        Debug.Log("[RemotePlayerManager] Spawned local Hider test dummy at " + dummyPos + " (Name: RemotePlayer_99) for quick F interact validation.");
    }

    /// <summary>
    /// 统计当前场上存活的 Hider（包括测试假人 RemotePlayer_99）数量。
    /// 一旦存活 Hider 数量归零，即触发捕获局终结算。
    /// </summary>
    public int GetActiveHiderCount()
    {
        int count = 0;

        // 1. 优先检查本地测试假人 RemotePlayer_99 的存活状态
        if (spawnTestDummy)
        {
            GameObject dummyGo = GameObject.Find("RemotePlayer_99");
            if (dummyGo != null)
            {
                RemotePlayer rp = dummyGo.GetComponent<RemotePlayer>();
                if (rp != null && rp.state != PlayerState.Caught && rp.state != PlayerState.Spectating)
                {
                    count++;
                }
            }
        }

        // 2. 统计其它联机远程 Hider 玩家的存活状态
        foreach (var kv in remotePlayers)
        {
            if (kv.Value == null) continue;
            RemotePlayer rp = kv.Value.GetComponent<RemotePlayer>();
            if (rp != null && rp.role == PlayerRole.Hider)
            {
                if (rp.state != PlayerState.Caught && rp.state != PlayerState.Spectating)
                {
                    count++;
                }
            }
        }

        return count;
    }

    void OnPlayerStates(PlayerStates states)
    {
        if (states?.Players == null) return;

        byte myId = NetworkManager.Instance != null ? NetworkManager.Instance.myPlayerId : (byte)255;

        foreach (var entry in states.Players)
        {
            int id = (int)entry.PlayerId;
            if (id == myId) continue;

            // 过滤和覆盖测试 Dummy 的 Id，避免冲突
            if (id == 99 && spawnTestDummy) continue;

            GameObject go;
            if (!remotePlayers.TryGetValue(id, out go) || go == null)
            {
                go = Instantiate(remotePlayerPrefab);
                go.name = $"RemotePlayer_{id}";
                go.SetActive(true);
                remotePlayers[id] = go;
            }

            // 获取或添加 RemotePlayer 组件来做平滑处理和视觉渲染
            RemotePlayer rp = go.GetComponent<RemotePlayer>();
            if (rp == null)
            {
                rp = go.AddComponent<RemotePlayer>();
            }

            // 初始化身份
            if (rp.playerId == -1)
            {
                rp.Setup(id);
            }

            // 更新状态
            rp.UpdateState(entry);
        }
    }
}
