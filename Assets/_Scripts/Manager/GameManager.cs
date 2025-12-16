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
        
        private SceneFlowManager _sceneFlowManager;
        private AuthManager _authManager;
        private NetworkManager _networkManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            _sceneFlowManager = SceneFlowManager.Instance;
            _authManager = AuthManager.Instance;
            _networkManager = NetworkManager.Singleton;
        }



        public void RequestLogout()
        {
            StartCoroutine(LogoutCoroutine());
        }

        private IEnumerator LogoutCoroutine()
        {
            // 1. Shutdown the network connection
            if (_networkManager != null)
            {
                _networkManager.Shutdown();
            }
            
            // 2. Clear authentication token
            if (_authManager != null)
            {
                _authManager.ClearStoredToken();
            }

            // 3. Wait a frame for shutdown processes to complete
            yield return null;

            // 4. Load the main menu/login scene
            _sceneFlowManager?.LoadLoginScene();
        }

        public void QuitApplication()
        {
            // If in a network session, shut it down first
            if (_networkManager != null && _networkManager.IsListening)
            {
                _networkManager.Shutdown();
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}
