using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button connectButton;

    void Start()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        connectButton.onClick.AddListener(OnConnectClicked);
    }

    private void OnHostClicked()
    {
        PlayerPrefs.SetString("Nickname", nicknameInput.text);
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private void OnConnectClicked()
    {
        PlayerPrefs.SetString("Nickname", nicknameInput.text);
        NetworkManager.Singleton.StartClient();
    }
}
