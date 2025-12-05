using UnityEngine;
using Unity.Netcode;
using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameNetworkManager : MonoBehaviour
{
    [Header("Assign player prefab here (optional - will also register to NetworkManager)")]
    [SerializeField] private GameObject playerPrefab;
    
    public class ClientInfo 
    {
        public string Uid;
        public string JwtToken;
        public NetworkObject PlayerNetworkObject;
    }

    private readonly Dictionary<ulong, ClientInfo> connectedClientsData = new();
    
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
        
        StartDedicatedServer();
    }
    
    public void AddPlayerToList(ulong clientId, NetworkObject networkObject)
    {
        if (NetworkManager.Singleton.IsServer && connectedClientsData.TryGetValue(clientId, out ClientInfo info))
        {
            info.PlayerNetworkObject = networkObject;
            Debug.Log($"[GNM] Added player NetworkObject for client {clientId} (UID: {info.Uid}).");
        }
    }

    public void RemovePlayerFromList(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer && connectedClientsData.ContainsKey(clientId))
        {
            connectedClientsData.Remove(clientId);
            Debug.Log($"[GNM] Removed client {clientId} from connectedClientsData.");
        }
    }

    private async void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = false;
        response.CreatePlayerObject = false;
        response.Pending = true;

        string jwtToken = "";
        try
        {
            if (request.Payload != null && request.Payload.Length > 0)
            {
                jwtToken = Encoding.UTF8.GetString(request.Payload);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Approval] Failed to parse payload as JWT: {ex}");
            response.Reason = "Invalid payload format.";
            response.Pending = false;
            return;
        }

        if (string.IsNullOrEmpty(jwtToken))
        {
            Debug.LogWarning("[Approval] JWT token not provided in payload.");
            response.Reason = "Authentication token required.";
            response.Pending = false;
            return;
        }

        if (ServerAuthService.Instance == null)
        {
            Debug.LogError("[Approval] ServerAuthService.Instance is null. Is it in the scene?");
            response.Reason = "Server authentication service unavailable.";
            response.Pending = false;
            return;
        }

        AuthValidationResult validationResult = await ServerAuthService.Instance.ValidateTokenAsync(jwtToken);
        
        string uid = "unknown";
        if (validationResult.IsValid)
        {
            uid = validationResult.UserId;
            Debug.Log($"[Approval] Token validated for user: {uid}");
            
            // --- Player Data Loading ---
            if (PlayerServerDataService.Instance == null)
            {
                Debug.LogError("[Approval] PlayerServerDataService.Instance is null. Is it in the scene?");
                response.Reason = "Player data service unavailable.";
                response.Pending = false;
                return;
            }

            PlayerData loadedPlayerData = await PlayerServerDataService.Instance.LoadPlayerDataAsync(jwtToken);
            Vector3 spawnPos = Vector3.zero;

            if (loadedPlayerData != null && loadedPlayerData.position != null)
            {
                spawnPos = loadedPlayerData.position.ToVector3();
                Debug.Log($"[Approval] Loaded player data for {uid} at {spawnPos}");
            }
            else
            {
                Debug.Log($"[Approval] No saved player data found or data is invalid for {uid}, spawning at default.");
            }
            // --- End Player Data Loading ---

            response.Approved = true;
            response.CreatePlayerObject = true;
            response.Position = spawnPos;
            response.Rotation = Quaternion.identity;
            
            connectedClientsData[request.ClientNetworkId] = new ClientInfo { Uid = uid, JwtToken = jwtToken };
            Debug.Log($"[Approval] Client {request.ClientNetworkId} (UID: {uid}) approved.");
        }
        else
        {
            Debug.LogWarning($"[Approval] JWT token validation failed for client {request.ClientNetworkId}: {validationResult.ErrorMessage}");
            response.Reason = $"Authentication failed: {validationResult.ErrorMessage}";
        }
        
        response.Pending = false;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[GNM] Client connected: {clientId}");
    }

    private async void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[GNM] Client disconnected: {clientId}");
        
        if (!NetworkManager.Singleton.IsServer)
        {
            connectedClientsData.Remove(clientId);
            return;
        }
        
        if (connectedClientsData.TryGetValue(clientId, out ClientInfo clientInfo))
        {
            if (clientInfo.PlayerNetworkObject != null)
            {
                Vector3 pos = clientInfo.PlayerNetworkObject.transform.position;
                string jwtToken = clientInfo.JwtToken;
                string uid = clientInfo.Uid;

                // --- Player Data Saving ---
                if (PlayerServerDataService.Instance == null)
                {
                    Debug.LogError($"[GNM] PlayerServerDataService.Instance is null on disconnect for client {clientId}. Cannot save data.");
                    connectedClientsData.Remove(clientId);
                    return;
                }
                
                PlayerData dataToSave = new PlayerData(pos);
                
                bool saveSuccess = await PlayerServerDataService.Instance.SavePlayerDataAsync(jwtToken, dataToSave);
                if (saveSuccess)
                {
                    Debug.Log($"[GNM] Saved data for uid {uid} on disconnect at {pos}.");
                }
                else
                {
                    Debug.LogWarning($"[GNM] Failed to save data for uid {uid} on disconnect.");
                }
                // --- End Player Data Saving ---
            }
            else
            {
                Debug.LogWarning($"[GNM] Player NetworkObject not found for client {clientId}. Cannot save position.");
            }
            connectedClientsData.Remove(clientId);
        }
        else
        {
            Debug.LogWarning($"[GNM] ClientInfo not found for client {clientId} on disconnect.");
        }
    }

    public bool TryGetClientInfo(ulong clientId, out ClientInfo clientInfo)
    {
        clientInfo = null; // Initialize out parameter
        return connectedClientsData.TryGetValue(clientId, out clientInfo);
    }
    
    public void StartHost()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[GNM] NetworkManager.Singleton is null. Cannot start host.");
            return;
        }
        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("[GNM] Host started successfully.");
        }
        else
        {
            Debug.LogError("[GNM] Failed to start host.");
        }
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[GNM] NetworkManager.Singleton is null. Cannot start client.");
            return;
        }
        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("[GNM] Client started successfully.");
        }
        else
        {
            Debug.LogError("[GNM] Failed to start client.");
        }
    }

    public void StartServer()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[GNM] NetworkManager.Singleton is null. Cannot start server.");
            return;
        }
        if (NetworkManager.Singleton.StartServer())
        {
            Debug.Log("[GNM] Server started successfully.");
        }
        else
        {
            Debug.LogError("[GNM] Failed to start server.");
        }
    }
    
    public void StartDedicatedServer()
    {
#if UNITY_SERVER
        if (!Application.isEditor)
        {
            Debug.Log("--- DEDICATED SERVER STARTING ---");

            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[GNM] NetworkManager.Singleton is null. Cannot start dedicated server.");
                return;
            }
            
            NetworkManager.Singleton.StartServer();
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }
#endif
    }
}