using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Jae.Manager;
using System.Collections;

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

        private void OnLoadCompleteWrapper(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            StartCoroutine(OnLoadCompleteCoroutine(clientId, sceneName, loadSceneMode));
        }
        
        private IEnumerator OnLoadCompleteCoroutine(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            yield return null;

            if (!NetworkManager.Singleton.IsServer) yield break;
            
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
                    Debug.LogError($"[NetworkGameOrchestrator] SpawnManager failed to spawn player for client {clientId}.");
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
                    SceneFlowManager.Instance.OnSceneLoadComplete -= OnLoadCompleteWrapper;
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
                ulong hostClientId = NetworkManager.ServerClientId;
                string hostUid = AuthManager.Instance.GetUserId();
                string hostToken = AuthManager.Instance.GetCurrentToken();

                if (PlayerSessionManager.Instance != null && !PlayerSessionManager.Instance.TryGetClientInfo(hostClientId, out _))
                {
                    PlayerSessionManager.Instance.AddClientInfo(hostClientId, hostUid, hostToken, Vector3.zero);
                }
                else
                {
                    Debug.LogError("[NetworkGameOrchestrator] PlayerSessionManager not found or host ClientInfo already exists.");
                }

                if (SceneFlowManager.Instance != null)
                {
                    
                    SceneFlowManager.Instance.OnSceneLoadComplete += OnLoadCompleteWrapper;
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
                SceneFlowManager.Instance.OnSceneLoadComplete += OnLoadCompleteWrapper;
            }
            else
            {
                Debug.LogError("[NetworkGameOrchestrator] SceneManager.Instance is null. OnLoadCompleteHandler will not be subscribed.");
            }
            SceneFlowManager.Instance.LoadGameScene();
        }
    }
}
