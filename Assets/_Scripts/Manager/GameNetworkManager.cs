using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Jae.Manager;
using Jae.Services;

public class GameNetworkManager : MonoBehaviour
{
    public static GameNetworkManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[GNM] NetworkManager.Singleton is null. Ensure a NetworkManager exists in the scene.");
            return;
        }
        
        NetworkManager.Singleton.ConnectionApprovalCallback = PlayerSessionManager.Instance.HandleConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += PlayerSessionManager.Instance.HandleClientDisconnected;

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
        
        if (clientId == NetworkManager.ServerClientId)
        {
            SpawnManager.Instance.SpawnInitialEnemies();
        }
        
        if (PlayerSessionManager.Instance.TryGetClientInfo(clientId, out var clientInfo))
        {
            NetworkObject playerNetworkObject = SpawnManager.Instance.SpawnPlayer(clientId, clientInfo.PlayerSpawnPosition);
            if (playerNetworkObject != null)
            {
                PlayerSessionManager.Instance.SetPlayerNetworkObject(clientId, playerNetworkObject);
            }
            else
            {
                Debug.LogError($"[GNM] SpawnManager failed to spawn player for client {clientId}.");
            }
        }
        else
        {
            if (clientId != NetworkManager.ServerClientId)
            {
                Debug.LogWarning($"[GNM] ClientInfo not found for client {clientId} in OnLoadCompleteHandler. Cannot spawn player.");
            }
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback -= PlayerSessionManager.Instance.HandleConnectionApprovalCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= PlayerSessionManager.Instance.HandleClientDisconnected;

            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnLoadCompleteHandler;
            }
        }
    }
    
    // ============================================
    //  START METHODS
    // ============================================
    public void StartHost()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[GNM] NetworkManager.Singleton is null. Cannot start host.");
            return;
        }
        if (AuthManager.Instance == null)
        {
            Debug.LogError("[GNM] AuthManager.Instance is null. Cannot start host.");
            return;
        }

        string token = AuthManager.Instance.GetCurrentToken();
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
        if (AuthManager.Instance == null)
        {
            Debug.LogError("[GNM] AuthManager.Instance is null. Cannot start client."); // Re-added check
            return;
        }

        string token = AuthManager.Instance.GetCurrentToken();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("[GNM] Cannot start client: User is not logged in (no JWT token).");
            return;
        }
        NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(token);

        if (!NetworkManager.Singleton.StartClient())
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
