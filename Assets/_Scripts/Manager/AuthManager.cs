using UnityEngine;
using UnityEngine.Events;
using Jae.Services;


namespace Jae.Manager
{
    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance { get; private set; }

        [Header("Server Settings")]
        [SerializeField] private string serverUrl = "http://localhost:3000/api/auth/";

        [Header("Events")]
        public UnityEvent OnLoginSuccess;
        public UnityEvent<string> OnLoginFailure;
        public UnityEvent<string> OnRegisterSuccess;
        public UnityEvent<string> OnRegisterFailure;

        private AuthService _authService;
        private string _jwtToken;
        private string _currentUserId; // Add this field
        private const string JWT_TOKEN_KEY = "JwtToken";
        private const string USER_ID_KEY = "UserId"; // Add this key

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
                LoadTokenFromPrefs();
                _authService = new AuthService(serverUrl);
            }
        }
        
        public string GetCurrentToken()
        {
            if (string.IsNullOrEmpty(_jwtToken))
            {
                LoadTokenFromPrefs();
            }
            return _jwtToken;
        }

        public string GetUserId() // Add this method
        {
            if (string.IsNullOrEmpty(_currentUserId))
            {
                LoadTokenFromPrefs();
            }
            return _currentUserId;
        }

        private void StoreToken(string token, string userId) // Modified to accept userId
        {
            _jwtToken = token;
            _currentUserId = userId; // Store userId
            PlayerPrefs.SetString(JWT_TOKEN_KEY, token);
            PlayerPrefs.SetString(USER_ID_KEY, userId); // Store userId in PlayerPrefs
            PlayerPrefs.Save();
        }

        public void ClearStoredToken()
        {
            PlayerPrefs.DeleteKey(JWT_TOKEN_KEY);
            PlayerPrefs.DeleteKey(USER_ID_KEY); // Clear userId
            PlayerPrefs.Save();
            _jwtToken = "";
            _currentUserId = ""; // Clear userId
        }
        
        private void LoadTokenFromPrefs()
        {
            _jwtToken = PlayerPrefs.GetString(JWT_TOKEN_KEY, "");
            _currentUserId = PlayerPrefs.GetString(USER_ID_KEY, ""); // Load userId
        }

        public async void AttemptLogin(string username, string password)
        {
            AuthRequestResult result = await _authService.LoginUser(username, password);
            if (result.Success)
            {
                // Validate token to get UserId
                AuthValidationResult validationResult = await _authService.ValidateToken(result.Token);
                if (validationResult.IsValid)
                {
                    StoreToken(result.Token, validationResult.UserId); // Store both token and userId
                    OnLoginSuccess.Invoke();
                }
                else
                {
                    OnLoginFailure.Invoke(validationResult.ErrorMessage); // If token validation fails
                }
            }
            else
            {
                OnLoginFailure.Invoke(result.Message);
            }
        }

        public async void AttemptRegister(string username, string email, string password)
        {
            AuthRequestResult result = await _authService.RegisterUser(username, email, password);
            if (result.Success)
            {
                OnRegisterSuccess.Invoke(result.Message);
            }
            else
            {
                OnRegisterFailure.Invoke(result.Message);
            }
        }
    }
}
