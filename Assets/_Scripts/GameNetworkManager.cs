using UnityEngine;
using Unity.Netcode;
using System;
using System.Text;
using System.Collections.Generic;

public class GameNetworkManager : MonoBehaviour
{
    [Header("Assign player prefab here (optional - will also register to NetworkManager)")]
    [SerializeField] private GameObject playerPrefab;

    private readonly Dictionary<ulong, string> connectedAccounts = new();
    private readonly Dictionary<ulong, NetworkObject> _playerObjects = new();

    public static GameNetworkManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
    
    public void AddPlayerToList(ulong clientId, NetworkObject networkObject)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            _playerObjects[clientId] = networkObject;
        }
    }

    public void RemovePlayerFromList(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer && _playerObjects.ContainsKey(clientId))
        {
            _playerObjects.Remove(clientId);
        }
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
        
        if (!NetworkManager.Singleton.IsServer)
        {
            connectedAccounts.Remove(clientId);
            return;
        }
        
        if (_playerObjects.TryGetValue(clientId, out NetworkObject playerNetworkObject) && playerNetworkObject != null)
        {
            if (connectedAccounts.TryGetValue(clientId, out string uid))
            {
                Vector3 pos = playerNetworkObject.transform.position;

                PlayerSaveData dataToSave = new PlayerSaveData(uid, pos);
                PlayerDataService.Instance.Save(dataToSave);
                Debug.Log($"[GNM] Saved data for uid {uid} on disconnect.");
            }
            else
            {
                Debug.LogWarning($"[GNM] No uid mapping for client {clientId} when saving data.");
            }
        }
        else
        {
            Debug.LogWarning($"[GNM] Player NetworkObject not found in managed list for client {clientId}. Cannot save data.");
        }
        
        _playerObjects.Remove(clientId);
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
