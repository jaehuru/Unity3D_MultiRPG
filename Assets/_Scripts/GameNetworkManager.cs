using UnityEngine;
using Unity.Netcode;
using System;
using System.Text;
using System.Collections.Generic;

public class GameNetworkManager : MonoBehaviour
{
    [Header("Assign player prefab here (optional - will also register to NetworkManager)")]
    [SerializeField] private GameObject playerPrefab;

    private Dictionary<ulong, string> connectedAccounts = new Dictionary<ulong, string>();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[GNM] NetworkManager.Singleton is null. Ensure a NetworkManager exists in the scene.");
            return;
        }

        NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        string uid = "unknown";
        try
        {
            if (request.Payload != null && request.Payload.Length > 0)
                uid = Encoding.UTF8.GetString(request.Payload);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Approval] Failed parse payload: {ex}");
            uid = "unknown";
        }

        Vector3 spawnPos = Vector3.zero;

        if (PlayerDataService.Instance != null)
        {
            PlayerSaveData pd = PlayerDataService.Instance.Load(uid);
            if (pd != null)
            {
                spawnPos = pd.GetPosition();
                Debug.Log($"[Approval] Loaded {uid} at {spawnPos}");
            }
            else
            {
                Debug.Log($"[Approval] No saved data for {uid}, spawn default");
            }
        }
        else
        {
            Debug.LogError("[Approval] PlayerDataService not found in scene!");
        }

        response.Approved = true;
        response.CreatePlayerObject = true;
        response.Position = spawnPos;
        response.Rotation = Quaternion.identity;
        response.Pending = false;
        
        connectedAccounts[request.ClientNetworkId] = uid;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[GNM] Client connected: {clientId}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[GNM] Client disconnected: {clientId}");

        if (!connectedAccounts.TryGetValue(clientId, out string uid))
        {
            Debug.LogWarning($"[GNM] No uid mapping for client {clientId}");
            return;
        }

        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[GNM] OnClientDisconnected should be executed on server.");
            connectedAccounts.Remove(clientId);
            return;
        }

        // 서버에서 플레이어 NetworkObject 가져오기
        NetworkObject playerNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);

        if (playerNetworkObject != null)
        {
            Vector3 pos = playerNetworkObject.transform.position;

            // 데이터 저장
            PlayerSaveData dataToSave = new PlayerSaveData(uid, pos);
            PlayerDataService.Instance.Save(dataToSave);
            Debug.Log($"[GNM] Saved data for uid {uid} on disconnect.");

            // NetworkObject despawn
            playerNetworkObject.Despawn(true); // true면 서버에서 강제 despawn
            Debug.Log($"[GNM] Player NetworkObject for client {clientId} despawned.");
        }
        else
        {
            Debug.LogWarning($"[GNM] Player NetworkObject not found for client {clientId}");
        }

        // 클라이언트-UID 매핑 제거
        connectedAccounts.Remove(clientId);
    }

    public bool TryGetAccountId(ulong clientId, out string uid)
    {
        return connectedAccounts.TryGetValue(clientId, out uid);
    }

    private void OnApplicationQuit()
    {
        if (PlayerDataService.Instance != null)
        {
            PlayerDataService.Instance.FlushAllToDisk();
        }
    }
}
