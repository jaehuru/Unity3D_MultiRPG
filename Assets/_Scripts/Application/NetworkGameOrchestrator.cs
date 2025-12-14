using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Jae.Manager;

namespace Jae.Application
{
    public class NetworkGameOrchestrator : MonoBehaviour
    {
        public static NetworkGameOrchestrator Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        void Start()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[NetworkGameOrchestrator] NetworkManager.Singleton is null. Ensure a NetworkManager exists in the scene.");
                return;
            }
            
            NetworkManager.Singleton.ConnectionApprovalCallback = PlayerSessionManager.Instance.HandleConnectionApprovalCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += PlayerSessionManager.Instance.HandleClientDisconnected;

#if UNITY_SERVER
            if (!UnityEngine.Application.isEditor)
            {
                StartServer();
            }
#endif
        }
        
        private void OnLoadCompleteHandler(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            Debug.Log($"[NetworkGameOrchestrator] OnLoadCompleteHandler Called for Client: {clientId}, Scene: {sceneName}");

            if (!NetworkManager.Singleton.IsServer) return;
            
            if (clientId == NetworkManager.ServerClientId)
            {
                SpawnManager.Instance.SpawnInitialEnemies();
            }
            
            if (PlayerSessionManager.Instance.TryGetClientInfo(clientId, out var clientInfo))
            {
                Debug.Log($"[NetworkGameOrchestrator] ClientInfo found for {clientId}. Attempting to spawn player.");
                NetworkObject playerNetworkObject = SpawnManager.Instance.SpawnPlayer(clientId, clientInfo.PlayerSpawnPosition);
                if (playerNetworkObject != null)
                {
                    Debug.Log($"[NetworkGameOrchestrator] Player spawned successfully for {clientId}. NetworkObjectId: {playerNetworkObject.NetworkObjectId}");
                    PlayerSessionManager.Instance.SetPlayerNetworkObject(clientId, playerNetworkObject);
                }
                else
                {
                    Debug.LogError($"[NetworkGameOrchestrator] SpawnManager failed to spawn player for client {clientId}. (Returned null NetworkObject)");
                }
            }
            else
            {
                if (clientId != NetworkManager.ServerClientId)
                {
                    Debug.LogWarning($"[NetworkGameOrchestrator] ClientInfo not found for client {clientId} in OnLoadCompleteHandler. Cannot spawn player.");
                }
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.ConnectionApprovalCallback -= PlayerSessionManager.Instance.HandleConnectionApprovalCallback;
                NetworkManager.Singleton.OnClientDisconnectCallback -= PlayerSessionManager.Instance.HandleClientDisconnected;

                if (SceneFlowManager.Instance != null) 
                {
                    SceneFlowManager.Instance.OnSceneLoadComplete -= OnLoadCompleteHandler;
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
                Debug.LogError("[NetworkGameOrchestrator] NetworkManager.Singleton is null. Cannot start host.");
                return;
            }
            if (AuthManager.Instance == null)
            {
                Debug.LogError("[NetworkGameOrchestrator] AuthManager.Instance is null. Cannot start host.");
                return;
            }

            string token = AuthManager.Instance.GetCurrentToken();
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("[NetworkGameOrchestrator] Cannot start host: User is not logged in (no JWT token).");
                return;
            }
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(token);

            if (NetworkManager.Singleton.StartHost())
            {
                // Manually add ClientInfo for the host client (Client ID 0)
                ulong hostClientId = NetworkManager.ServerClientId; // This is always 0
                string hostUid = AuthManager.Instance.GetUserId(); // Get User ID from AuthManager
                string hostToken = AuthManager.Instance.GetCurrentToken(); // Get JWT token from AuthManager

                if (PlayerSessionManager.Instance != null && !PlayerSessionManager.Instance.TryGetClientInfo(hostClientId, out _))
                {
                    // For PlayerSpawnPosition, use Vector3.zero as a default. Actual spawn position will be set by SpawnManager.
                    PlayerSessionManager.Instance.AddClientInfo(hostClientId, hostUid, hostToken, Vector3.zero);
                    Debug.Log($"[NetworkGameOrchestrator] Host ClientInfo manually added for Client: {hostClientId}");
                }
                else
                {
                    Debug.LogError("[NetworkGameOrchestrator] PlayerSessionManager not found or host ClientInfo already exists.");
                }

                if (SceneFlowManager.Instance != null)
                {
                    
                    SceneFlowManager.Instance.OnSceneLoadComplete += OnLoadCompleteHandler;
                }
                else
                {
                    Debug.LogError("[NetworkGameOrchestrator] SceneManager.Instance is null. OnLoadCompleteHandler will not be subscribed.");
                }

                SceneFlowManager.Instance.LoadGameScene();
            }
            else
            {
                Debug.LogError("[NetworkGameOrchestrator] Failed to start host.");
            }
        }

        public void StartClient()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[NetworkGameOrchestrator] NetworkManager.Singleton is null. Cannot start client.");
                return;
            }
            if (AuthManager.Instance == null)
            {
                Debug.LogError("[NetworkGameOrchestrator] AuthManager.Instance is null. Cannot start client.");
                return;
            }

            string token = AuthManager.Instance.GetCurrentToken();
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("[NetworkGameOrchestrator] Cannot start client: User is not logged in (no JWT token).");
                return;
            }
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(token);

            if (!NetworkManager.Singleton.StartClient())
            {
                Debug.LogError("[NetworkGameOrchestrator] Failed to start client.");
            }
        }

        public void StartServer()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[NetworkGameOrchestrator] NetworkManager.Singleton is null. Cannot start server.");
                return;
            }

            NetworkManager.Singleton.StartServer();
            if (SceneFlowManager.Instance != null)
            {
                SceneFlowManager.Instance.OnSceneLoadComplete += OnLoadCompleteHandler;
            }
            else
            {
                Debug.LogError("[NetworkGameOrchestrator] SceneManager.Instance is null. OnLoadCompleteHandler will not be subscribed.");
            }
            SceneFlowManager.Instance.LoadGameScene();
        }
    }
}
