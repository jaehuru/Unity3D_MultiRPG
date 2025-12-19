using System.Collections;
//Unity
using Unity.Netcode;
// Project
using Jae.Application;

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
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }



        public void RequestLogout()
        {
            StartCoroutine(LogoutCoroutine());
        }

        private IEnumerator LogoutCoroutine()
        {
            if (SceneFlowManager.Instance != null && NetworkGameOrchestrator.Instance != null)
            {
                SceneFlowManager.Instance.OnSceneLoadComplete -= NetworkGameOrchestrator.Instance.OnLoadCompleteWrapper;
            }
            
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
            
            if (PlayerSessionManager.Instance != null)
            {
                PlayerSessionManager.Instance.ClearSessionData();
            }
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ClearRegisteredWorldSpaceCanvases();
            }
            
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.ClearStoredToken();
            }
            
            yield return null;
            
            SceneFlowManager.Instance?.LoadLoginScene();        
        }

        public void QuitApplication()
        {
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
