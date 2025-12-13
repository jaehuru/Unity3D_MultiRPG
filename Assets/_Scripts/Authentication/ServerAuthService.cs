using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using System;

namespace Jae.Authentication
{
    [Serializable]
    public class ValidateTokenResponse
    {
        public string message;
        public DecodedTokenData decoded;
    }

    [Serializable]
    public class DecodedTokenData
    {
        public int id;
        public string username;
    }

    public class AuthValidationResult
    {
        public bool IsValid;
        public string UserId;
        public string ErrorMessage;

        public AuthValidationResult(bool isValid, string userId = null, string errorMessage = null)
        {
            IsValid = isValid;
            UserId = userId;
            ErrorMessage = errorMessage;
        }
    }

    public class ServerAuthService : MonoBehaviour
    {
        public static ServerAuthService Instance { get; private set; }

        [Header("Auth Server Settings")]
        public string authServerUrl = "http://localhost:3000/api/auth/validateToken";

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
            }
        }
        
        public Task<AuthValidationResult> ValidateTokenAsync(string jwtToken)
        {
            var tcs = new TaskCompletionSource<AuthValidationResult>();
            StartCoroutine(ValidateTokenCoroutine(jwtToken, tcs));
            return tcs.Task;
        }

        private IEnumerator ValidateTokenCoroutine(string jwtToken, TaskCompletionSource<AuthValidationResult> tcs)
        {
            if (string.IsNullOrEmpty(jwtToken))
            {
                tcs.SetResult(new AuthValidationResult(false, errorMessage: "JWT Token is empty."));
                yield break;
            }

            string jsonPayload = "{\"token\":\"" + jwtToken + "\"}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

            UnityWebRequest request = new UnityWebRequest(authServerUrl, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");


            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMsg = $"[ServerAuthService] Token validation failed: {request.error} - {request.downloadHandler.text}";
                Debug.LogError(errorMsg);
                tcs.SetResult(new AuthValidationResult(false, errorMessage: errorMsg));
            }
            else
            {
                string responseText = request.downloadHandler.text;


                try
                {
                    ValidateTokenResponse response = JsonUtility.FromJson<ValidateTokenResponse>(responseText);

                    if (response != null && response.decoded != null && !string.IsNullOrEmpty(response.decoded.username))
                    {
                        tcs.SetResult(new AuthValidationResult(true, userId: response.decoded.username));
                    }
                    else
                    {
                        string errorMsg = $"[ServerAuthService] Invalid token response format or token invalid: {responseText}";
                        Debug.LogWarning(errorMsg);
                        tcs.SetResult(new AuthValidationResult(false, errorMessage: errorMsg));
                    }
                }
                catch (Exception e)
                {
                    string errorMsg = $"[ServerAuthService] Error parsing token validation response: {e.Message} - {responseText}";
                    Debug.LogError(errorMsg);
                    tcs.SetResult(new AuthValidationResult(false, errorMessage: errorMsg));
                }
            }
        }
    }
}