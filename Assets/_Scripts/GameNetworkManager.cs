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
        public Vector3 PlayerSpawnPosition;
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
        
#if UNITY_SERVER
        if (!Application.isEditor)
        {
            StartServer();
        }
#endif
    }
    
    private void OnLoadCompleteHandler(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (!NetworkManager.Singleton.IsServer) return;

                
        if (connectedClientsData.TryGetValue(clientId, out ClientInfo clientInfo))
        {
            if (clientInfo.PlayerNetworkObject != null)
            {
                Debug.LogWarning($"[GNM] Client {clientId} (UID: {clientInfo.Uid}) already has a player object. Skipping manual spawn.");
                return;
            }

            if (playerPrefab == null)
            {
                Debug.LogError($"[GNM] Player prefab is not assigned. Cannot spawn player for client {clientId}.");
                return;
            }
            
            GameObject playerInstance = Instantiate(playerPrefab, clientInfo.PlayerSpawnPosition, Quaternion.identity);
            NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                Debug.LogError($"[GNM] Player prefab does not have a NetworkObject component. Cannot spawn for client {clientId}.");
                Destroy(playerInstance);
                return;
            }

            networkObject.SpawnAsPlayerObject(clientId, true);
            clientInfo.PlayerNetworkObject = networkObject;

        }
        else
        {
            Debug.LogWarning($"[GNM] ClientInfo not found for client {clientId} in OnLoadCompleteHandler. Cannot spawn player.");
        }
    }
    
    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnLoadCompleteHandler;
        }
    }
    
    public void AddPlayerToList(ulong clientId, NetworkObject networkObject)
    {
        if (NetworkManager.Singleton.IsServer && connectedClientsData.TryGetValue(clientId, out ClientInfo info))
        {
            info.PlayerNetworkObject = networkObject;

        }
    }

    public void RemovePlayerFromList(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer && connectedClientsData.ContainsKey(clientId))
        {
            connectedClientsData.Remove(clientId);

        }
    }

    private async void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = false;
        response.CreatePlayerObject = false;
        response.Pending = true;

        string jwtToken = "";
        
        if (request.ClientNetworkId == NetworkManager.ServerClientId && NetworkManager.Singleton.IsHost)
        {
            jwtToken = AuthService.Instance.GetCurrentToken();
        }
        else
        {
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

            
            // --- NEW CHECK: Prevent multiple logins for the same user ID ---
            // Check if this UID is already connected
            foreach (var clientEntry in connectedClientsData.Values)
            {
                if (clientEntry.Uid == uid)
                {
                    Debug.LogWarning($"[Approval] User {uid} (client {request.ClientNetworkId}) is already connected. Blocking new connection.");
                    response.Approved = false;
                    response.Reason = "User already connected.";
                    response.Pending = false;
                    return;
                }
            }
            // --- END NEW CHECK ---
            
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
    
            }
            else
            {
    
            }
            // --- End Player Data Loading ---

            response.Approved = true;
            response.CreatePlayerObject = false;
            response.Position = spawnPos;
            response.Rotation = Quaternion.identity;
            
            connectedClientsData[request.ClientNetworkId] = new ClientInfo { Uid = uid, JwtToken = jwtToken, PlayerSpawnPosition = spawnPos };

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

        
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }
    }
    
    private async void OnClientDisconnected(ulong clientId)
    {

        
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
        clientInfo = null;
        return connectedClientsData.TryGetValue(clientId, out clientInfo);
    }
    
    // ============================================================
    //  START METHODS
    // ============================================================
    public void StartHost()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[GNM] NetworkManager.Singleton is null. Cannot start host.");
            return;
        }
        if (AuthService.Instance == null)
        {
            Debug.LogError("[GNM] AuthService.Instance is null. Cannot start host.");
            return;
        }

        string token = AuthService.Instance.GetCurrentToken();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("[GNM] Cannot start host: User is not logged in (no JWT token).");
            return;
        }
        NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(token);
        
        if (NetworkManager.Singleton.StartHost())
        {
    
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoadCompleteHandler;
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
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
        if (AuthService.Instance == null)
        {
            Debug.LogError("[GNM] AuthService.Instance is null. Cannot start client.");
            return;
        }

        string token = AuthService.Instance.GetCurrentToken();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("[GNM] Cannot start client: User is not logged in (no JWT token).");
            return;
        }
        NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(token);
        
        if (NetworkManager.Singleton.StartClient())
        {
    
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
        
        NetworkManager.Singleton.StartServer();
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoadCompleteHandler;
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }
}