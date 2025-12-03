using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerManager : MonoBehaviour
{
    void Start()
    {
#if UNITY_SERVER
        if (!Application.isEditor)
        {
            Debug.Log("--- DEDICATED SERVER STARTING ---");

            NetworkManager.Singleton.StartServer();

            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }
#endif
    }
}
