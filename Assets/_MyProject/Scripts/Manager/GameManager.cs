using System.Collections;
//Unity
using Unity.Netcode;

namespace Jae.Manager
{
    // 게임 진행 로직 담당
    // ex) Stage, 보스, 퀘스트, 경험치, 레벨 관리, UI 이벤트 처리
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        // --- 성능 최적화: 인스턴스 캐싱 (가이드라인에 따라 제거됨) ---
        // private SceneFlowManager _sceneFlowManager;
        // private AuthManager _authManager;
        // private NetworkManager _networkManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 매니저 참조 캐싱 제거 (가이드라인에 따라)
            // _sceneFlowManager = SceneFlowManager.Instance;
            // _authManager = AuthManager.Instance;
            // _networkManager = NetworkManager.Singleton;
        }



        public void RequestLogout()
        {
            StartCoroutine(LogoutCoroutine());
        }

        private IEnumerator LogoutCoroutine()
        {
            // 1. Shutdown the network connection
            // 캐싱 제거 후 NetworkManager.Singleton에 직접 접근
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
            
            // 2. Clear authentication token
            // 캐싱 제거 후 AuthManager.Instance에 직접 접근
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.ClearStoredToken();
            }

            // 3. Wait a frame for shutdown processes to complete
            yield return null;

            // 4. Load the main menu/login scene
            // 캐싱 제거 후 SceneFlowManager.Instance에 직접 접근
            SceneFlowManager.Instance?.LoadLoginScene();
        }

        public void QuitApplication()
        {
            // If in a network session, shut it down first
            // 캐싱 제거 후 NetworkManager.Singleton에 직접 접근
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