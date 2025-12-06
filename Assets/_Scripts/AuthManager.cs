using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine.Events;
using Unity.Netcode; 

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    [Header("Server Settings")]
    public string serverUrl = "http://localhost:3000/api/auth/";

    [Header("UI References - Register")]
    public TMP_InputField registerUsernameInput;
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerPasswordInput;
    public TMP_Text registerStatusText;

    [Header("UI References - Login")]
    public TMP_InputField loginUsernameInput;
    public TMP_InputField loginPasswordInput;
    public TMP_Text loginStatusText;

    [Header("UI References - Panels")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject networkRoleSelectionPanel;

    [Header("Events")]
    public UnityEvent OnAuthSuccess; 

    private string _jwtToken;
    private const string JWT_TOKEN_KEY = "JwtToken";
    
    [System.Serializable]
    private class AuthResponse
    {
        public string message;
        public string token;
    }

    void Awake()
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

    void Start()
    {
        _jwtToken = GetStoredToken();
        if (!string.IsNullOrEmpty(_jwtToken))
        {
            Debug.Log("Found stored JWT token.");
        }
        
        ShowLoginPanel();
        networkRoleSelectionPanel?.SetActive(false);
    }
    
    public void Register()
    {
        string username = registerUsernameInput.text;
        string email = registerEmailInput.text;
        string password = registerPasswordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            if (registerStatusText != null) registerStatusText.text = "All fields are required.";
            Debug.LogError("Registration Error: All fields are required.");
            return;
        }
        if (registerStatusText != null) registerStatusText.text = "Registering...";
        StartCoroutine(RegisterRequest(username, email, password));
    }
    
    public void Login()
    {
        string username = loginUsernameInput.text;
        string password = loginPasswordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            if (loginStatusText != null) loginStatusText.text = "Username and password are required.";
            Debug.LogError("Login Error: Username and password are required.");
            return;
        }
        if (loginStatusText != null) loginStatusText.text = "Logging in...";
        StartCoroutine(LoginRequest(username, password));
    }

    IEnumerator RegisterRequest(string username, string email, string password)
    {
        string jsonPayload = "{\"username\":\"" + username + "\",\"email\":\"" + email + "\",\"password\":\"" + password + "\"}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        UnityWebRequest request = new UnityWebRequest(serverUrl + "register", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("Sending registration request to: " + request.url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Registration Error: " + request.error);
            if (registerStatusText != null) registerStatusText.text = "Registration Failed: " + request.downloadHandler.text;
            Debug.LogError("Registration Response: " + request.downloadHandler.text);
        }
        else
        {
            Debug.Log("Registration Success: " + request.downloadHandler.text);
            if (registerStatusText != null) registerStatusText.text = "Registration Successful!";
        }
    }

    IEnumerator LoginRequest(string username, string password)
    {
        string jsonPayload = "{\"username\":\"" + username + "\",\"password\":\"" + password + "\"}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        UnityWebRequest request = new UnityWebRequest(serverUrl + "login", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("Sending login request to: " + request.url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Login Error: " + request.error);
            if (loginStatusText != null) loginStatusText.text = "Login Failed: " + request.downloadHandler.text;
            Debug.LogError("Login Response: " + request.downloadHandler.text);
        }
        else
        {
            Debug.Log("Login Success: " + request.downloadHandler.text);
            if (loginStatusText != null) loginStatusText.text = "Login Successful!";

            string responseText = request.downloadHandler.text;
            try
            {
                AuthResponse authResponse = JsonUtility.FromJson<AuthResponse>(responseText);
                if (authResponse != null && !string.IsNullOrEmpty(authResponse.token))
                {
                    _jwtToken = authResponse.token;
                    Debug.Log("Received JWT Token: " + _jwtToken);
                    StoreToken(_jwtToken);
                    OnAuthSuccess.Invoke();
                }
                else
                {
                    Debug.LogError("Error parsing token: Token not found in response.");
                    if (loginStatusText != null) loginStatusText.text = "Login Successful, but failed to parse token.";
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error parsing token: " + e.Message);
                if (loginStatusText != null) loginStatusText.text = "Login Successful, but failed to parse token.";
            }
        }
    }

    private void StoreToken(string token)
    {
        PlayerPrefs.SetString(JWT_TOKEN_KEY, token);
        PlayerPrefs.Save();
        Debug.Log("JWT token stored in PlayerPrefs.");
    }

    public string GetStoredToken()
    {
        return PlayerPrefs.GetString(JWT_TOKEN_KEY, "");
    }

    public void ClearStoredToken()
    {
        PlayerPrefs.DeleteKey(JWT_TOKEN_KEY);
        PlayerPrefs.Save();
        _jwtToken = "";
        Debug.Log("JWT token cleared from PlayerPrefs.");
    }

    public string GetCurrentToken()
    {
        return _jwtToken;
    }
    
    public void OnLoginSuccessAndShowRoleSelection()
    {
        Debug.Log("Authentication successful. Displaying network role selection.");
        loginPanel?.SetActive(false);
        registerPanel?.SetActive(false);
        networkRoleSelectionPanel?.SetActive(true);
    }
    
    public void ShowLoginPanel()
    {
        loginPanel?.SetActive(true);
        registerPanel?.SetActive(false);
        networkRoleSelectionPanel?.SetActive(false);
    }

    public void ShowRegisterPanel()
    {
        loginPanel?.SetActive(false);
        registerPanel?.SetActive(true);
        networkRoleSelectionPanel?.SetActive(false);
    }
}
