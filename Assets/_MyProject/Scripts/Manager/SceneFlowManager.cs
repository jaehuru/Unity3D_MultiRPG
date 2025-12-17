using System;
// Unity
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
// Project
using Jae.Common;

namespace Jae.Manager
{
    public class SceneFlowManager : NetworkBehaviour
    {
        public static SceneFlowManager Instance { get; private set; }

        public event Action<ulong, string, LoadSceneMode> OnSceneLoadComplete;

        // --- 성능 최적화: 인스턴스 캐싱 (NetworkManager는 캐싱하지 않음) ---
        // private NetworkManager _networkManager; // 필드 제거됨

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Persist across scenes
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (NetworkManager.Singleton == null)
            {
                // 서버 운영에 필수적인 로그이므로 유지
                Debug.LogError("[SceneManager] NetworkManager.Singleton is null. Ensure a NetworkManager exists in the scene.");
                return;
            }

            // Subscribe to NetworkManager's scene load complete event
            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadComplete += HandleNetworkSceneLoadComplete;
            }
            else
            {
                // 서버 운영에 필수적인 로그이므로 유지
                Debug.LogError("[SceneManager] NetworkManager's SceneManager is null.");
            }
        }

        public override void OnNetworkDespawn()
        {
            // NetworkManager.Singleton에 직접 접근
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadComplete -= HandleNetworkSceneLoadComplete;
            }
            base.OnNetworkDespawn();
        }

        private void HandleNetworkSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            OnSceneLoadComplete?.Invoke(clientId, sceneName, loadSceneMode);
        }


        public void LoadGameScene()
        {
            // NetworkManager.Singleton에 직접 접근
            if (NetworkManager.Singleton == null)
            {
                // 서버 운영에 필수적인 로그이므로 유지
                Debug.LogError("[SceneManager] NetworkManager.Singleton is null. Cannot load game scene.");
                return;
            }
            if (!NetworkManager.Singleton.IsServer)
            {
                // 서버 운영에 필수적인 로그이므로 유지
                Debug.LogError("[SceneManager] Only server can initiate scene loading.");
                return;
            }

            NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.GameScene, LoadSceneMode.Single);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[SceneManager] Attempting to load {SceneNames.GameScene}.");
#endif
        }


        public void LoadLoginScene()
        {
            // NetworkManager.Singleton에 직접 접근
            if (NetworkManager.Singleton == null)
            {
                // 서버 운영에 필수적인 로그이므로 유지
                Debug.LogError("[SceneManager] NetworkManager.Singleton is null. Cannot load login scene.");
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.MainScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogWarning($"[SceneManager] NetworkManager not found, falling back to regular scene load for {SceneNames.MainScene} (Login Scene).");
#endif
                return;
            }

            if (NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.MainScene, LoadSceneMode.Single);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log($"[SceneManager] Attempting to load {SceneNames.MainScene} (Login Scene) via NetworkSceneManager.");
#endif
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.MainScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log($"[SceneManager] Attempting to load {SceneNames.MainScene} (Login Scene) via regular SceneManager.");
#endif
            }
        }
    }
}
