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

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (NetworkManager.Singleton == null)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogError("[SceneManager] NetworkManager.Singleton is null. Ensure a NetworkManager exists in the scene.");
#endif
                return;
            }
            
            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadComplete += HandleNetworkSceneLoadComplete;
            }
            else
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogError("[SceneManager] NetworkManager's SceneManager is null.");
#endif
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
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogError("[SceneManager] NetworkManager.Singleton is null. Cannot load game scene.");
#endif
                return;
            }
            if (!NetworkManager.Singleton.IsServer)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogError("[SceneManager] Only server can initiate scene loading.");
#endif
                return;
            }

            NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.GameScene, LoadSceneMode.Single);
            Debug.Log($"[SceneManager] Attempting to load {SceneNames.GameScene}.");
        }


        public void LoadLoginScene()
        {
            if (NetworkManager.Singleton == null)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogError("[SceneManager] NetworkManager.Singleton is null. Cannot load login scene.");
#endif
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