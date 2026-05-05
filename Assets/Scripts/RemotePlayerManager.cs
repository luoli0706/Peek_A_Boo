using System.Collections.Generic;
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
            // Create a default capsule prefab
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

    void OnPlayerStates(RemotePlayerState[] states)
    {
        if (states == null) return;

        byte myId = NetworkManager.Instance.myPlayerId;

        foreach (var s in states)
        {
            // Skip local player (we control our own position)
            if (s.playerId == myId) continue;

            GameObject go;
            if (!remotePlayers.TryGetValue(s.playerId, out go) || go == null)
            {
                go = Instantiate(remotePlayerPrefab);
                go.name = $"RemotePlayer_{s.playerId}";
                go.SetActive(true);
                remotePlayers[s.playerId] = go;
            }

            go.transform.position = new Vector3(s.posX, 0.5f, s.posZ);
            go.transform.rotation = Quaternion.Euler(0f, s.rotY, 0f);
        }
    }
}
