using System.Collections.Generic;
using Peekaboo;
using UnityEngine;

public class RemotePlayerManager : MonoBehaviour
{
    public GameObject remotePlayerPrefab;

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
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.OnPlayerStates -= OnPlayerStates;
    }

    void OnPlayerStates(PlayerStates states)
    {
        if (states?.Players == null) return;

        byte myId = NetworkManager.Instance.myPlayerId;

        foreach (var entry in states.Players)
        {
            int id = (int)entry.PlayerId;
            if (id == myId) continue;

            GameObject go;
            if (!remotePlayers.TryGetValue(id, out go) || go == null)
            {
                go = Instantiate(remotePlayerPrefab);
                go.name = $"RemotePlayer_{id}";
                go.SetActive(true);
                remotePlayers[id] = go;
            }

            go.transform.position = new Vector3(entry.PosX, 0.5f, entry.PosZ);
            go.transform.rotation = Quaternion.Euler(0f, entry.RotY, 0f);
        }
    }
}
