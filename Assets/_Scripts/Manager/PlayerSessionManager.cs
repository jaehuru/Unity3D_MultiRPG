using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;
using System.Text;
using System.Threading.Tasks;
using Jae.Authentication;

namespace Jae.Manager
{
    public class PlayerSessionManager : NetworkBehaviour
    {
        public static PlayerSessionManager Instance { get; private set; }

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
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;
            base.OnNetworkDespawn();
        }
        
        public async void HandleConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            var result = await HandleConnectionApproval(request);
            response.Approved = result.Approved;
            response.Reason = result.Reason;
            response.CreatePlayerObject = false;
            response.Position = result.Position;
            response.Rotation = result.Rotation;
            response.Pending = false;
        }

        public async void HandleClientDisconnected(ulong clientId)
        {
            await HandleClientDisconnect(clientId);
        }
        
        private async Task<(bool Approved, string Reason, Vector3 Position, Quaternion Rotation)> HandleConnectionApproval(NetworkManager.ConnectionApprovalRequest request)
        {
            string jwtToken = "";
            
            // Host
            if (request.ClientNetworkId == NetworkManager.ServerClientId && NetworkManager.Singleton.IsHost)
            {
                if (AuthService.Instance == null)
                {
                    Debug.LogError("[PlayerSessionManager] AuthService.Instance is null. Cannot get token for host.");
                    return (false, "Auth service unavailable for host.", Vector3.zero, Quaternion.identity);
                }
                jwtToken = AuthService.Instance.GetCurrentToken();
            }
            else // Client
            {
                try
                {
                    if (request.Payload != null && request.Payload.Length > 0)
                    {
                        jwtToken = Encoding.UTF8.GetString(request.Payload);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PlayerSessionManager] Failed to parse payload as JWT: {ex}");
                    return (false, "Invalid payload format.", Vector3.zero, Quaternion.identity);
                }
            }

            if (string.IsNullOrEmpty(jwtToken))
            {
                Debug.LogWarning("[PlayerSessionManager] JWT token not provided in payload.");
                return (false, "Authentication token required.", Vector3.zero, Quaternion.identity);
            }

            if (ServerAuthService.Instance == null)
            {
                Debug.LogError("[PlayerSessionManager] ServerAuthService.Instance is null. Is it in the scene?");
                return (false, "Server authentication service unavailable.", Vector3.zero, Quaternion.identity);
            }

            AuthValidationResult validationResult = await ServerAuthService.Instance.ValidateTokenAsync(jwtToken);
            
            string uid = "unknown";
            if (validationResult.IsValid)
            {
                uid = validationResult.UserId;

                // 동일 ID 로그인 방지
                foreach (var clientEntry in connectedClientsData.Values)
                {
                    if (clientEntry.Uid == uid)
                    {
                        Debug.LogWarning($"[PlayerSessionManager] User {uid} (client {request.ClientNetworkId}) is already connected. Blocking new connection.");
                        return (false, "User already connected.", Vector3.zero, Quaternion.identity);
                    }
                }
                
                // Player Data Loading
                if (PlayerServerDataService.Instance == null)
                {
                    Debug.LogError("[PlayerSessionManager] PlayerServerDataService.Instance is null. Is it in the scene?");
                    return (false, "Player data service unavailable.", Vector3.zero, Quaternion.identity);
                }

                PlayerData loadedPlayerData = await PlayerServerDataService.Instance.LoadPlayerDataAsync(jwtToken);
                Vector3 spawnPos = Vector3.zero;

                if (loadedPlayerData != null && loadedPlayerData.position != null)
                {
                    spawnPos = loadedPlayerData.position.ToVector3();
                }

                connectedClientsData[request.ClientNetworkId] = new ClientInfo { Uid = uid, JwtToken = jwtToken, PlayerSpawnPosition = spawnPos };
                
                return (true, "", spawnPos, Quaternion.identity); // Approved
            }
            else
            {
                Debug.LogWarning($"[PlayerSessionManager] JWT token validation failed for client {request.ClientNetworkId}: {validationResult.ErrorMessage}");
                return (false, $"Authentication failed: {validationResult.ErrorMessage}", Vector3.zero, Quaternion.identity);
            }
        }

        // --- Disconnect Logic ---
        private async Task HandleClientDisconnect(ulong clientId)
        {
            if (!IsServer) return; // Should only run on server

            if (connectedClientsData.TryGetValue(clientId, out ClientInfo clientInfo))
            {
                if (clientInfo.PlayerNetworkObject != null)
                {
                    Vector3 pos = clientInfo.PlayerNetworkObject.transform.position;
                    string jwtToken = clientInfo.JwtToken;
                    string uid = clientInfo.Uid;

                    // Player Data Saving
                    if (PlayerServerDataService.Instance == null)
                    {
                        Debug.LogError($"[PlayerSessionManager] PlayerServerDataService.Instance is null on disconnect for client {clientId}. Cannot save data.");
                        connectedClientsData.Remove(clientId);
                        return;
                    }
                    
                    PlayerData dataToSave = new PlayerData(pos);
                    
                    bool saveSuccess = await PlayerServerDataService.Instance.SavePlayerDataAsync(jwtToken, dataToSave);
                    if (!saveSuccess)
                    {
                        Debug.LogWarning($"[PlayerSessionManager] Failed to save data for uid {uid} on disconnect.");
                    }
                }
                else
                {
                    Debug.LogWarning($"[PlayerSessionManager] Player NetworkObject not found for client {clientId}. Cannot save position.");
                }
                connectedClientsData.Remove(clientId);
            }
            else
            {
                    Debug.LogWarning($"[PlayerSessionManager] ClientInfo not found for client {clientId} on disconnect.");
            }
        }
        
        // --- Client Data Access ---
        public bool TryGetClientInfo(ulong clientId, out ClientInfo clientInfo)
        {
            clientInfo = null;
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
            else
            {
                Debug.LogWarning($"[PlayerSessionManager] ClientInfo not found for client {clientId}. Cannot set player network object.");
            }
        }
    }
}