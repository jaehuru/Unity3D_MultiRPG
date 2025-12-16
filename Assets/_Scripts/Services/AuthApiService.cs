using System.Threading.Tasks;
using System.Collections.Generic;
// Unity
using UnityEngine;
// Project
using Jae.UnityAdapter;

namespace Jae.Services
{
    // --- Result Structs for AuthService ---
    public struct AuthRequestResult
    {
        public bool Success;
        public string Message;
        public string Token;
    }

    public struct AuthValidationResult
    {
        public bool IsValid;
        public string UserId;
        public string ErrorMessage;
    }

    // --- Request DTOs ---
    [System.Serializable]
    internal class RegisterRequestDTO
    {
        public string username;
        public string email;
        public string password;
    }

    [System.Serializable]
    internal class LoginRequestDTO
    {
        public string username;
        public string password;
    }
    
    [System.Serializable]
    internal class ValidateTokenRequestDTO
    {
        public string token;
    }

    // --- Response DTOs ---
    [System.Serializable]
    internal class AuthResponseDTO
    {
        public string message;
        public string token;
    }
    
    [System.Serializable]
    internal class ValidateTokenResponseDTO
    {
        public string message;
        public DecodedTokenData decoded;
    }

    [System.Serializable]
    internal class DecodedTokenData
    {
        public int id;
        public string username;
    }
    
    public class AuthService
    {
        private readonly string _baseUrl;
        private readonly string _registerUrl;
        private readonly string _loginUrl;
        private readonly string _validateUrl;
        // --- 성능 최적화: 인스턴스 캐싱 ---
        private readonly WebRequestAdapter _webRequestAdapter;

        public AuthService(string baseUrl)
        {
            _baseUrl = baseUrl;
            _registerUrl = _baseUrl + "register";
            _loginUrl = _baseUrl + "login";
            _validateUrl = _baseUrl + "validateToken";
            _webRequestAdapter = WebRequestAdapter.Instance;
        }

        public async Task<AuthRequestResult> RegisterUser(string username, string email, string password)
        {
            var requestDto = new RegisterRequestDTO { username = username, email = email, password = password };
            string jsonPayload = JsonUtility.ToJson(requestDto);
            var headers = new Dictionary<string, string> { { "Content-Type", "application/json" } };
            
            WebRequestResult result = await _webRequestAdapter.PostAsync(_registerUrl, jsonPayload, headers);
            
            if (!result.Success)
            {
                return new AuthRequestResult { Success = false, Message = result.Error };
            }
            
            AuthResponseDTO response = JsonUtility.FromJson<AuthResponseDTO>(result.ResponseText);
            return new AuthRequestResult { Success = true, Message = response?.message ?? "Success!" };
        }

        public async Task<AuthRequestResult> LoginUser(string username, string password)
        {
            var requestDto = new LoginRequestDTO { username = username, password = password };
            string jsonPayload = JsonUtility.ToJson(requestDto);
            var headers = new Dictionary<string, string> { { "Content-Type", "application/json" } };
            
            WebRequestResult result = await _webRequestAdapter.PostAsync(_loginUrl, jsonPayload, headers);

            if (!result.Success)
            {
                return new AuthRequestResult { Success = false, Message = result.Error };
            }

            AuthResponseDTO response = JsonUtility.FromJson<AuthResponseDTO>(result.ResponseText);
            return new AuthRequestResult { Success = true, Message = response?.message, Token = response?.token };
        }
        
        public async Task<AuthValidationResult> ValidateToken(string jwtToken)
        {
            if (string.IsNullOrEmpty(jwtToken))
            {
                return new AuthValidationResult { IsValid = false, ErrorMessage = "JWT Token is empty." };
            }
            
            var requestDto = new ValidateTokenRequestDTO { token = jwtToken };
            string jsonPayload = JsonUtility.ToJson(requestDto);
            var headers = new Dictionary<string, string> { { "Content-Type", "application/json" } };
            
            WebRequestResult result = await _webRequestAdapter.PostAsync(_validateUrl, jsonPayload, headers);

            if (!result.Success)
            {
                return new AuthValidationResult { IsValid = false, ErrorMessage = result.Error };
            }

            ValidateTokenResponseDTO response = JsonUtility.FromJson<ValidateTokenResponseDTO>(result.ResponseText);
            if (response?.decoded != null && !string.IsNullOrEmpty(response.decoded.username))
            {
                return new AuthValidationResult { IsValid = true, UserId = response.decoded.username };
            }
            else
            {
                return new AuthValidationResult { IsValid = false, ErrorMessage = response?.message ?? "Invalid token response format."};
            }
        }
    }
}
