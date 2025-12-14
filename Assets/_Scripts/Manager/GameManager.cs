using Unity.Netcode;
using System.Collections;

namespace Jae.Manager
{
    // 게임 진행 로직 담당
    // ex) Stage, 보스, 퀘스트, 경험치, 레벨 관리, UI 이벤트 처리
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }

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
            // This is a global manager, it should persist but only execute logic on the appropriate side.
        }
        
        public override void OnNetworkDespawn()
        {
            // Cleanup server-side subscriptions if any
        }

        public void RequestLogout()
        {
            StartCoroutine(LogoutCoroutine());
        }

        private IEnumerator LogoutCoroutine()
        {
            // 1. Shutdown the network connection
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
            
            // 2. Clear authentication token
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.ClearStoredToken();
            }

            // 3. Wait a frame for shutdown processes to complete
            yield return null;

            // 4. Load the main menu/login scene
            SceneFlowManager.Instance.LoadLoginScene();
        }

        public void QuitApplication()
        {
            // If in a network session, shut it down first
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}