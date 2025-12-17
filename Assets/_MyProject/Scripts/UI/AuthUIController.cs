// Unity
using UnityEngine;
using TMPro; 
using UnityEngine.UI;
using Unity.Netcode;
// Project
using Jae.Manager; 
using Jae.Application; 

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
            if (NetworkGameOrchestrator.Instance != null)
            {
                NetworkGameOrchestrator.Instance.StartHost();
            }
            else
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogError("[AuthUIController] NetworkGameOrchestrator.Instance is not found!");
#endif
            }
        });
        
        startClientButton?.onClick.AddListener(() =>
        {
            if (NetworkGameOrchestrator.Instance != null)
            {
                NetworkGameOrchestrator.Instance.StartClient();
            }
            else
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogError("[AuthUIController] NetworkGameOrchestrator.Instance is not found!");
#endif
            }
        });
        
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnLoginSuccess.AddListener(OnLoginSuccessHandler);
            AuthManager.Instance.OnLoginFailure.AddListener(OnLoginFailureHandler);
            AuthManager.Instance.OnRegisterSuccess.AddListener(OnRegisterSuccessHandler);
            AuthManager.Instance.OnRegisterFailure.AddListener(OnRegisterFailureHandler);
        }
        else
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.LogError("[AuthUIController] AuthManager.Instance is null. Is AuthManager GameObject in the scene and persistent?");
#endif
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
        }
    }

    private void OnDestroy()
    {
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnLoginSuccess.RemoveListener(OnLoginSuccessHandler);
            AuthManager.Instance.OnLoginFailure.RemoveListener(OnLoginFailureHandler);
            AuthManager.Instance.OnRegisterSuccess.RemoveListener(OnRegisterSuccessHandler);
            AuthManager.Instance.OnRegisterFailure.RemoveListener(OnRegisterFailureHandler);
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return;
        
        if (NetworkManager.Singleton.IsServer)
        {
            return;
        }
        
        string reason = NetworkManager.Singleton.DisconnectReason;
        if (!string.IsNullOrEmpty(reason))
        {
            ShowLoginPanel($"Connection failed: {reason}");
        }
        else
        {
            ShowLoginPanel("Failed to connect to the server.");
        }
    }
    
    private void OnLoginSuccessHandler()
    {
        ShowRoleSelectionPanel();
    }

    private void OnLoginFailureHandler(string message)
    {
        if (loginStatusText != null)
        {
            loginStatusText.text = "Login Failed: " + message;
        }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        Debug.LogError("[AuthUIController] Login failed callback: " + message);
#endif
    }

    private void OnRegisterSuccessHandler(string message)
    {
        if (registerStatusText != null)
        {
            registerStatusText.text = "Registration Successful: " + message;
        }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        Debug.Log("[AuthUIController] Registration Successful: " + message);
#endif
        ShowLoginPanel();
    }

    private void OnRegisterFailureHandler(string message)
    {
        if (registerStatusText != null)
        {
            registerStatusText.text = "Registration Failed: " + message;
        }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        Debug.LogError("[AuthUIController] Registration failed callback: " + message);
#endif
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
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.LogError("[AuthUIController] Registration Error: All fields are required.");
#endif
            return;
        }
        if (registerStatusText != null) registerStatusText.text = "Registering...";
        
        if (AuthManager.Instance != null)
        {
            _ = AuthManager.Instance.AttemptRegister(username, email, password);
        }
        else
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.LogError("[AuthUIController] AuthManager.Instance is null during OnRegisterButtonClick.");
#endif
        }
    }
    
    public void OnLoginButtonClick()
    {
        string username = loginUsernameInput.text;
        string password = loginPasswordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            if (loginStatusText != null) loginStatusText.text = "Username and password are required.";
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.LogError("[AuthUIController] Login Error: Username and password are required.");
#endif
            return;
        }
        if (loginStatusText != null) loginStatusText.text = "Logging in...";
        if (connectionStatusText != null) connectionStatusText.text = "";
        
        if (AuthManager.Instance != null)
        {
            _ = AuthManager.Instance.AttemptLogin(username, password);
        }
        else
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.LogError("[AuthUIController] AuthManager.Instance is null during OnLoginButtonClick.");
#endif
        }
    }

    public void OnQuitApplicationButtonClick()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitApplication();
        }
        else
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.LogError("[AuthUIController] GameManager.Instance is null during OnQuitApplicationButtonClick.");
#endif
        }
    }
    
    public void ShowRoleSelectionPanel()
    {
        if (AuthManager.Instance != null && !string.IsNullOrEmpty(AuthManager.Instance.GetCurrentToken()))
        {
    
            loginPanel?.SetActive(false);
            registerPanel?.SetActive(false);
            networkRoleSelectionPanel?.SetActive(true);
        }
        else
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.LogWarning("[AuthUIController] No valid token found, returning to login panel.");
#endif
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