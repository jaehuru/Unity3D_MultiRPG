using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using Jae.UnityAdapter;

namespace Jae.Services
{
    // --- DTOs and Data Structures ---
    [System.Serializable]
    public class PlayerPosition
    {
        public float x, y, z;
        public PlayerPosition(Vector3 pos) { x = pos.x; y = pos.y; z = pos.z; }
        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    [System.Serializable]
    public class PlayerData
    {
        public PlayerPosition position;
        public int health;
        public List<string> inventory;

        public PlayerData() 
        {
            position = new PlayerPosition(Vector3.zero);
            health = 100;
            inventory = new List<string>();
        }

        public PlayerData(Vector3 pos)
        {
            position = new PlayerPosition(pos);
            health = 100;
            inventory = new List<string>();
        }
    }

    [System.Serializable]
    internal class LoadPlayerResponseDTO
    {
        public string message;
        public PlayerData playerData;
    }

    [System.Serializable]
    internal class SavePlayerRequestDTO
    {
        public PlayerData playerData;
    }
    
    public class PlayerDataService
    {
        private readonly string _loadUrl;
        private readonly string _saveUrl;

        public PlayerDataService(string baseUrl)
        {
            _loadUrl = baseUrl + "load";
            _saveUrl = baseUrl + "save";
        }

        public async Task<PlayerData> LoadPlayerData(string jwtToken)
        {
            var headers = new Dictionary<string, string> { { "Authorization", "Bearer " + jwtToken } };

            WebRequestResult result = await WebRequestAdapter.Instance.GetAsync(_loadUrl, headers);

            if (!result.Success)
            {
                Debug.LogError($"PlayerDataService Load Error: {result.Error}");
                return null;
            }
            
            LoadPlayerResponseDTO response = JsonUtility.FromJson<LoadPlayerResponseDTO>(result.ResponseText);
            return response.playerData;
        }

        public async Task<bool> SavePlayerData(string jwtToken, PlayerData dataToSave)
        {
            if (dataToSave == null)
            {
                Debug.LogError("[PlayerDataService] Data to save is null.");
                return false;
            }
            
            var requestBody = new SavePlayerRequestDTO { playerData = dataToSave };
            string jsonPayload = JsonUtility.ToJson(requestBody);
            
            var headers = new Dictionary<string, string>
            {
                { "Authorization", "Bearer " + jwtToken },
                { "Content-Type", "application/json" }
            };

            WebRequestResult result = await WebRequestAdapter.Instance.PostAsync(_saveUrl, jsonPayload, headers);
            
            if (!result.Success)
            {
                 Debug.LogError($"PlayerDataService Save Error: {result.Error}");
            }
            
            return result.Success;
        }
    }
}
