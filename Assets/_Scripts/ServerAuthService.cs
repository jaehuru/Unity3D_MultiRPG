using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using System;

// Helper class to deserialize the JWT validation response
[Serializable]
public class ValidateTokenResponse
{
    public string message;
    public DecodedTokenData decoded; // This will hold the id and username if valid
}

// Helper class to hold the decoded data from JWT
[Serializable]
public class DecodedTokenData
{
    public int id;
    public string username;
    // Add other fields from your JWT payload if necessary, e.g., email, roles
}

public class AuthValidationResult
{
    public bool IsValid;
    public string UserId; // This will be the username from the decoded token
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
    public string authServerUrl = "http://localhost:3000/api/auth/validateToken"; // Endpoint for JWT validation

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

    // Public method to be called by GameNetworkManager
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

        Debug.Log($"[ServerAuthService] Sending token validation request to: {authServerUrl}");
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
            Debug.Log($"[ServerAuthService] Token validation response: {responseText}");

            try
            {
                ValidateTokenResponse response = JsonUtility.FromJson<ValidateTokenResponse>(responseText);

                if (response != null && response.decoded != null && !string.IsNullOrEmpty(response.decoded.username))
                {
                    // Token is valid and decoded user data is available
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