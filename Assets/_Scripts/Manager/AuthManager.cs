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
        private const string JWT_TOKEN_KEY = "JwtToken";

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

        private void StoreToken(string token)
        {
            _jwtToken = token;
            PlayerPrefs.SetString(JWT_TOKEN_KEY, token);
            PlayerPrefs.Save();
        }

        public void ClearStoredToken()
        {
            PlayerPrefs.DeleteKey(JWT_TOKEN_KEY);
            PlayerPrefs.Save();
            _jwtToken = "";
        }
        
        private void LoadTokenFromPrefs()
        {
            _jwtToken = PlayerPrefs.GetString(JWT_TOKEN_KEY, "");
        }

        public async void AttemptLogin(string username, string password)
        {
            AuthRequestResult result = await _authService.LoginUser(username, password);
            if (result.Success)
            {
                StoreToken(result.Token);
                OnLoginSuccess.Invoke();
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
