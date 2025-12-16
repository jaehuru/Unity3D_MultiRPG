using System.Collections.Generic;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
// Unity
using UnityEngine;
using Unity.Netcode;
// Project
using Jae.Services;

namespace Jae.Manager
{
    public class PlayerSessionManager : NetworkBehaviour
    {
        public static PlayerSessionManager Instance { get; private set; }
        
        [Header("Service URLs")]
        [SerializeField] private string authUrl = "http://localhost:3000/api/auth/";
        [SerializeField] private string playerDataUrl = "http://localhost:3000/api/playerdata/";

        private AuthService _authService;
        private PlayerDataService _playerDataService;
        
        public class ClientInfo 
        {
            public string Uid;
            public string JwtToken;
            public NetworkObject PlayerNetworkObject;
            public Vector3 PlayerSpawnPosition;
        }

        private readonly Dictionary<ulong, ClientInfo> connectedClientsData = new();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                _authService = new AuthService(authUrl);
                _playerDataService = new PlayerDataService(playerDataUrl);
                
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        public void HandleConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            if (this == null || !gameObject.activeInHierarchy || !enabled)
            {
                response.Approved = false;
                response.Pending = false;
                return;
            }
            StartCoroutine(ConnectionApprovalCoroutine(request, response));
        }
        
        public void HandleClientDisconnected(ulong clientId)
        {
            _ = SaveOnDisconnectAsync(clientId);
        }

        private IEnumerator ConnectionApprovalCoroutine(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Pending = true;
            
            string jwtToken = "";
            bool shouldProcess = true;
            
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                response.Approved = false;
                response.Reason = "NetworkManager.Singleton is NULL at approval stage.";
                response.Pending = false;
                Debug.LogError("[PlayerSessionManager] ConnectionApprovalCoroutine: NetworkManager.Singleton is NULL. Ensure NetworkManager is set up correctly in the scene!");
                yield break;
            }

            if (request.ClientNetworkId == NetworkManager.ServerClientId && networkManager.IsHost)
            {
                AuthManager authManager = AuthManager.Instance; // AuthManager.Instance를 여기서 가져와 null 체크
                if (authManager == null)
                {
                    response.Approved = false;
                    response.Reason = "AuthManager.Instance is NULL at approval stage.";
                    response.Pending = false;
                    Debug.LogError("[PlayerSessionManager] ConnectionApprovalCoroutine: AuthManager.Instance is NULL!");
                    yield break;
                }
                jwtToken = authManager.GetCurrentToken();
            }
            else
            {
                try { jwtToken = Encoding.UTF8.GetString(request.Payload); }
                catch (Exception ex)
                {
                    response.Reason = $"Invalid payload format: {ex.Message}";
                    shouldProcess = false;
                }
            }
            
            if (string.IsNullOrEmpty(jwtToken))
            {
                response.Reason = "Authentication token required.";
                shouldProcess = false;
            }
            
            if (!shouldProcess)
            {
                response.Approved = false;
                response.Pending = false;
                yield break;
            }
            
            Task<AuthValidationResult> validationTask = _authService.ValidateToken(jwtToken);
            yield return new WaitUntil(() => validationTask.IsCompleted);
            
            if (validationTask.IsFaulted)
            {
                response.Approved = false;
                response.Reason = "Authentication service error.";
                response.Pending = false;
                yield break;
            }
            
            AuthValidationResult validationResult = validationTask.Result;
            
            if (!validationResult.IsValid)
            {
                response.Approved = false;
                response.Reason = $"Authentication failed: {validationResult.ErrorMessage}";
                response.Pending = false;
                yield break;
            }
            
            string uid = validationResult.UserId;

            foreach (var clientEntry in connectedClientsData.Values)
            {
                if (clientEntry.Uid == uid)
                {
                    response.Approved = false;
                    response.Reason = "User already connected.";
                    response.Pending = false;
                    yield break;
                }
            }

            Task<PlayerData> loadTask = _playerDataService.LoadPlayerData(jwtToken);
            yield return new WaitUntil(() => loadTask.IsCompleted);
            PlayerData loadedPlayerData = loadTask.Result;

            Vector3 spawnPos = loadedPlayerData?.position.ToVector3() ?? Vector3.zero;

            connectedClientsData[request.ClientNetworkId] = new ClientInfo { Uid = uid, JwtToken = jwtToken, PlayerSpawnPosition = spawnPos };
            
            response.Approved = true;
            response.CreatePlayerObject = false;
            response.Position = spawnPos;
            response.Rotation = Quaternion.identity;
            response.Pending = false;
        }
        
        private async Task SaveOnDisconnectAsync(ulong clientId)
        {
            if (!IsServer || !connectedClientsData.TryGetValue(clientId, out ClientInfo clientInfo)) return;

            connectedClientsData.Remove(clientId);

            if (clientInfo.PlayerNetworkObject != null)
            {
                PlayerData dataToSave = new PlayerData(clientInfo.PlayerNetworkObject.transform.position);
                bool saveSuccess = await _playerDataService.SavePlayerData(clientInfo.JwtToken, dataToSave);
            }
        }
        
        public bool TryGetClientInfo(ulong clientId, out ClientInfo clientInfo)
        {
            return connectedClientsData.TryGetValue(clientId, out clientInfo);
        }

        public bool TryGetPlayerNetworkObject(ulong clientId, out NetworkObject playerNetworkObject)
        {
            playerNetworkObject = null;
            if (connectedClientsData.TryGetValue(clientId, out ClientInfo clientInfo))
            {
                playerNetworkObject = clientInfo.PlayerNetworkObject;
                return playerNetworkObject != null;
            }
            return false;
        }

        public void SetPlayerNetworkObject(ulong clientId, NetworkObject playerNetworkObject)
        {
            if (connectedClientsData.TryGetValue(clientId, out ClientInfo clientInfo))
            {
                clientInfo.PlayerNetworkObject = playerNetworkObject;
            }
        }

        public void AddClientInfo(ulong clientId, string uid, string jwtToken, Vector3 spawnPosition)
        {
            if (!connectedClientsData.ContainsKey(clientId))
            {
                connectedClientsData[clientId] = new ClientInfo { Uid = uid, JwtToken = jwtToken, PlayerSpawnPosition = spawnPosition };
            }
        }
        
        public void RequestSavePosition(ulong clientId, Vector3 position)
        {
            if (!IsServer) return;
            _ = SavePositionAsync(clientId, position);
        }
        
        private async Task SavePositionAsync(ulong clientId, Vector3 position)
        {
            if (connectedClientsData.TryGetValue(clientId, out ClientInfo clientInfo))
            {
                PlayerData dataToSave = new PlayerData(position);
                bool saveSuccess = await _playerDataService.SavePlayerData(clientInfo.JwtToken, dataToSave);
            }
        }
    }
}