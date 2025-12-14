using UnityEngine;
using TMPro; 
using UnityEngine.UI; 
using Jae.Manager; 
using Jae.Application; 
using Unity.Netcode;

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
                Debug.LogError("[AuthUIController] NetworkGameOrchestrator.Instance is not found!");
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
                Debug.LogError("[AuthUIController] NetworkGameOrchestrator.Instance is not found!");
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
            Debug.LogError("[AuthUIController] AuthManager.Instance is null. Is AuthManager GameObject in the scene and persistent?");
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
        Debug.LogError("[AuthUIController] Login failed callback: " + message);
    }

    private void OnRegisterSuccessHandler(string message)
    {
        if (registerStatusText != null)
        {
            registerStatusText.text = "Registration Successful: " + message;
        }
        Debug.Log("[AuthUIController] Registration Successful: " + message);
        ShowLoginPanel(); // Go back to login after successful registration
    }

    private void OnRegisterFailureHandler(string message)
    {
        if (registerStatusText != null)
        {
            registerStatusText.text = "Registration Failed: " + message;
        }
        Debug.LogError("[AuthUIController] Registration failed callback: " + message);
    }
    
    private void HandleClientDisconnect(ulong clientId)
    {
        // This is still using NetworkManager, which is fine
        // A dedicated NetworkUIController might be better for this in future
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) return; 

        string reason = NetworkManager.Singleton?.DisconnectReason; // Use null conditional operator
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
        
        AuthManager.Instance.AttemptRegister(username, email, password);
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
        
        AuthManager.Instance.AttemptLogin(username, password);
    }

    public void OnQuitApplicationButtonClick()
    {
        GameManager.Instance.QuitApplication();
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