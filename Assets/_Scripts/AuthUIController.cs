using UnityEngine;
using TMPro; 
using Unity.Netcode; 
using UnityEngine.UI; 

public class AuthUIController : MonoBehaviour
{
    public static AuthUIController Instance { get; private set; }

    [Header("UI References - Register")]
    public TMP_InputField registerUsernameInput;
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerPasswordInput;
    public TMP_Text registerStatusText;

    [Header("UI References - Login")]
    public TMP_InputField loginUsernameInput;
    public TMP_InputField loginPasswordInput;
    public TMP_Text loginStatusText;
    public TMP_Text connectionStatusText;

    [Header("UI References - Network")]
    public Button startHostButton;
    public Button startClientButton;

    [Header("UI References - Panels")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject networkRoleSelectionPanel;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        ShowLoginPanel();
        networkRoleSelectionPanel?.SetActive(false);
        
        startHostButton?.onClick.AddListener(() =>
        {
            if (GameNetworkManager.Instance != null)
            {
                GameNetworkManager.Instance.StartHost();
            }
            else
            {
                Debug.LogError("[AuthUIController] GameNetworkManager.Instance is not found!");
            }
        });
        
        startClientButton?.onClick.AddListener(() =>
        {
            if (GameNetworkManager.Instance != null)
            {
                GameNetworkManager.Instance.StartClient();
            }
            else
            {
                Debug.LogError("[AuthUIController] GameNetworkManager.Instance is not found!");
            }
        });
        
        if (AuthService.Instance != null)
        {
            AuthService.Instance.OnAuthSuccess.AddListener(OnAuthServiceSuccess);
        }
        else
        {
            Debug.LogError("[AuthUIController] AuthService.Instance is null. Is AuthService GameObject in the scene and persistent?");
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
        }
    }

    private void OnDestroy()
    {
        if (AuthService.Instance != null)
        {
            AuthService.Instance.OnAuthSuccess.RemoveListener(OnAuthServiceSuccess);
        }
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }
    }
    
    private void OnAuthServiceSuccess()
    {
                ShowRoleSelectionPanel();
    }
    
    private void HandleClientDisconnect(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer) return; 

        string reason = NetworkManager.Singleton.DisconnectReason;
        if (string.IsNullOrEmpty(reason))
        {
            reason = "Failed to connect or lost connection.";
        }
        
        ShowLoginPanel(reason);
    }
    
    // ============================================================
    // PANEL DISPLAY METHODS
    // ============================================================
    public void OnRegisterButtonClick()
    {
        string username = registerUsernameInput.text;
        string email = registerEmailInput.text;
        string password = registerPasswordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            if (registerStatusText != null) registerStatusText.text = "All fields are required.";
            Debug.LogError("[AuthUIController] Registration Error: All fields are required.");
            return;
        }
        if (registerStatusText != null) registerStatusText.text = "Registering...";
        
        if (AuthService.Instance != null)
        {
            AuthService.Instance.RegisterUser(username, email, password, (success, message) => {
                if (registerStatusText != null) registerStatusText.text = message;
    
                else Debug.LogError("[AuthUIController] Registration failed callback: " + message);
            });
        }
        else Debug.LogError("[AuthUIController] AuthService.Instance is null for registration.");
    }
    
    public void OnLoginButtonClick()
    {
        string username = loginUsernameInput.text;
        string password = loginPasswordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            if (loginStatusText != null) loginStatusText.text = "Username and password are required.";
            Debug.LogError("[AuthUIController] Login Error: Username and password are required.");
            return;
        }
        if (loginStatusText != null) loginStatusText.text = "Logging in...";
        if (connectionStatusText != null) connectionStatusText.text = "";
        
        if (AuthService.Instance != null)
        {
            AuthService.Instance.LoginUser(username, password, (success, message) => {
                if (loginStatusText != null) loginStatusText.text = message;
    
                else Debug.LogError("[AuthUIController] Login failed callback: " + message);
            });
        }
        else Debug.LogError("[AuthUIController] AuthService.Instance is null for login.");
    }

    public void OnQuitApplicationButtonClick()
    {

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    public void ShowRoleSelectionPanel()
    {
        if (AuthService.Instance != null && !string.IsNullOrEmpty(AuthService.Instance.GetCurrentToken()))
        {
    
            loginPanel?.SetActive(false);
            registerPanel?.SetActive(false);
            networkRoleSelectionPanel?.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[AuthUIController] No valid token found, returning to login panel.");
            ShowLoginPanel("Session expired or login failed. Please log in again.");
        }
    }
    
    public void OnExitRegisterButtonClick()
    {
        ShowLoginPanel();
    }

    public void ShowLoginPanel(string connectionMessage = null)
    {
        loginPanel?.SetActive(true);
        registerPanel?.SetActive(false);
        networkRoleSelectionPanel?.SetActive(false);
        
        if (loginStatusText != null) loginStatusText.text = "";

        if (connectionStatusText != null)
        {
            connectionStatusText.text = connectionMessage ?? "";
        }
    }

    public void ShowRegisterPanel()
    {
        loginPanel?.SetActive(false);
        registerPanel?.SetActive(true);
        networkRoleSelectionPanel?.SetActive(false);
        if (registerStatusText != null) registerStatusText.text = "";
        if (connectionStatusText != null) connectionStatusText.text = "";
    }
}