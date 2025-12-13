using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Jae.Manager
{
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
            SceneManager.LoadScene("MainScene");
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
            Application.Quit();
#endif
        }
    }
}