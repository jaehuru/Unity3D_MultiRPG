using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Text;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField uidInput;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button connectButton;

    void Start()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        connectButton.onClick.AddListener(OnConnectClicked);
    }

    private void OnHostClicked()
    {
        string uid = uidInput.text.Trim();
        if (string.IsNullOrEmpty(uid)) { Debug.LogWarning("UID empty"); return; }
        
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(uid);
        NetworkManager.Singleton.StartHost();
        Debug.Log($"[UI] StartHost with UID: {uid}");
        
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private void OnConnectClicked()
    {
        string uid = uidInput.text.Trim();
        if (string.IsNullOrEmpty(uid)) { Debug.LogWarning("UID empty"); return; }

        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(uid);
        NetworkManager.Singleton.StartClient();
        Debug.Log($"[UI] StartClient with UID: {uid}");
    }
}
