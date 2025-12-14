using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System;
using Jae.Common;

namespace Jae.Manager
{
    public class SceneFlowManager : NetworkBehaviour
    {
        public static SceneFlowManager Instance { get; private set; }

        public event Action<ulong, string, LoadSceneMode> OnSceneLoadComplete;

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
                Debug.LogError("[SceneManager] NetworkManager's SceneManager is null.");
            }
        }

        public override void OnNetworkDespawn()
        {
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
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[SceneManager] NetworkManager.Singleton is null. Cannot load game scene.");
                return;
            }
            if (!NetworkManager.Singleton.IsServer)
            {
                Debug.LogError("[SceneManager] Only server can initiate scene loading.");
                return;
            }

            NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.GameScene, LoadSceneMode.Single);
            Debug.Log($"[SceneManager] Attempting to load {SceneNames.GameScene}.");
        }

        public void LoadLoginScene()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[SceneManager] NetworkManager.Singleton is null. Cannot load login scene.");
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.MainScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
                Debug.LogWarning($"[SceneManager] NetworkManager not found, falling back to regular scene load for {SceneNames.MainScene} (Login Scene).");
                return;
            }

            if (NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.MainScene, LoadSceneMode.Single);
                Debug.Log($"[SceneManager] Attempting to load {SceneNames.MainScene} (Login Scene) via NetworkSceneManager.");
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.MainScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
                Debug.Log($"[SceneManager] Attempting to load {SceneNames.MainScene} (Login Scene) via regular SceneManager.");
            }
        }
    }
}
