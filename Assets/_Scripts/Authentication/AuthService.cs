using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using UnityEngine.Events;

namespace Jae.Authentication
{
    public class AuthService : MonoBehaviour
    {
        public static AuthService Instance { get; private set; }

        [Header("Server Settings")]
        public string serverUrl = "http://localhost:3000/api/auth/";
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
        
        public void RegisterUser(string username, string email, string password, System.Action<bool, string> callback)
        {
            StartCoroutine(RegisterRequest(username, email, password, callback));
        }

        public void LoginUser(string username, string password, System.Action<bool, string> callback)
        {
            StartCoroutine(LoginRequest(username, password, callback));
        }


        IEnumerator RegisterRequest(string username, string email, string password, System.Action<bool, string> callback)
        {
            string jsonPayload = "{\"username\":\"" + username + "\",\"email\":\"" + email + "\",\"password\":\"" + password + "\"}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

            UnityWebRequest request = new UnityWebRequest(serverUrl + "register", "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

                    yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Registration Error: " + request.error);
                string errorMessage = ParseErrorMessage(request.downloadHandler?.text ?? "");
                callback?.Invoke(false, "Registration Failed: " + errorMessage);
            }
            else
            {

                callback?.Invoke(true, "Registration Successful!");
            }
        }

        IEnumerator LoginRequest(string username, string password, System.Action<bool, string> callback)
        {
            string jsonPayload = "{\"username\":\"" + username + "\",\"password\":\"" + password + "\"}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

            UnityWebRequest request = new UnityWebRequest(serverUrl + "login", "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");


            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Login Error: " + request.error);
                string errorMessage = ParseErrorMessage(request.downloadHandler?.text ?? "");
                callback?.Invoke(false, "Login Failed: " + errorMessage);
            }
            else
            {

                string responseText = request.downloadHandler.text;
    
                try
                {
                    AuthResponse authResponse = JsonUtility.FromJson<AuthResponse>(responseText);
                    if (authResponse != null && !string.IsNullOrEmpty(authResponse.token))
                    {
                        _jwtToken = authResponse.token;
            
                        StoreToken(_jwtToken);
                        callback?.Invoke(true, "Login Successful!");
                        OnAuthSuccess.Invoke();
                    }
                    else
                    {
                        string errorMessage = ParseErrorMessage(responseText);
                        Debug.LogError("Error parsing token: " + errorMessage);
                        callback?.Invoke(false, "Login Successful, but failed to parse token: " + errorMessage);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error parsing token: " + e.Message);
                    callback?.Invoke(false, "Login Successful, but failed to parse token: " + e.Message);
                }
            }
        }

        private string ParseErrorMessage(string jsonText)
        {
            if (string.IsNullOrEmpty(jsonText)) return "No response from server.";
            try
            {
                AuthResponse errorResponse = JsonUtility.FromJson<AuthResponse>(jsonText);
                return errorResponse?.message ?? jsonText;
            }
            catch
            {
                return jsonText;
            }
        }

        private void StoreToken(string token)
        {
            PlayerPrefs.SetString(JWT_TOKEN_KEY, token);
            PlayerPrefs.Save();
        }

        public string GetStoredToken()
        {
            if (string.IsNullOrEmpty(_jwtToken))
            {
                _jwtToken = PlayerPrefs.GetString(JWT_TOKEN_KEY, "");
            }
            return _jwtToken;
        }

        public void ClearStoredToken()
        {
            PlayerPrefs.DeleteKey(JWT_TOKEN_KEY);
            PlayerPrefs.Save();
            _jwtToken = "";
        }

        public string GetCurrentToken()
        {
            if (string.IsNullOrEmpty(_jwtToken))
            {
                _jwtToken = GetStoredToken(); 
            }
            return _jwtToken;
        }

        public void InvalidateToken()
        {
            ClearStoredToken();
        }
    }
}